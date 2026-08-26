using Benday.CommandsFramework.Tui.Model;

namespace Benday.CommandsFramework.Tui.Tests;

/// <summary>
/// Tests the filter. What a filter matches is a decision, so it lives in the model and gets
/// tested here rather than being discovered by squinting at a terminal.
/// </summary>
public class FuzzyMatchFixture
{
    [Theory]
    [InlineData("check-configuration", "check")]
    [InlineData("check-configuration", "config")]
    [InlineData("check-configuration", "CHECK")]
    [InlineData("check-configuration", "ckcfg")]
    [InlineData("widget list", "wl")]
    public void IsMatch_FindsWhatSomeoneWouldType(string text, string filter)
    {
        Assert.True(FuzzyMatch.IsMatch(text, filter));
    }

    [Theory]
    [InlineData("check-configuration", "zzz")]
    [InlineData("check-configuration", "noitarugifnoc")]
    [InlineData("", "anything")]
    public void IsMatch_RejectsWhatIsNotThere(string text, string filter)
    {
        Assert.False(FuzzyMatch.IsMatch(text, filter));
    }

    [Fact]
    public void AnEmptyFilterMatchesEverything()
    {
        // an empty filter is not a question, so everything answers it
        Assert.True(FuzzyMatch.IsMatch("anything", ""));
        Assert.True(FuzzyMatch.IsMatch("anything", null));
    }

    [Fact]
    public void AWholeWordBeatsScatteredLetters()
    {
        var contiguous = FuzzyMatch.Score("deploy", "dep");
        var scattered = FuzzyMatch.Score("dance-elephant-porridge", "dep");

        Assert.True(contiguous > scattered);
    }

    [Fact]
    public void MatchingAtTheStartBeatsMatchingInTheMiddle()
    {
        var atStart = FuzzyMatch.Score("config-check", "config");
        var inTheMiddle = FuzzyMatch.Score("check-config-now", "config");

        Assert.True(atStart > inTheMiddle);
    }

    [Fact]
    public void BestScore_LooksAcrossEverythingAnItemHas()
    {
        // the user might remember the description rather than the name
        var score = FuzzyMatch.BestScore(
            "widget", "list", "Lists the things", "Widget Management");

        Assert.True(score > 0);
    }

    [Fact]
    public void BestScore_IsZeroWhenNothingMatches()
    {
        Assert.Equal(0, FuzzyMatch.BestScore("zzz", "list", "Lists the things"));
    }
}
