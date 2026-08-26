using Benday.CommandsFramework;
using Benday.CommandsFramework.Samples;
using Benday.CommandsFramework.Tui.Model;

using Spectre.Console;
using Spectre.Console.Testing;

namespace Benday.CommandsFramework.Tui.Tests;

/// <summary>
/// Tests the rendering layer against Spectre's TestConsole, so none of this needs a
/// terminal.
/// </summary>
public class SpectreTuiHostFixture
{
    private static DefaultProgramOptions GetOptions()
    {
        return new DefaultProgramOptions
        {
            ApplicationName = "Sample Tool",
            Version = "9.9.9",
            Website = "https://www.benday.com",
            OutputProvider = new StringBuilderTextOutputProvider(),
            UsesConfiguration = false
        };
    }

    private static TestConsole GetConsole()
    {
        // wide enough that the assertions are about content rather than about wrapping
        var console = new TestConsole();

        console.Profile.Width = 120;

        return console;
    }

    private static DefaultProgram GetProgram()
    {
        return new DefaultProgram(GetOptions(), typeof(SampleCommand1).Assembly);
    }

    [Fact]
    public async Task RunAsync_OpensAndExitsCleanly()
    {
        // arrange
        var console = GetConsole();
        var host = new SpectreTuiHost(console);

        // act
        var exitCode = await host.RunAsync(GetProgram(), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(CommandFrameworkConstants.ExitCode_Success, exitCode);
    }

    [Fact]
    public async Task RunAsync_ShowsWhichToolItIsShowing()
    {
        // arrange
        var console = GetConsole();
        var host = new SpectreTuiHost(console);

        // act
        await host.RunAsync(GetProgram(), TestContext.Current.CancellationToken);

        // assert
        var text = console.Output;

        Assert.Contains("Sample Tool", text);
        Assert.Contains("9.9.9", text);
        Assert.Contains("https://www.benday.com", text);
    }

    [Fact]
    public async Task RunAsync_ShowsHowManyCommandsTheToolHas()
    {
        // arrange
        var console = GetConsole();
        var host = new SpectreTuiHost(console);
        var session = TuiSession.Create(GetOptions(), typeof(SampleCommand1).Assembly);

        // act
        await host.RunAsync(session, TestContext.Current.CancellationToken);

        // assert
        Assert.Contains($"Commands: {session.CommandCount}", console.Output);
    }

    [Fact]
    public async Task RunAsync_OnANonInteractiveTerminal_DoesNotWaitForAKeyPress()
    {
        // arrange -- a TestConsole is not interactive, and neither is a redirected one. There
        // is no key press coming, so waiting for one would hang the tool forever.
        var console = GetConsole();

        Assert.False(console.Profile.Capabilities.Interactive);

        var host = new SpectreTuiHost(console);

        // act
        var exitCode = await host.RunAsync(GetProgram(), TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(CommandFrameworkConstants.ExitCode_Success, exitCode);
        Assert.DoesNotContain("Press any key", console.Output);
    }

    [Fact]
    public async Task RunAsync_OnAnInteractiveTerminal_WaitsForAKeyPress()
    {
        // arrange
        var console = GetConsole();

        console.Interactive();
        console.Input.PushKey(ConsoleKey.Q);

        var host = new SpectreTuiHost(console);

        // act
        var exitCode = await host.RunAsync(GetProgram(), TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(CommandFrameworkConstants.ExitCode_Success, exitCode);
        Assert.Contains("Press any key", console.Output);
    }

    [Fact]
    public async Task RunAsync_WhenAlreadyCancelled_ExitsWithoutRenderingAnything()
    {
        // arrange
        var console = GetConsole();
        var host = new SpectreTuiHost(console);

        using var cts = new CancellationTokenSource();

        await cts.CancelAsync();

        // act -- being cancelled out of the interface is not a failure of the tool
        var exitCode = await host.RunAsync(GetProgram(), cts.Token);

        // assert
        Assert.Equal(CommandFrameworkConstants.ExitCode_Success, exitCode);
        Assert.DoesNotContain("Sample Tool", console.Output);
    }

    [Fact]
    public async Task RunAsync_ShowsWhatMakesACommandUnreachable()
    {
        // arrange -- a problem is otherwise only visible to a tool author who thought to
        // assert on CommandRegistry.Problems from a unit test
        var options = GetOptions();

        options.CommandRegistry =
            CommandRegistry.BuildFromTypes(
                [typeof(ShadowedAliasCommand), typeof(ShadowingCommand)],
                primaryAssembly: typeof(SampleCommand1).Assembly);

        var console = GetConsole();
        var host = new SpectreTuiHost(console);

        var session = TuiSession.Create(options, typeof(SampleCommand1).Assembly);

        // act
        await host.RunAsync(session, TestContext.Current.CancellationToken);

        // assert
        Assert.True(session.HasProblems);
        Assert.Contains("Problems", console.Output);
        Assert.Contains("shadowed", console.Output);
    }

    [Fact]
    public async Task RunAsync_RefusesNothingToShow()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => new SpectreTuiHost(GetConsole()).RunAsync(
                (ICommandProgram)null!, TestContext.Current.CancellationToken));
    }

    [Command(Name = "alpha", Aliases = ["shadowed"],
        Description = "Its alias is also the name of another command")]
    private class ShadowedAliasCommand : Command
    {
        public ShadowedAliasCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider)
            : base(info, outputProvider)
        {
        }

        public override ArgumentCollection GetArguments() => new();

        protected override Task OnExecute(CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    [Command(Name = "shadowed", Description = "Wins over the alias of the same name")]
    private class ShadowingCommand : Command
    {
        public ShadowingCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider)
            : base(info, outputProvider)
        {
        }

        public override ArgumentCollection GetArguments() => new();

        protected override Task OnExecute(CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
