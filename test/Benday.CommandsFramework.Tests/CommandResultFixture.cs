using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

/// <summary>
/// Tests the execution contract: a command reports what happened by returning a result, not
/// by assigning Environment.ExitCode as a side effect, and it can be cancelled.
/// </summary>
public class CommandResultFixture
{
    [Command(Name = "result-sample", Description = "Command used by the execution contract tests")]
    private class ResultSampleCommand : Command
    {
        public ResultSampleCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider)
            : base(info, outputProvider)
        {
        }

        public bool DidRun { get; private set; }

        public CancellationToken TokenSeen { get; private set; }

        public override ArgumentCollection GetArguments()
        {
            var args = new ArgumentCollection();

            args.AddString("required-thing").AsRequired().WithDescription("Has to be supplied");

            return args;
        }

        protected override Task OnExecute(CancellationToken cancellationToken)
        {
            DidRun = true;
            TokenSeen = cancellationToken;

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// A command that keeps working until it is told to stop.
    /// </summary>
    [Command(Name = "long-running-sample", Description = "Runs until cancelled")]
    private class LongRunningCommand : Command
    {
        public LongRunningCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider)
            : base(info, outputProvider)
        {
        }

        public int Iterations { get; private set; }

        public override ArgumentCollection GetArguments() => new();

        protected override async Task OnExecute(CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Iterations++;

                await Task.Yield();
            }
        }
    }

    /// <summary>
    /// A command still deriving from the obsolete shim. Nothing in this repo uses it any
    /// more, so this is what keeps it working for the tools that have not been renamed yet.
    /// </summary>
#pragma warning disable CS0618 // AsynchronousCommand is obsolete, which is the point here
    [Command(Name = "obsolete-base-sample", Description = "Still on the old base class")]
    private class StillOnAsynchronousCommand : AsynchronousCommand
    {
        public StillOnAsynchronousCommand(
            CommandExecutionInfo info, ITextOutputProvider outputProvider)
            : base(info, outputProvider)
        {
        }

        public bool DidRun { get; private set; }

        public override ArgumentCollection GetArguments() => new();

        protected override Task OnExecute(CancellationToken cancellationToken)
        {
            DidRun = true;

            return Task.CompletedTask;
        }
    }
#pragma warning restore CS0618

    [Fact]
    public async Task ObsoleteBaseClass_StillRuns()
    {
        // arrange
        var executionInfo = new ArgumentCollectionFactory().Parse(
            Utilities.GetStringArray("obsolete-base-sample"));

        var command = new StillOnAsynchronousCommand(
            executionInfo, new StringBuilderTextOutputProvider());

        // act
        var actual = await command.ExecuteAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.True(actual.IsSuccess);
        Assert.True(command.DidRun);
        Assert.IsAssignableFrom<Command>(command);
    }

    private static T GetCommand<T>(params string[] args) where T : Command
    {
        var name = typeof(T) == typeof(LongRunningCommand) ? "long-running-sample" : "result-sample";

        var executionInfo = new ArgumentCollectionFactory().Parse(
            Utilities.GetStringArray(new[] { name }.Concat(args).ToArray()));

        var command = Activator.CreateInstance(
            typeof(T), executionInfo, new StringBuilderTextOutputProvider());

        return (T)command!;
    }

    [Fact]
    public async Task Success_ReportsSuccess()
    {
        // act
        var command = GetCommand<ResultSampleCommand>("/required-thing:value");

        var actual = await command.ExecuteAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(CommandExecutionStatus.Success, actual.Status);
        Assert.True(actual.IsSuccess);
        Assert.Equal(CommandFrameworkConstants.ExitCode_Success, actual.ExitCode);
        Assert.True(command.DidRun);
    }

    [Fact]
    public async Task ValidationFailure_ReportsWhichArgumentsFailed()
    {
        // act -- the required argument is missing
        var command = GetCommand<ResultSampleCommand>();

        var actual = await command.ExecuteAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(CommandExecutionStatus.ValidationFailed, actual.Status);
        Assert.False(actual.IsSuccess);
        Assert.Equal(CommandFrameworkConstants.ExitCode_Failure, actual.ExitCode);
        Assert.False(command.DidRun);
        Assert.Contains(actual.InvalidArguments, x => x.Name == "required-thing");
        Assert.Contains("required-thing", actual.Message);
    }

    [Fact]
    public async Task HelpRequest_ReportsUsageDisplayedAndCountsAsSuccess()
    {
        // act -- the user asked for the usage information and got it
        var command = GetCommand<ResultSampleCommand>(ArgumentFrameworkConstants.ArgumentHelpString);

        var actual = await command.ExecuteAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(CommandExecutionStatus.UsageDisplayed, actual.Status);
        Assert.True(actual.IsSuccess);
        Assert.False(command.DidRun);
    }

    [Fact]
    public async Task RunningACommand_DoesNotTouchTheProcessExitCode()
    {
        // arrange -- Validate() used to assign Environment.ExitCode as a side effect, which
        // is fine for one command per process and actively wrong in any longer lived host
        var originalExitCode = Environment.ExitCode;

        Environment.ExitCode = 42;

        try
        {
            // act -- a validation failure, which is the case that used to set it
            var command = GetCommand<ResultSampleCommand>();

            await command.ExecuteAsync(TestContext.Current.CancellationToken);

            // assert
            Assert.Equal(42, Environment.ExitCode);
        }
        finally
        {
            Environment.ExitCode = originalExitCode;
        }
    }

    [Fact]
    public async Task Cancellation_StopsTheCommandAndReportsIt()
    {
        // arrange
        using var cancellation = new CancellationTokenSource();

        var command = GetCommand<LongRunningCommand>();

        var task = command.ExecuteAsync(cancellation.Token);

        // act
        cancellation.Cancel();

        var actual = await task;

        // assert -- cancelling this command does not have to mean stopping the process,
        // which is the whole reason the token exists
        Assert.Equal(CommandExecutionStatus.Cancelled, actual.Status);
        Assert.False(actual.IsSuccess);
        Assert.True(command.Iterations > 0);
    }

    [Fact]
    public async Task Cancellation_BeforeTheCommandStartsMeansItNeverRuns()
    {
        // arrange
        using var cancellation = new CancellationTokenSource();

        cancellation.Cancel();

        var command = GetCommand<ResultSampleCommand>("/required-thing:value");

        // act
        var actual = await command.ExecuteAsync(cancellation.Token);

        // assert
        Assert.Equal(CommandExecutionStatus.Cancelled, actual.Status);
        Assert.False(command.DidRun);
    }

    [Fact]
    public async Task Token_ReachesTheCommand()
    {
        // arrange
        using var cancellation = new CancellationTokenSource();

        var command = GetCommand<ResultSampleCommand>("/required-thing:value");

        // act
        await command.ExecuteAsync(cancellation.Token);

        // assert
        Assert.Equal(cancellation.Token, command.TokenSeen);
    }

    [Fact]
    public async Task Program_ReturnsTheExitCodeInsteadOfSettingIt()
    {
        // arrange
        var options = new DefaultProgramOptions
        {
            ApplicationName = "Test Sample Application",
            ConfigurationFolderName = "TestSampleApplication-Deleteable",
            OutputProvider = new StringBuilderTextOutputProvider(),
            UsesConfiguration = false
        };

        var program = new DefaultProgram(options, typeof(SampleCommand1).Assembly);

        var originalExitCode = Environment.ExitCode;

        Environment.ExitCode = 7;

        try
        {
            // act
            var success = await program.RunAsync(
                [ApplicationConstants.CommandName_Greeting, "/name:World"], TestContext.Current.CancellationToken);

            var failure = await program.RunAsync(["no-such-command"], TestContext.Current.CancellationToken);

            // assert
            Assert.Equal(CommandFrameworkConstants.ExitCode_Success, success);
            Assert.Equal(CommandFrameworkConstants.ExitCode_Failure, failure);
            Assert.Equal(7, Environment.ExitCode);
        }
        finally
        {
            Environment.ExitCode = originalExitCode;
        }
    }

    [Fact]
    public async Task CommandsApp_IsWhereTheProcessExitCodeGetsSet()
    {
        // arrange -- the console entry point is the one place that decides this
        var originalExitCode = Environment.ExitCode;

        Environment.ExitCode = 7;

        try
        {
            // act
            var actual = await CommandsApp
                .Create<SampleCommand1>(["no-such-command"])
                .ConfigureOptions(options =>
                {
                    options.ApplicationName = "Test Sample Application";
                    options.ConfigurationFolderName = "TestSampleApplication-Deleteable";
                    options.OutputProvider = new StringBuilderTextOutputProvider();
                    options.UsesConfiguration = false;
                })
                .RunAsync(TestContext.Current.CancellationToken);

            // assert
            Assert.Equal(CommandFrameworkConstants.ExitCode_Failure, actual);
            Assert.Equal(CommandFrameworkConstants.ExitCode_Failure, Environment.ExitCode);
        }
        finally
        {
            Environment.ExitCode = originalExitCode;
        }
    }
}
