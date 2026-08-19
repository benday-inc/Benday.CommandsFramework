using System.Text;

using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

/// <summary>
/// Tests for the three output channels. One undifferentiated channel meant a command that
/// grew a /json flag emitted invalid JSON the moment anything else wrote a message, and a
/// failing command wrote its error message into the payload.
/// </summary>
[Collection(ProcessExitCodeCollection.Name)]
public class OutputChannelFixture
{
    private const string ResultText = "this is the result";
    private const string StatusText = "this is a status message";
    private const string ErrorText = "this is an error";

    /// <summary>
    /// An ITextOutputProvider written before the channels existed -- it implements only the
    /// three original members.
    /// </summary>
    private class LegacyOutputProvider : ITextOutputProvider
    {
        public StringBuilder Everything { get; } = new();

        public void WriteLine(string line) => Everything.AppendLine(line);

        public void WriteLine() => Everything.AppendLine();

        public void Write(string message) => Everything.Append(message);
    }

    [Command(Name = "channel-sample", Description = "Writes to all three channels")]
    private class ChannelSampleCommand : Command
    {
        public ChannelSampleCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider)
            : base(info, outputProvider)
        {
        }

        public override ArgumentCollection GetArguments() => new();

        protected override Task OnExecute(CancellationToken cancellationToken)
        {
            WriteLine(ResultText);
            WriteStatus(StatusText);
            WriteError(ErrorText);

            return Task.CompletedTask;
        }
    }

    private static ChannelSampleCommand GetCommand(
        ITextOutputProvider output, params string[] args)
    {
        var executionInfo = new ArgumentCollectionFactory().Parse(
            Utilities.GetStringArray(new[] { "channel-sample" }.Concat(args).ToArray()));

        return new ChannelSampleCommand(executionInfo, output);
    }

    [Fact]
    public async Task StringBuilderProvider_KeepsTheChannelsApart()
    {
        // arrange
        var output = new StringBuilderTextOutputProvider();

        await // act
        GetCommand(output).ExecuteAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Contains(ResultText, output.GetResultOutput());
        Assert.DoesNotContain(StatusText, output.GetResultOutput());
        Assert.DoesNotContain(ErrorText, output.GetResultOutput());

        Assert.Contains(StatusText, output.GetStatusOutput());
        Assert.Contains(ErrorText, output.GetErrorOutput());
    }

    [Fact]
    public async Task StringBuilderProvider_GetOutputStillReturnsEverything()
    {
        // arrange -- GetOutput() is what every existing test uses, so it keeps meaning
        // "everything, in the order it was written"
        var output = new StringBuilderTextOutputProvider();

        await // act
        GetCommand(output).ExecuteAsync(TestContext.Current.CancellationToken);

        // assert
        var all = output.GetOutput();

        Assert.Contains(ResultText, all);
        Assert.Contains(StatusText, all);
        Assert.Contains(ErrorText, all);
    }

    [Fact]
    public async Task LegacyProvider_StillGetsEverythingOnOneChannel()
    {
        // arrange -- WriteStatus() and WriteError() are default interface members that fall
        // back to WriteLine(), so a provider written before the split keeps working
        var output = new LegacyOutputProvider();

        await // act
        GetCommand(output).ExecuteAsync(TestContext.Current.CancellationToken);

        // assert
        var all = output.Everything.ToString();

        Assert.Contains(ResultText, all);
        Assert.Contains(StatusText, all);
        Assert.Contains(ErrorText, all);
    }

    [Fact]
    public async Task QuietMode_SuppressesStatusButNeverErrors()
    {
        // arrange
        var output = new StringBuilderTextOutputProvider();

        await // act
        GetCommand(output, $"/{CommandFrameworkConstants.CommandArgName_QuietMode}").ExecuteAsync(TestContext.Current.CancellationToken);

        // assert -- silencing the chatter must never silence a failure
        Assert.DoesNotContain(StatusText, output.GetOutput());
        Assert.Contains(ErrorText, output.GetErrorOutput());
    }

    [Fact]
    public void ConsoleProvider_SendsStatusAndErrorsToStandardError()
    {
        // arrange
        var originalOut = Console.Out;
        var originalError = Console.Error;

        var stdout = new StringWriter();
        var stderr = new StringWriter();

        try
        {
            Console.SetOut(stdout);
            Console.SetError(stderr);

            var provider = new ConsoleTextOutputProvider();

            // act
            provider.WriteLine(ResultText);
            provider.WriteStatus(StatusText);
            provider.WriteError(ErrorText);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }

        // assert
        Assert.Contains(ResultText, stdout.ToString());
        Assert.DoesNotContain(StatusText, stdout.ToString());
        Assert.DoesNotContain(ErrorText, stdout.ToString());

        Assert.Contains(StatusText, stderr.ToString());
        Assert.Contains(ErrorText, stderr.ToString());
    }

    [Fact]
    public async Task FailedCommand_KeepsItsErrorOutOfTheResult()
    {
        // arrange -- this is the live bug: a failed command piping --json to a file used to
        // land its error text inside the JSON
        var output = new StringBuilderTextOutputProvider();

        var options = new DefaultProgramOptions
        {
            ApplicationName = "Test Sample Application",
            ConfigurationFolderName = "TestSampleApplication-Deleteable",
            OutputProvider = output,
            UsesConfiguration = false
        };

        var program = new DefaultProgram(
            options, typeof(SampleCommand1).Assembly);

        var originalExitCode = Environment.ExitCode;

        try
        {
            await // act
            program.RunAsync(["no-such-command"], TestContext.Current.CancellationToken);
        }
        finally
        {
            Environment.ExitCode = originalExitCode;
        }

        // assert
        Assert.Contains("no-such-command", output.GetErrorOutput());
        Assert.Empty(output.GetResultOutput());
    }

    [Fact]
    public async Task CommandUsage_ListsTheReservedNames()
    {
        // arrange -- usage lists only a command's own arguments, so quiet and --help used to
        // be undiscoverable
        var output = new StringBuilderTextOutputProvider();

        var executionInfo = new ArgumentCollectionFactory().Parse(
            Utilities.GetStringArray(
                ApplicationConstants.CommandName_CommandWithAllowedValues,
                ArgumentFrameworkConstants.ArgumentHelpString));

        var command = new SampleCommandWithAllowedValues(executionInfo, output);

        await // act
        command.ExecuteAsync(TestContext.Current.CancellationToken);

        // assert
        var text = output.GetOutput();

        Assert.Contains("** ALSO AVAILABLE **", text);
        Assert.Contains(ArgumentFrameworkConstants.ArgumentHelpString, text);
        Assert.Contains(CommandFrameworkConstants.CommandArgName_QuietMode, text);
    }

    [Fact]
    public async Task ProgramUsage_ListsTheReservedNames()
    {
        // arrange
        var output = new StringBuilderTextOutputProvider();

        var options = new DefaultProgramOptions
        {
            ApplicationName = "Test Sample Application",
            ConfigurationFolderName = "TestSampleApplication-Deleteable",
            OutputProvider = output,
            UsesConfiguration = false
        };

        var program = new DefaultProgram(options, typeof(SampleCommand1).Assembly);

        var originalExitCode = Environment.ExitCode;

        try
        {
            await // act
            program.RunAsync([], TestContext.Current.CancellationToken);
        }
        finally
        {
            Environment.ExitCode = originalExitCode;
        }

        // assert
        var text = output.GetOutput();

        Assert.Contains("Also available:", text);
        Assert.Contains(ArgumentFrameworkConstants.ArgumentJson, text);
        Assert.Contains(ArgumentFrameworkConstants.ArgumentGui, text);
    }

    [Fact]
    public void ReservedKeywords_AreTheSameListArgumentValidationSkips()
    {
        // assert -- one source, so usage output and validation cannot drift apart
        Assert.Contains(ArgumentFrameworkConstants.ArgumentHelpString, ReservedKeywords.AllNames);
        Assert.Contains(ArgumentFrameworkConstants.ArgumentJson, ReservedKeywords.AllNames);
        Assert.Contains(ArgumentFrameworkConstants.ArgumentGui, ReservedKeywords.AllNames);
        Assert.Contains(CommandFrameworkConstants.CommandArgName_QuietMode, ReservedKeywords.AllNames);

        Assert.Contains(ArgumentFrameworkConstants.CommandCompletion, ReservedKeywords.AllNames);

        // --complete is reserved but deliberately not listed in usage output: it exists for
        // shell completion stubs rather than for people
        Assert.Contains(ArgumentFrameworkConstants.ArgumentComplete, ReservedKeywords.AllNames);
        Assert.DoesNotContain(
            ReservedKeywords.ForPrograms,
            x => x.Name == ArgumentFrameworkConstants.ArgumentComplete);

        // --help is on both lists but is only counted once
        Assert.Equal(6, ReservedKeywords.AllNames.Count);
    }
}
