using Benday.CommandsFramework;
using Benday.CommandsFramework.Tui.Model;

namespace Benday.CommandsFramework.Tui.Tests;

/// <summary>
/// Tests where a running command's output goes. The provider collects and announces; nothing
/// here needs a terminal, which is the point of it holding no rendering.
/// </summary>
public class TuiTextOutputProviderFixture
{
    [Fact]
    public void EachChannelIsKeptApart()
    {
        // arrange
        var systemUnderTest = new TuiTextOutputProvider();

        // act
        systemUnderTest.WriteLine("the result");
        systemUnderTest.WriteStatus("commentary");
        systemUnderTest.WriteError("what went wrong");

        // assert
        Assert.Equal(["the result"], systemUnderTest.GetLines(TuiOutputChannel.Result));
        Assert.Equal(["commentary"], systemUnderTest.GetLines(TuiOutputChannel.Status));
        Assert.Equal(["what went wrong"], systemUnderTest.GetLines(TuiOutputChannel.Error));
    }

    [Fact]
    public void EverythingLandsInOnePlaceInWriteOrder()
    {
        // arrange -- one pane, because a user watching a command wants chronology
        var systemUnderTest = new TuiTextOutputProvider();

        // act
        systemUnderTest.WriteStatus("first");
        systemUnderTest.WriteLine("second");
        systemUnderTest.WriteError("third");

        // assert
        Assert.Equal(["first", "second", "third"],
            systemUnderTest.Lines.Select(x => x.Text).ToList());
    }

    [Fact]
    public void APartialLineIsFinishedByTheLineThatEndsIt()
    {
        // arrange -- this is the shape of a prompt: the question is written without a line
        // ending, and the answer's echo finishes it
        var systemUnderTest = new TuiTextOutputProvider();

        // act
        systemUnderTest.Write("Name: ");
        systemUnderTest.WriteLine("Ann");

        // assert
        Assert.Equal(["Name: Ann"], systemUnderTest.Lines.Select(x => x.Text).ToList());
    }

    [Fact]
    public void APartialLineIsNotSwallowedByAnotherChannel()
    {
        // arrange -- an error is not part of the sentence the result was half way through
        var systemUnderTest = new TuiTextOutputProvider();

        // act
        systemUnderTest.Write("halfway");
        systemUnderTest.WriteError("it broke");

        // assert
        Assert.Equal(2, systemUnderTest.Lines.Count);
        Assert.Equal(TuiOutputChannel.Result, systemUnderTest.Lines[0].Channel);
        Assert.Equal("halfway", systemUnderTest.Lines[0].Text);
        Assert.Equal(TuiOutputChannel.Error, systemUnderTest.Lines[1].Channel);
        Assert.Equal("it broke", systemUnderTest.Lines[1].Text);
    }

    [Fact]
    public void TakingThePendingTextIsHowAQuestionBecomesAPrompt()
    {
        // arrange
        var systemUnderTest = new TuiTextOutputProvider();

        systemUnderTest.Write("What is your name? ");

        // act
        var question = systemUnderTest.TakePendingText();

        // assert
        Assert.Equal("What is your name? ", question);
        Assert.False(systemUnderTest.HasPendingText);
        Assert.Empty(systemUnderTest.Lines);
    }

    [Fact]
    public void LinesAreAnnouncedAsTheyAreWritten()
    {
        // arrange -- a display draws each line as it arrives rather than waiting for the end
        var systemUnderTest = new TuiTextOutputProvider();
        var announced = new List<TuiOutputLine>();

        systemUnderTest.LineWritten += announced.Add;

        // act
        systemUnderTest.WriteLine("one");
        systemUnderTest.WriteError("two");

        // assert
        Assert.Equal(["one", "two"], announced.Select(x => x.Text).ToList());
        Assert.Equal(TuiOutputChannel.Error, announced[1].Channel);
    }

    [Fact]
    public void ProgressIsRecordedAndAnnounced()
    {
        // arrange
        var systemUnderTest = new TuiTextOutputProvider();
        var announced = new List<CommandProgress>();

        systemUnderTest.ProgressReported += announced.Add;

        // act
        systemUnderTest.ReportProgress(new CommandProgress("working", 1, 4));

        // assert
        var report = Assert.Single(systemUnderTest.ProgressReports);

        Assert.Equal("working", report.Message);
        Assert.True(report.IsMeasured);
        Assert.Equal(0.25, report.Fraction);
        Assert.Single(announced);

        // progress is not output: it is redrawn in place, so it is not part of the transcript
        Assert.Empty(systemUnderTest.Lines);
    }

    [Fact]
    public void TheWidthIsThePaneRatherThanTheWindow()
    {
        // arrange -- the reason ITextOutputProvider.Width exists at all
        var systemUnderTest = new TuiTextOutputProvider();

        // assert
        Assert.Equal(CommandFrameworkConstants.DefaultOutputWidth, systemUnderTest.Width);

        // act
        systemUnderTest.Width = 120;

        // assert
        Assert.Equal(120, systemUnderTest.Width);
    }

    [Fact]
    public void AWidthThatCouldNotBeMeasuredFallsBackToTheDefault()
    {
        // arrange -- a console that reports nothing useful should not wrap usage text to zero
        var systemUnderTest = new TuiTextOutputProvider();

        // act
        systemUnderTest.Width = 0;

        // assert
        Assert.Equal(CommandFrameworkConstants.DefaultOutputWidth, systemUnderTest.Width);
    }

    [Fact]
    public void ClearingLeavesNothingFromTheRunBefore()
    {
        // arrange
        var systemUnderTest = new TuiTextOutputProvider();

        systemUnderTest.WriteLine("from the last run");
        systemUnderTest.Write("unfinished");
        systemUnderTest.ReportProgress(new CommandProgress("working"));

        // act
        systemUnderTest.Clear();

        // assert
        Assert.Empty(systemUnderTest.Lines);
        Assert.Empty(systemUnderTest.ProgressReports);
        Assert.False(systemUnderTest.HasPendingText);
    }
}
