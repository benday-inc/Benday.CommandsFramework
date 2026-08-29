using Benday.CommandsFramework.Tui.Model;

namespace Benday.CommandsFramework.Tui.Tests;

/// <summary>
/// Tests how much of the terminal a list is allowed to use. Sizing is a decision, so it is
/// tested here rather than being a number typed into a renderer.
/// </summary>
public class TuiLayoutFixture
{
    [Fact]
    public void ATallerWindowShowsMoreRows()
    {
        // arrange, act and assert -- the list used to show twenty rows whatever the window was
        Assert.True(TuiLayout.GetPageSize(50) > TuiLayout.GetPageSize(24));
    }

    [Fact]
    public void ThePageLeavesRoomForWhatIsDrawnAroundIt()
    {
        // arrange -- title, search line, more-choices line and a margin
        var height = 49;

        // act
        var pageSize = TuiLayout.GetPageSize(height);

        // assert
        Assert.True(pageSize < height);
        Assert.True(pageSize > height - 10);
    }

    [Fact]
    public void AWindowWithAlmostNoRoomStillShowsAList()
    {
        // arrange -- Spectre refuses a page size under three, and a list of one is not a list
        Assert.True(TuiLayout.GetPageSize(4) >= 5);
        Assert.True(TuiLayout.GetPageSize(0) >= 5);
    }

    [Fact]
    public void ADescriptionThatFitsIsLeftAlone()
    {
        // arrange, act and assert
        Assert.Equal(
            "Lists the widgets.",
            TuiLayout.FitDescription("widget list", "Lists the widgets.", 200));
    }

    [Fact]
    public void ADescriptionTooLongForTheLineIsCutToOneLine()
    {
        // arrange -- the page counts rows, not the lines they take, so a row that wraps makes
        // the list overflow the window it was measured against
        var description = new string('x', 400);

        // act
        var fitted = TuiLayout.FitDescription("exportagentcapabilities", description, 80);

        // assert
        Assert.True(fitted.Length < 80);
        Assert.EndsWith("…", fitted);
    }

    [Fact]
    public void ADescriptionWrittenOverSeveralLinesBecomesOne()
    {
        // arrange, act and assert
        Assert.Equal(
            "one two",
            TuiLayout.FitDescription("cmd", "one\r\ntwo", 200));
    }

    [Fact]
    public void ANarrowWindowKeepsTheNameAndDropsTheDescription()
    {
        // arrange -- three letters of a sentence are worth less than the name they crowd
        var fitted = TuiLayout.FitDescription("a-long-command-name", "Does something", 24);

        // assert
        Assert.Empty(fitted);
    }

    [Fact]
    public void ThereIsNothingToShowForACommandWithNoDescription()
    {
        // arrange, act and assert
        Assert.Empty(TuiLayout.FitDescription("cmd", "", 200));
        Assert.Empty(TuiLayout.FitDescription("cmd", "   ", 200));
    }
}
