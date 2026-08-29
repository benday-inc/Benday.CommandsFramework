using Benday.CommandsFramework;
using Benday.CommandsFramework.Samples;
using Benday.CommandsFramework.Tui.Model;
using Benday.CommandsFramework.Tui.Screens;

using Spectre.Console.Testing;

namespace Benday.CommandsFramework.Tui.Tests;

/// <summary>
/// Tests what a run looks like, against Spectre's TestConsole. The screen holds no decisions,
/// so these are about what reaches the screen and nothing else.
/// </summary>
public class CommandRunScreenFixture
{
    private static DefaultProgramOptions GetOptions()
    {
        return new DefaultProgramOptions
        {
            ApplicationName = "Sample Tool",
            OutputProvider = new StringBuilderTextOutputProvider(),
            UsesConfiguration = false,
            ArgumentSyntax = ArgumentSyntax.Posix
        };
    }

    private static TestConsole GetConsole()
    {
        // wide enough that the assertions are about content rather than about wrapping
        var console = new TestConsole();

        console.Profile.Width = 200;
        console.Profile.Height = 60;

        return console;
    }

    private static TuiCommandForm Open(ICommandProgramOptions options, string path)
    {
        var browser = new TuiCommandBrowser(
            TuiSession.Create(options, typeof(SampleCommand1).Assembly));

        return TuiCommandForm.Open(
            options,
            typeof(SampleCommand1).Assembly,
            browser.AllCommands.Single(x => x.PathAsString == path));
    }

    private static async Task<(string Output, TuiRunResult Result)> RunAsync(
        TestConsole console, ICommandProgramOptions options, TuiCommandForm form)
    {
        var runner = new TuiCommandRunner(options, typeof(SampleCommand1).Assembly);

        runner.Output.Width = console.Profile.Width;

        var result = await new CommandRunScreen(console, runner)
            .ShowAsync(form, TestContext.Current.CancellationToken);

        return (console.Output, result);
    }

    [Fact]
    public async Task ARunShowsTheCommandLineTheOutputAndHowItWent()
    {
        // arrange
        var options = GetOptions();
        var console = GetConsole();

        using var form = Open(options, ApplicationConstants.CommandName_Greeting);

        form.FindField("name")!.TrySetValue("Ann");

        // act
        var (output, result) = await RunAsync(console, options, form);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Contains(form.GetCommandLine(), output);
        Assert.Contains("Hello, Ann!", output);
        Assert.Contains("Done", output);
    }

    [Fact]
    public async Task ProgressIsShownWhileTheCommandRuns()
    {
        // arrange -- redrawn in place on a terminal, and written as ordinary lines where
        // there is no terminal to redraw on, which is what a TestConsole is
        var options = GetOptions();
        var console = GetConsole();

        using var form = Open(options, ApplicationConstants.CommandName_Progress);

        form.FindField(SampleProgressCommand.ArgumentName_Count)!.TrySetValue("3");

        // act
        var (output, _) = await RunAsync(console, options, form);

        // assert
        Assert.Contains("Processing item 3 3/3 (100%)", output);
        Assert.Contains("Processed 3 items.", output);
    }

    [Fact]
    public async Task AFormThatIsNotFilledInSaysSoRatherThanRunning()
    {
        // arrange
        var options = GetOptions();
        var console = GetConsole();

        using var form = Open(options, ApplicationConstants.CommandName_Greeting);

        // act
        var (output, result) = await RunAsync(console, options, form);

        // assert
        Assert.Equal(CommandExecutionStatus.ValidationFailed, result.Status);
        Assert.Contains("the arguments are not valid", output);

        // the command's own usage output is what explains it, so it has to be on screen
        Assert.Contains("name", output);
    }

    [Fact]
    public async Task AFailureIsShownWithWhatTheCommandManagedToWrite()
    {
        // arrange
        var options = GetOptions();
        var console = GetConsole();

        using var form = Open(options, ApplicationConstants.CommandName_Throws);

        // act
        var (output, result) = await RunAsync(console, options, form);

        // assert
        Assert.Equal(CommandExecutionStatus.Failed, result.Status);
        Assert.Contains("About to fail.", output);
        Assert.Contains("It failed", output);
        Assert.Contains("Something nobody planned for.", output);
    }

    [Fact]
    public async Task ARunOnATerminalSaysHowToStopIt()
    {
        // arrange -- said before the command starts, because the moment someone wants to know
        // is while a long one is running
        var options = GetOptions();
        var console = GetConsole();

        console.Interactive();

        using var form = Open(options, ApplicationConstants.CommandName_Greeting);

        form.FindField("name")!.TrySetValue("Ann");

        // act
        var (output, _) = await RunAsync(console, options, form);

        // assert
        Assert.Contains("Press ctrl-c to cancel it", output);
    }

    [Fact]
    public async Task ARunWithNoTerminalDoesNotOfferAKeystroke()
    {
        // arrange -- there is no keyboard attached to a pipe
        var options = GetOptions();
        var console = GetConsole();

        using var form = Open(options, ApplicationConstants.CommandName_Greeting);

        form.FindField("name")!.TrySetValue("Ann");

        // act
        var (output, _) = await RunAsync(console, options, form);

        // assert
        Assert.DoesNotContain("ctrl-c", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ThereIsNoPromptWhereThereIsNoOneToAnswerIt()
    {
        // arrange -- a redirected console is a test, a pipe or a CI log. Prompting there
        // waits forever for input that is not coming.
        var options = GetOptions();
        var console = GetConsole();

        Assert.False(console.Profile.Capabilities.Interactive);

        using var form = Open(options, ApplicationConstants.CommandName_Interactive);

        // act
        var (output, result) = await RunAsync(console, options, form);

        // assert
        Assert.Equal(CommandExecutionStatus.Success, result.Status);
        Assert.Contains("No name supplied.", output);
    }
}
