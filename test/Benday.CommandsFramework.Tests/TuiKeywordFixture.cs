using System.Reflection;

using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

/// <summary>
/// Tests the framework side of the terminal interface: the keyword is reserved, a command
/// cannot claim it, and the program dispatches to whatever host was configured.
/// </summary>
[Collection(ProcessExitCodeCollection.Name)]
public class TuiKeywordFixture
{
    /// <summary>
    /// A stand-in for the real interface, so the dispatch can be tested without a terminal
    /// or a reference to the TUI package.
    /// </summary>
    private class FakeTuiHost : ITuiHost
    {
        public FakeTuiHost(int exitCode = CommandFrameworkConstants.ExitCode_Success)
        {
            ExitCode = exitCode;
        }

        public int ExitCode { get; }

        public int RunCount { get; private set; }

        public ICommandProgram? ProgramSeen { get; private set; }

        public CancellationToken TokenSeen { get; private set; }

        public Task<int> RunAsync(
            ICommandProgram program, CancellationToken cancellationToken = default)
        {
            RunCount++;
            ProgramSeen = program;
            TokenSeen = cancellationToken;

            return Task.FromResult(ExitCode);
        }
    }

    private static (DefaultProgram Program, StringBuilderTextOutputProvider Output,
        DefaultProgramOptions Options) GetSystemUnderTest(ITuiHost? host)
    {
        var output = new StringBuilderTextOutputProvider();

        var options = new DefaultProgramOptions
        {
            ApplicationName = "tui-keyword-test",
            OutputProvider = output,
            UsesConfiguration = false,
            TuiHost = host
        };

        return (new DefaultProgram(options, typeof(SampleCommand1).Assembly), output, options);
    }

    [Fact]
    public void TuiIsAReservedKeyword()
    {
        // assert -- one source, so usage output, completion and the registry's collision
        // check cannot disagree about whether the name is taken
        Assert.Contains(ArgumentFrameworkConstants.ArgumentTui, ReservedKeywords.AllNames);

        Assert.Contains(
            ReservedKeywords.ForPrograms,
            x => x.Name == ArgumentFrameworkConstants.ArgumentTui);
    }

    [Fact]
    public void TuiIsTypedAsABareWordRatherThanAsAnArgument()
    {
        // arrange
        var keyword = Assert.Single(
            ReservedKeywords.ForPrograms,
            x => x.Name == ArgumentFrameworkConstants.ArgumentTui);

        // assert -- 'tui' is a command like 'gui' and 'completion', not an argument, so it is
        // typed as it is under every syntax
        Assert.False(keyword.IsArgument);
        Assert.Equal("tui", keyword.GetDisplayName(ArgumentSyntax.Posix));
        Assert.Equal("tui", keyword.GetDisplayName(ArgumentSyntax.Slash));
    }

    [Fact]
    public void CommandRegistryProblems_FlagsACommandThatClaimsTheTuiName()
    {
        // act
        var registry = CommandRegistry.BuildFromTypes([typeof(TuiNameCollisionCommand)]);

        // assert -- one bad command does not stop the others, so this is a problem rather
        // than an exception
        var problem = Assert.Single(registry.Problems);

        Assert.Contains(
            "Command name 'tui' is a reserved framework keyword", problem);
    }

    [Fact]
    public void GetCommandNameProblems_FlagsACommandThatClaimsTheTuiName()
    {
        // arrange
        var util = new CommandAttributeUtility(new DefaultProgramOptions());

        var attributes = new List<CommandAttribute>
        {
            new() { Name = ArgumentFrameworkConstants.ArgumentTui }
        };

        // act
        var problems = util.GetCommandNameProblems(attributes);

        // assert
        var problem = Assert.Single(problems);

        Assert.Contains("Command name 'tui' is a reserved framework keyword", problem);
    }

    [Fact]
    public async Task Tui_WithNoHostConfigured_SaysWhatTheToolAuthorHasToDo()
    {
        // arrange
        var (program, output, _) = GetSystemUnderTest(host: null);

        // act
        var exitCode = await program.RunAsync(
            [ArgumentFrameworkConstants.ArgumentTui], TestContext.Current.CancellationToken);

        // assert -- unlike 'gui' there is nothing to offer to install, because TUI support is
        // a compile time reference
        Assert.Equal(CommandFrameworkConstants.ExitCode_Failure, exitCode);

        var text = output.GetErrorOutput();

        Assert.Contains("Benday.CommandsFramework.Tui", text);
        Assert.Contains("WithTui()", text);

        // the message is an error, so it stays out of anything the tool's result is piped to
        Assert.Empty(output.GetResultOutput());
    }

    [Fact]
    public async Task Tui_RunsTheConfiguredHostAndReturnsWhatItSays()
    {
        // arrange
        var host = new FakeTuiHost(exitCode: 42);
        var (program, _, _) = GetSystemUnderTest(host);

        // act
        var exitCode = await program.RunAsync(
            [ArgumentFrameworkConstants.ArgumentTui], TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(42, exitCode);
        Assert.Equal(1, host.RunCount);
        Assert.Same(program, host.ProgramSeen);
    }

    [Fact]
    public async Task Tui_HandsTheHostTheCancellationToken()
    {
        // arrange
        var host = new FakeTuiHost();
        var (program, _, _) = GetSystemUnderTest(host);

        using var cts = new CancellationTokenSource();

        // act
        await program.RunAsync([ArgumentFrameworkConstants.ArgumentTui], cts.Token);

        // assert -- the interface has to be stoppable
        Assert.Equal(cts.Token, host.TokenSeen);
    }

    [Fact]
    public async Task Tui_IsListedInTheToolsUsageOutput()
    {
        // arrange
        var (program, output, _) = GetSystemUnderTest(host: null);

        // act
        await program.RunAsync([], TestContext.Current.CancellationToken);

        // assert
        var text = output.GetOutput();

        Assert.Contains("Also available:", text);
        Assert.Contains(ArgumentFrameworkConstants.ArgumentTui, text);
    }

    [Fact]
    public void TuiHost_DefaultsToNullOnAnOptionsImplementationThatPredatesIt()
    {
        // arrange
        ICommandProgramOptions options = new OptionsWithoutTuiSupport();

        // assert -- a default interface member, so adding it broke no implementor
        Assert.Null(options.TuiHost);
    }

    /// <summary>
    /// An implementation of the options interface that says nothing about a terminal
    /// interface, standing in for one written before the member existed.
    /// </summary>
    private class OptionsWithoutTuiSupport : ICommandProgramOptions
    {
        public string ApplicationName { get; set; } = string.Empty;
        public DisplayUsageOptions DisplayUsageOptions { get; set; } = new();
        public string Version { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string ConfigurationFolderName { get; set; } = string.Empty;
        public bool UsesConfiguration { get; set; }
        public ITextOutputProvider OutputProvider { get; set; } = new StringBuilderTextOutputProvider();
        public ITextInputProvider InputProvider { get; set; } = new QueuedTextInputProvider();
        public CommandRegistry? CommandRegistry { get; set; }
        public Microsoft.Extensions.DependencyInjection.IServiceCollection? ServiceCollection { get; set; }
        public IServiceProvider? ServiceProvider { get; set; }
        public bool StrictArgumentValidation { get; set; }
    }

    /// <summary>
    /// A command that tries to claim the reserved 'tui' name. It can never run, which is the
    /// point -- the registry reports it rather than leaving the tool author to find out.
    /// </summary>
    /// <remarks>
    /// Nested and private so that nothing sweeping the test assembly for commands finds it.
    /// </remarks>
    [Command(Name = "tui", Description = "Never reachable, because 'tui' is a framework keyword")]
    private class TuiNameCollisionCommand : Command
    {
        public TuiNameCollisionCommand(
            CommandExecutionInfo info, ITextOutputProvider outputProvider)
            : base(info, outputProvider)
        {
        }

        public override ArgumentCollection GetArguments() => new();

        protected override Task OnExecute(CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
