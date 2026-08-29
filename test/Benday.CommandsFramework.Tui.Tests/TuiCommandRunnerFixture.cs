using Benday.CommandsFramework;
using Benday.CommandsFramework.Samples;
using Benday.CommandsFramework.Tui.Model;

namespace Benday.CommandsFramework.Tui.Tests;

/// <summary>
/// Tests running a command from the interface: what runs, what it writes, and what happens
/// when it fails. All of it without a terminal, because none of these decisions belong to
/// one.
/// </summary>
public class TuiCommandRunnerFixture
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

    private static TuiCommandItem GetCommand(ICommandProgramOptions options, string path)
    {
        var browser = new TuiCommandBrowser(
            TuiSession.Create(options, typeof(SampleCommand1).Assembly));

        return browser.AllCommands.Single(x => x.PathAsString == path);
    }

    private static TuiCommandForm Open(
        ICommandProgramOptions options,
        string path,
        IReadOnlyDictionary<string, string>? presets = null)
    {
        return TuiCommandForm.Open(
            options, typeof(SampleCommand1).Assembly, GetCommand(options, path), presets);
    }

    private static TuiCommandRunner GetRunner(ICommandProgramOptions options)
    {
        return new TuiCommandRunner(options, typeof(SampleCommand1).Assembly);
    }

    private static string GetResultText(TuiRunResult result)
    {
        return string.Join(
            Environment.NewLine,
            result.Output
                .Where(x => x.Channel == TuiOutputChannel.Result)
                .Select(x => x.Text));
    }

    [Fact]
    public async Task AFilledInFormRunsAndDoesTheWork()
    {
        // arrange
        var options = GetOptions();

        using var form = Open(options, ApplicationConstants.CommandName_Greeting);

        form.FindField("name")!.TrySetValue("Ann");

        // act
        var result = await GetRunner(options).RunAsync(
            form, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(CommandExecutionStatus.Success, result.Status);
        Assert.True(result.IsSuccess);
        Assert.Contains("Hello, Ann!", GetResultText(result));
    }

    [Fact]
    public async Task WhatRunsIsWhatThePreviewSaysWouldRun()
    {
        // arrange -- the command is built from the command line the form shows, parsed by the
        // parser that would have parsed it had it been typed. That is what makes the preview
        // verifiable rather than decorative.
        var options = GetOptions();

        using var form = Open(options, ApplicationConstants.CommandName_Deploy);

        form.FindField("environment")!.TrySetValue("production");
        form.FindField("verbose")!.TrySetValue(bool.TrueString);

        var preview = form.GetCommandLine();

        // act
        var result = await GetRunner(options).RunAsync(
            form, TestContext.Current.CancellationToken);

        // assert
        Assert.Contains("--environment production", preview);
        Assert.Contains("--verbose", preview);

        var output = GetResultText(result);

        Assert.Contains("environment: production", output);
        Assert.Contains("verbose: True", output);
    }

    [Fact]
    public async Task AFormOpenedFromAnAliasRunsWithTheValuesTheAliasSupplies()
    {
        // arrange
        var options = GetOptions();

        var presets = new Dictionary<string, string>(
            ArgumentCollection.ArgumentNameComparer)
        {
            { "environment", "production" }
        };

        using var form = Open(options, ApplicationConstants.CommandName_Deploy, presets);

        // act
        var result = await GetRunner(options).RunAsync(
            form, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(CommandExecutionStatus.Success, result.Status);
        Assert.Contains("environment: production", GetResultText(result));
    }

    [Fact]
    public async Task AnIncompleteFormFailsValidationRatherThanThrowing()
    {
        // arrange -- the command is the authority on whether its arguments are valid, and it
        // says so in its own words
        var options = GetOptions();

        using var form = Open(options, ApplicationConstants.CommandName_Greeting);

        // act
        var result = await GetRunner(options).RunAsync(
            form, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(CommandExecutionStatus.ValidationFailed, result.Status);
        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.ValidationFailures);

        // the command wrote its own usage and reasons, so there is something to show
        Assert.NotEmpty(result.Output);
    }

    [Fact]
    public async Task ProgressIsReportedWhileTheCommandRuns()
    {
        // arrange
        var options = GetOptions();

        using var form = Open(options, ApplicationConstants.CommandName_Progress);

        form.FindField(SampleProgressCommand.ArgumentName_Count)!.TrySetValue("3");

        var runner = GetRunner(options);
        var seen = new List<CommandProgress>();

        runner.Output.ProgressReported += seen.Add;

        // act
        var result = await runner.RunAsync(form, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(CommandExecutionStatus.Success, result.Status);
        Assert.Contains("Processed 3 items.", GetResultText(result));

        // reports arrive as they happen rather than in a lump at the end
        Assert.Equal(seen.Count, runner.Output.ProgressReports.Count);
        Assert.Contains(seen, x => x.IsMeasured == true && x.Fraction == 1);

        // progress is commentary, so it never lands in the command's result
        Assert.DoesNotContain("Processing item", GetResultText(result));
    }

    [Fact]
    public async Task ACommandThatAsksQuestionsGetsThemAnswered()
    {
        // arrange -- this is what running in process buys: Prompt() goes through the input
        // provider, so a command that asks questions works in here knowing nothing about here
        var options = GetOptions();

        using var form = Open(options, ApplicationConstants.CommandName_Interactive);

        var runner = GetRunner(options);
        var answers = new Queue<string>(["Ann", "y"]);
        var questions = new List<string>();

        runner.Input.Reader = question =>
        {
            questions.Add(question);

            return answers.Count == 0 ? null : answers.Dequeue();
        };

        // act
        var result = await runner.RunAsync(form, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(CommandExecutionStatus.Success, result.Status);
        Assert.Contains("Hello, Ann!", GetResultText(result));

        // the question the command wrote becomes the prompt rather than half a line of output
        Assert.Equal(2, runner.Input.ReadCount);
        Assert.Contains("What is your name?", questions[0]);
        Assert.Contains("Say hello to Ann?", questions[1]);
    }

    [Fact]
    public async Task ACommandWithNothingToAskWithReadsTheEndOfInput()
    {
        // arrange -- no reader means no way to ask, which is what a command sees when its
        // input is a file that has run out
        var options = GetOptions();

        using var form = Open(options, ApplicationConstants.CommandName_Interactive);

        var runner = GetRunner(options);

        runner.Input.Reader = null;

        // act
        var result = await runner.RunAsync(form, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(CommandExecutionStatus.Success, result.Status);
        Assert.Contains("No name supplied.", GetResultText(result));
    }

    [Fact]
    public async Task AFailureTheToolExpectedIsReportedAsAMessage()
    {
        // arrange
        var options = GetOptions();

        using var form = Open(options, ApplicationConstants.CommandName_Throws);

        form.FindField(SampleThrowingCommand.ArgumentName_Expected)!
            .TrySetValue(bool.TrueString);

        // act
        var result = await GetRunner(options).RunAsync(
            form, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(CommandExecutionStatus.Failed, result.Status);
        Assert.Equal("That did not work.", result.Message);
        Assert.Null(result.Exception);

        // what it managed to write before it failed is still worth showing
        Assert.Contains("About to fail.", GetResultText(result));
    }

    [Fact]
    public async Task ACommandThatThrowsCostsTheCommandRatherThanTheInterface()
    {
        // arrange -- deliberately wider than the console entry point, which lets an
        // unexpected exception end the process. Here the process is the interface.
        var options = GetOptions();

        using var form = Open(options, ApplicationConstants.CommandName_Throws);

        // act
        var result = await GetRunner(options).RunAsync(
            form, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(CommandExecutionStatus.Failed, result.Status);
        Assert.Contains("Something nobody planned for.", result.Message);
        Assert.IsType<InvalidOperationException>(result.Exception);
    }

    [Fact]
    public async Task CancellingACommandIsNotTheSameAsFailing()
    {
        // arrange
        var options = GetOptions();

        using var form = Open(options, ApplicationConstants.CommandName_Progress);

        using var cancellation = new CancellationTokenSource();

        await cancellation.CancelAsync();

        // act
        var result = await GetRunner(options).RunAsync(form, cancellation.Token);

        // assert
        Assert.Equal(CommandExecutionStatus.Cancelled, result.Status);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task AnUnknownCommandIsAFailureRatherThanAnException()
    {
        // arrange
        var options = GetOptions();

        // act
        var result = await GetRunner(options).RunAsync(
            ["nothing-is-named-this"], TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(CommandExecutionStatus.Failed, result.Status);
        Assert.Contains("nothing-is-named-this", result.Message);
    }

    [Fact]
    public async Task TheToolsOwnOutputProviderIsLeftAlone()
    {
        // arrange -- a command built with the tool's own options would write to the console
        // underneath the interface, which is why the run swaps the provider rather than the
        // interface reading the console back
        var options = GetOptions();
        var console = (StringBuilderTextOutputProvider)options.OutputProvider;

        using var form = Open(options, ApplicationConstants.CommandName_Greeting);

        form.FindField("name")!.TrySetValue("Ann");

        // act
        var result = await GetRunner(options).RunAsync(
            form, TestContext.Current.CancellationToken);

        // assert
        Assert.Contains("Hello, Ann!", GetResultText(result));
        Assert.Empty(console.GetOutput());
    }

    [Fact]
    public async Task TheToolsRegistryAndServicesAreSharedRatherThanRebuilt()
    {
        // arrange -- the options the run uses forward these two rather than copying them. A
        // second service provider would mean singletons that are not.
        var options = GetOptions();

        var session = TuiSession.Create(options, typeof(SampleCommand1).Assembly);

        var registry = options.CommandRegistry;

        Assert.NotNull(registry);

        using var form = Open(options, ApplicationConstants.CommandName_Greeting);

        form.FindField("name")!.TrySetValue("Ann");

        // act
        await GetRunner(options).RunAsync(form, TestContext.Current.CancellationToken);

        // assert
        Assert.Same(registry, options.CommandRegistry);
        Assert.Same(registry, session.Registry);
    }

    [Fact]
    public async Task EachRunStartsWithAnEmptyPane()
    {
        // arrange
        var options = GetOptions();

        using var form = Open(options, ApplicationConstants.CommandName_Greeting);

        var runner = GetRunner(options);

        form.FindField("name")!.TrySetValue("Ann");

        await runner.RunAsync(form, TestContext.Current.CancellationToken);

        // act
        form.FindField("name")!.TrySetValue("Bob");

        var result = await runner.RunAsync(form, TestContext.Current.CancellationToken);

        // assert
        Assert.Contains("Hello, Bob!", GetResultText(result));
        Assert.DoesNotContain("Hello, Ann!", GetResultText(result));
    }

    [Fact]
    public async Task NothingAboutARunAssignsAProcessExitCode()
    {
        // arrange -- the interface outlives any one command, which is the entire reason a
        // command reports a result instead of assigning Environment.ExitCode as a side effect
        var options = GetOptions();
        var before = Environment.ExitCode;

        using var form = Open(options, ApplicationConstants.CommandName_Throws);

        // act
        var result = await GetRunner(options).RunAsync(
            form, TestContext.Current.CancellationToken);

        // assert
        Assert.False(result.IsSuccess);
        Assert.Equal(before, Environment.ExitCode);
    }

    [Fact]
    public async Task RunningWithNoCommandIsARefusalRatherThanAGuess()
    {
        // arrange
        var options = GetOptions();

        // act and assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => GetRunner(options).RunAsync([], TestContext.Current.CancellationToken));
    }
}
