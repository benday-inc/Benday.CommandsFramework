using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

/// <summary>
/// Tests for the input side of the framework. Before ITextInputProvider existed, a command
/// that prompted had to call Console.ReadLine() itself, which meant an interactive command
/// could not be tested at all.
/// </summary>
public class TextInputProviderFixture
{
    private static SampleInteractiveCommand GetCommand(
        StringBuilderTextOutputProvider output,
        ITextInputProvider input,
        params string[] args)
    {
        var options = new DefaultProgramOptions
        {
            ApplicationName = "Test Sample Application",
            ConfigurationFolderName = "TestSampleApplication-Deleteable",
            OutputProvider = output,
            InputProvider = input,
            UsesConfiguration = false
        };

        var executionInfo = new ArgumentCollectionFactory().Parse(
            Utilities.GetStringArray(
                new[] { ApplicationConstants.CommandName_Interactive }.Concat(args).ToArray()));

        executionInfo.Options = options;

        return new SampleInteractiveCommand(executionInfo, output);
    }

    [Fact]
    public void QueuedInput_HandsOutLinesInOrder()
    {
        // arrange
        var input = new QueuedTextInputProvider("first", "second");

        // act & assert
        Assert.Equal(2, input.RemainingLineCount);
        Assert.Equal("first", input.ReadLine());
        Assert.Equal("second", input.ReadLine());
        Assert.Equal(2, input.ReadCount);
        Assert.Equal(0, input.RemainingLineCount);
    }

    [Fact]
    public void QueuedInput_ReturnsNullWhenEmpty()
    {
        // arrange -- null is what Console.ReadLine() returns at end of input, so a command
        // that keeps reading sees the same thing it would see from a closed stdin
        var input = new QueuedTextInputProvider();

        // act & assert
        Assert.Null(input.ReadLine());
        Assert.Equal(1, input.ReadCount);
    }

    [Fact]
    public void DefaultOptions_UseTheConsole()
    {
        // arrange
        var options = new DefaultProgramOptions();

        // assert
        Assert.IsType<ConsoleTextInputProvider>(options.InputProvider);
    }

    [Fact]
    public void Command_PromptsForAValueThatWasNotSupplied()
    {
        // arrange
        var output = new StringBuilderTextOutputProvider();
        var input = new QueuedTextInputProvider("Ben", "y");

        var command = GetCommand(output, input);

        // act
        command.Execute();

        // assert
        Assert.Equal("Ben", command.NameUsed);
        Assert.True(command.DidGreet);
        Assert.Contains("What is your name?", output.GetOutput());
        Assert.Contains("Hello, Ben!", output.GetOutput());
        Assert.Equal(2, input.ReadCount);
    }

    [Fact]
    public void Command_DoesNotPromptForAValueThatWasSupplied()
    {
        // arrange
        var output = new StringBuilderTextOutputProvider();
        var input = new QueuedTextInputProvider("y");

        var command = GetCommand(output, input, "/name:Alice");

        // act
        command.Execute();

        // assert
        Assert.Equal("Alice", command.NameUsed);
        Assert.True(command.DidGreet);
        Assert.DoesNotContain("What is your name?", output.GetOutput());
        Assert.Equal(1, input.ReadCount);
    }

    [Fact]
    public void PromptForYesNo_TakesTheDefaultOnAnEmptyAnswer()
    {
        // arrange -- pressing enter accepts the default
        var output = new StringBuilderTextOutputProvider();
        var input = new QueuedTextInputProvider("", "");

        var command = GetCommand(output, input, "/name:Alice");

        // act
        command.Execute();

        // assert
        Assert.True(command.DidGreet);
        Assert.Contains("(Y/n)", output.GetOutput());
    }

    [Fact]
    public void PromptForYesNo_UnderstandsNo()
    {
        // arrange
        var output = new StringBuilderTextOutputProvider();
        var input = new QueuedTextInputProvider("n");

        var command = GetCommand(output, input, "/name:Alice");

        // act
        command.Execute();

        // assert
        Assert.False(command.DidGreet);
        Assert.Contains("Suit yourself.", output.GetOutput());
    }

    [Fact]
    public void Command_HandlesRunningOutOfInput()
    {
        // arrange -- nothing queued at all, which is what a closed stdin looks like
        var output = new StringBuilderTextOutputProvider();
        var input = new QueuedTextInputProvider();

        var command = GetCommand(output, input);

        // act
        command.Execute();

        // assert
        Assert.Equal(string.Empty, command.NameUsed);
        Assert.False(command.DidGreet);
        Assert.Contains("No name supplied.", output.GetOutput());
    }
}
