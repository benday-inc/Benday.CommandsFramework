using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

/// <summary>
/// Tests progress reporting. Progress is commentary about the work rather than the work's
/// result, so it goes to the diagnostic channel -- which is why git clone's progress survives
/// a redirect and does not end up inside the redirected file.
/// </summary>
public class ProgressReportingFixture
{
    private static SampleProgressCommand GetCommand(
        StringBuilderTextOutputProvider output, params string[] args)
    {
        var executionInfo = new ArgumentCollectionFactory().Parse(
            Utilities.GetStringArray(
                new[] { ApplicationConstants.CommandName_Progress }.Concat(args).ToArray()));

        return new SampleProgressCommand(executionInfo, output);
    }

    [Fact]
    public async Task Progress_StaysOutOfTheResult()
    {
        // arrange
        var output = new StringBuilderTextOutputProvider();

        using var command = GetCommand(output, "/count:3");

        // act
        await command.ExecuteAsync(TestContext.Current.CancellationToken);

        // assert -- 'mytool progress > out.txt' captures the result and nothing else
        Assert.Equal($"Processed 3 items.{Environment.NewLine}", output.GetResultOutput());
        Assert.Contains("Processing item 2", output.GetStatusOutput());
    }

    [Fact]
    public async Task Progress_IsRecordedForAssertions()
    {
        // arrange
        var output = new StringBuilderTextOutputProvider();

        using var command = GetCommand(output, "/count:3");

        // act
        await command.ExecuteAsync(TestContext.Current.CancellationToken);

        // assert -- a test asserts on what was reported, not on how it was drawn
        Assert.Equal(4, output.ProgressReports.Count);

        var first = output.ProgressReports[0];

        Assert.Equal("Starting", first.Message);
        Assert.False(first.IsMeasured);
        Assert.Null(first.Fraction);

        var last = output.ProgressReports[^1];

        Assert.True(last.IsMeasured);
        Assert.Equal(3, last.Current);
        Assert.Equal(3, last.Total);
        Assert.Equal(1.0, last.Fraction);
    }

    [Fact]
    public async Task Progress_IsSuppressedInQuietMode()
    {
        // arrange -- progress is commentary, and quiet mode silences commentary
        var output = new StringBuilderTextOutputProvider();

        using var command = GetCommand(
            output, "/count:3", $"/{CommandFrameworkConstants.CommandArgName_QuietMode}");

        // act
        await command.ExecuteAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(output.ProgressReports);
    }

    [Theory]
    [InlineData(0, 10, "0%")]
    [InlineData(5, 10, "50%")]
    [InlineData(10, 10, "100%")]
    public void Report_FormatsAsAProportionWhenBothNumbersAreKnown(
        int current, int total, string expected)
    {
        // arrange
        var progress = new CommandProgress("Working", current, total);

        // assert
        Assert.Contains(expected, progress.ToString());
        Assert.Contains($"{current}/{total}", progress.ToString());
    }

    [Fact]
    public void Report_IsJustTheMessageWhenTheWorkIsNotCountable()
    {
        // arrange
        var progress = new CommandProgress("Connecting");

        // assert
        Assert.Equal("Connecting", progress.ToString());
        Assert.False(progress.IsMeasured);
    }

    [Fact]
    public void Report_HandlesATotalOfZeroWithoutDividingByIt()
    {
        // arrange
        var progress = new CommandProgress("Nothing to do", 0, 0);

        // assert
        Assert.False(progress.IsMeasured);
        Assert.Null(progress.Fraction);
        Assert.Equal("Nothing to do", progress.ToString());
    }

    [Fact]
    public void Report_ClampsAFractionThatOvershoots()
    {
        // arrange
        var progress = new CommandProgress("Working", 12, 10);

        // assert
        Assert.Equal(1.0, progress.Fraction);
    }

    [Fact]
    public void ConsoleProvider_DoesNotAnimateWhenStandardErrorIsRedirected()
    {
        // arrange -- the carriage returns would fill the destination with unreadable spam
        var originalError = Console.Error;

        var stderr = new StringWriter();

        try
        {
            Console.SetError(stderr);

            var provider = new ConsoleTextOutputProvider();

            // act
            provider.ReportProgress(new CommandProgress("Working", 1, 2));
            provider.ReportProgress(new CommandProgress("Working", 2, 2));
        }
        finally
        {
            Console.SetError(originalError);
        }

        // assert -- Console.IsErrorRedirected is true under the test runner, so each report
        // is an ordinary status line
        var written = stderr.ToString();

        Assert.DoesNotContain("\r", written);
        Assert.Contains("Working 1/2", written);
        Assert.Contains("Working 2/2", written);
    }

    [Fact]
    public async Task LegacyProvider_GetsProgressAsStatusText()
    {
        // arrange -- ReportProgress is a default interface member that falls back to the
        // status channel, so a provider written before progress existed still shows it
        var output = new StatusOnlyOutputProvider();

        var executionInfo = new ArgumentCollectionFactory().Parse(
            Utilities.GetStringArray(ApplicationConstants.CommandName_Progress, "/count:2"));

        using var command = new SampleProgressCommand(executionInfo, output);

        // act
        await command.ExecuteAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Contains("Processing item 2 2/2 (100%)", output.Lines);
    }

    /// <summary>
    /// A provider that implements only the three original members.
    /// </summary>
    private class StatusOnlyOutputProvider : ITextOutputProvider
    {
        public List<string> Lines { get; } = new();

        public void WriteLine(string line) => Lines.Add(line);

        public void WriteLine() => Lines.Add(string.Empty);

        public void Write(string message) => Lines.Add(message);
    }
}
