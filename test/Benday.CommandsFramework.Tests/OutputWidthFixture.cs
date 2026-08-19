using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

/// <summary>
/// Tests that usage text is wrapped against the width of wherever the output is going, not
/// against the console window. Inside a pane of a terminal UI, or in a web page, the console
/// window's width is the wrong number -- and reading it from a process with no console throws.
/// </summary>
public class OutputWidthFixture
{
    /// <summary>
    /// A provider that reports a width but does not implement the member itself, standing in
    /// for one written before Width existed.
    /// </summary>
    private class LegacyOutputProvider : ITextOutputProvider
    {
        public void WriteLine(string line) { }

        public void WriteLine() { }

        public void Write(string message) { }
    }

    private static string GetUsage(int width)
    {
        var output = new StringBuilderTextOutputProvider { Width = width };

        var executionInfo = new ArgumentCollectionFactory().Parse(
            Utilities.GetStringArray(
                ApplicationConstants.CommandName_Command3,
                ArgumentFrameworkConstants.ArgumentHelpString));

        var command = new SampleCommand3(executionInfo, output);

        command.ExecuteAsync(TestContext.Current.CancellationToken).GetAwaiter().GetResult();

        return output.GetOutput();
    }

    private static int LongestLineLength(string text)
    {
        return text
            .Split(Environment.NewLine)
            .Select(x => x.TrimEnd().Length)
            .DefaultIfEmpty(0)
            .Max();
    }

    [Fact]
    public void UsageText_WrapsAtTheProvidersWidth()
    {
        // arrange -- SampleCommand3 has a deliberately long argument description
        var narrow = GetUsage(60);
        var wide = GetUsage(200);

        // assert
        Assert.True(
            LongestLineLength(narrow) <= 60,
            $"longest narrow line was {LongestLineLength(narrow)}");

        Assert.True(
            LongestLineLength(wide) > LongestLineLength(narrow),
            "a wider provider should produce longer lines");
    }

    [Fact]
    public void StringBuilderProvider_DefaultsToTheFrameworkDefault()
    {
        // arrange
        var provider = new StringBuilderTextOutputProvider();

        // assert
        Assert.Equal(CommandFrameworkConstants.DefaultOutputWidth, provider.Width);
    }

    [Fact]
    public void LegacyProvider_ReportsTheDefaultWidth()
    {
        // arrange -- Width is a default interface member, so a provider written before it
        // existed keeps working and reports the width the framework used to assume
        ITextOutputProvider provider = new LegacyOutputProvider();

        // assert
        Assert.Equal(CommandFrameworkConstants.DefaultOutputWidth, provider.Width);
    }

    [Fact]
    public async Task ProgramUsage_WrapsAtTheProvidersWidth()
    {
        // arrange
        var output = new StringBuilderTextOutputProvider { Width = 50 };

        var options = new DefaultProgramOptions
        {
            ApplicationName = "Test Sample Application",
            ConfigurationFolderName = "TestSampleApplication-Deleteable",
            OutputProvider = output,
            UsesConfiguration = false
        };

        var program = new DefaultProgram(options, typeof(SampleCommand1).Assembly);

        // act
        await program.RunAsync([], TestContext.Current.CancellationToken);

        // assert -- the command list is padded to the longest command name, so lines can be
        // longer than the width; what matters is that the width is what drives the wrapping
        var narrow = LongestLineLength(output.GetOutput());

        var wideOutput = new StringBuilderTextOutputProvider { Width = 200 };

        options.OutputProvider = wideOutput;

        var wideProgram = new DefaultProgram(options, typeof(SampleCommand1).Assembly);

        await wideProgram.RunAsync([], TestContext.Current.CancellationToken);

        Assert.True(
            LongestLineLength(wideOutput.GetOutput()) > narrow,
            "a wider provider should produce longer lines");
    }
}
