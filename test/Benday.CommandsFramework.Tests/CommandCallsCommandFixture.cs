using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

/// <summary>
/// Tests for running a command from inside another command so that command logic can be
/// reused without shelling out to the command line.
/// </summary>
public class CommandCallsCommandFixture
{
    private StringBuilderTextOutputProvider? _OutputProvider;

    private StringBuilderTextOutputProvider OutputProvider
    {
        get
        {
            _OutputProvider ??= new StringBuilderTextOutputProvider();

            return _OutputProvider;
        }
    }

    private static CommandExecutionInfo GetExecutionInfo(params string[] commandLineArgs)
    {
        return new ArgumentCollectionFactory().Parse(commandLineArgs);
    }

    [Fact]
    public async Task CallingCommandGetsResultsBackFromTheCommandItRan()
    {
        // arrange
        var executionInfo = GetExecutionInfo(
            ApplicationConstants.CommandName_CallsOtherCommands,
            "/names:Alice,Bob");

        var systemUnderTest = new SampleCommandThatCallsOtherCommands(executionInfo, OutputProvider);

        await // act
        systemUnderTest.ExecuteAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(
            new[] { "Hello, Alice!", "Hello, Bob!" },
            systemUnderTest.Greetings);
    }

    [Fact]
    public async Task ArgumentsPassedToTheCommandAreUsed()
    {
        // arrange
        var executionInfo = GetExecutionInfo(
            ApplicationConstants.CommandName_CallsOtherCommands,
            "/names:Alice",
            "/salutation:Howdy");

        var systemUnderTest = new SampleCommandThatCallsOtherCommands(executionInfo, OutputProvider);

        await // act
        systemUnderTest.ExecuteAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(new[] { "Howdy, Alice!" }, systemUnderTest.Greetings);
    }

    [Fact]
    public async Task DefaultValuesStillApplyToTheCommandThatGetsRun()
    {
        // arrange
        // no salutation is supplied, so the greeting command's default is used
        var executionInfo = GetExecutionInfo(
            ApplicationConstants.CommandName_CallsOtherCommands,
            "/names:Alice");

        var systemUnderTest = new SampleCommandThatCallsOtherCommands(executionInfo, OutputProvider);

        await // act
        systemUnderTest.ExecuteAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(new[] { "Hello, Alice!" }, systemUnderTest.Greetings);
    }

    [Fact]
    public async Task CommandThatGetsRunIsQuietByDefault()
    {
        // arrange
        var executionInfo = GetExecutionInfo(
            ApplicationConstants.CommandName_CallsOtherCommands,
            "/names:Alice");

        var systemUnderTest = new SampleCommandThatCallsOtherCommands(executionInfo, OutputProvider);

        await // act
        systemUnderTest.ExecuteAsync(TestContext.Current.CancellationToken);

        // assert
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);

        // the greeting shows up once, from the calling command's own summary, rather
        // than twice
        var occurrences = output.Split("Hello, Alice!").Length - 1;

        Assert.Equal(1, occurrences);
    }

    [Fact]
    public async Task OutputFromTheCommandGoesToTheCallingCommandsOutputProvider()
    {
        // arrange
        var executionInfo = GetExecutionInfo(
            ApplicationConstants.CommandName_Greeting,
            "/name:Alice");

        var systemUnderTest = new SampleGreetingCommand(executionInfo, OutputProvider);

        await // act
        systemUnderTest.ExecuteAsync(TestContext.Current.CancellationToken);

        // assert
        // running the command directly is not quiet, so its output lands on the provider
        // that was handed to it
        Assert.Contains("Hello, Alice!", OutputProvider.GetOutput());
    }

    [Fact]
    public async Task ValidationFailureDoesNotPrintUsageForTheCommandThatWasRun()
    {
        // arrange
        var executionInfo = GetExecutionInfo(ApplicationConstants.CommandName_CallsOtherCommandsWithBadArgs);

        var systemUnderTest = new SampleCommandThatCallsOtherCommandsWithBadArgs(
            executionInfo, OutputProvider);

        // act
        await Assert.ThrowsAsync<KnownException>(() => systemUnderTest.ExecuteAsync(TestContext.Current.CancellationToken));

        // assert
        // running a command from the command line prints usage on a validation failure.
        // Running one from inside another command has to throw instead, otherwise the
        // calling command has no way of knowing that it did not run.
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);

        Assert.DoesNotContain("** USAGE **", output);
        Assert.DoesNotContain("** INVALID ARGUMENT", output);
    }

    [Fact]
    public async Task ValidationFailureInTheCommandThrowsKnownException()
    {
        // arrange
        var executionInfo = GetExecutionInfo(ApplicationConstants.CommandName_CallsOtherCommandsWithBadArgs);

        var systemUnderTest = new SampleCommandThatCallsOtherCommandsWithBadArgs(
            executionInfo, OutputProvider);

        // act
        var exception = await Assert.ThrowsAsync<KnownException>(() => systemUnderTest.ExecuteAsync(TestContext.Current.CancellationToken));

        // assert
        Assert.Contains($"Could not run command '{ApplicationConstants.CommandName_Greeting}'", exception.Message);
        Assert.Contains("name", exception.Message);
    }

    [Fact]
    public async Task NestingDepthGuardStopsACommandThatCallsItself()
    {
        // arrange
        var executionInfo = GetExecutionInfo(ApplicationConstants.CommandName_SelfCalling);

        var systemUnderTest = new SampleSelfCallingCommand(executionInfo, OutputProvider);

        // act
        var exception = await Assert.ThrowsAsync<KnownException>(() => systemUnderTest.ExecuteAsync(TestContext.Current.CancellationToken));

        // assert
        Assert.Contains("nested more than", exception.Message);
        Assert.Contains(nameof(SampleSelfCallingCommand), exception.Message);
    }

    [Fact]
    public async Task RunningACommandDoesNotChangeTheProcessExitCode()
    {
        // arrange
        var originalExitCode = Environment.ExitCode;

        try
        {
            Environment.ExitCode = CommandFrameworkConstants.ExitCode_Success;

            var executionInfo = GetExecutionInfo(ApplicationConstants.CommandName_CallsOtherCommandsWithBadArgs);

            var systemUnderTest = new SampleCommandThatCallsOtherCommandsWithBadArgs(
                executionInfo, OutputProvider);

            // act
            await Assert.ThrowsAsync<KnownException>(() => systemUnderTest.ExecuteAsync(TestContext.Current.CancellationToken));

            // assert
            // the command that was run failed validation, but that is the calling
            // command's problem to report -- it must not silently set the exit code
            Assert.Equal(CommandFrameworkConstants.ExitCode_Success, Environment.ExitCode);
        }
        finally
        {
            Environment.ExitCode = originalExitCode;
        }
    }

    [Fact]
    public async Task AsyncCommandCanRunAnotherAsyncCommand()
    {
        // arrange
        var executionInfo = GetExecutionInfo(
            ApplicationConstants.CommandName_AsyncCallsOtherCommands,
            "/name:Alice");

        var systemUnderTest = new SampleAsyncCallerCommand(executionInfo, OutputProvider);

        // act
        await systemUnderTest.ExecuteAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("Hello, Alice!", systemUnderTest.Greeting);
        Assert.Contains("** SUCCESS ** Hello, Alice!", OutputProvider.GetOutput());
    }

    [Fact]
    public async Task CommandTypeWithoutACommandAttributeThrows()
    {
        // arrange
        var executionInfo = GetExecutionInfo(ApplicationConstants.CommandName_CallsATypeWithNoAttribute);

        var systemUnderTest = new SampleCommandThatCallsATypeWithNoAttribute(
            executionInfo, OutputProvider);

        // act
        var exception = await Assert.ThrowsAsync<KnownException>(() => systemUnderTest.ExecuteAsync(TestContext.Current.CancellationToken));

        // assert
        Assert.Contains("does not have a CommandAttribute", exception.Message);
    }
}
