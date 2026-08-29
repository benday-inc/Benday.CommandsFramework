namespace Benday.CommandsFramework.Tui.Model;

/// <summary>
/// Matches a filter against text the way a person expects when they type a few letters into
/// a list: the letters have to appear in order, but not next to each other.
/// </summary>
/// <remarks>
/// Deliberately small and deterministic rather than clever. It is in the model layer because
/// what a filter matches is a decision, and decisions get tested; the renderer only draws
/// whatever survives.
/// </remarks>
public static class FuzzyMatch
{
    /// <summary>
    /// True when every character of the filter appears in the text, in order and without
    /// regard to case. An empty filter matches everything.
    /// </summary>
    /// <param name="text">Text to search</param>
    /// <param name="filter">What was typed</param>
    /// <returns>Whether it matches</returns>
    public static bool IsMatch(string? text, string? filter)
    {
        return Score(text, filter) > 0;
    }

    /// <summary>
    /// How well the filter matches, higher being better, or zero for no match at all.
    /// </summary>
    /// <remarks>
    /// A whole word match beats a run of letters, which beats letters scattered through the
    /// text, and matching at the start beats matching in the middle. That ordering is the
    /// only thing about the number that matters -- nothing should depend on its magnitude.
    /// </remarks>
    /// <param name="text">Text to search</param>
    /// <param name="filter">What was typed</param>
    /// <returns>The score, or 0 when the filter does not match</returns>
    public static int Score(string? text, string? filter)
    {
        // an empty filter is not a question, so everything answers it
        if (string.IsNullOrEmpty(filter) == true)
        {
            return 1;
        }

        if (string.IsNullOrEmpty(text) == true)
        {
            return 0;
        }

        // a contiguous match is worth more than a scattered one, and one at the start is worth
        // more than one in the middle
        var contiguous = ScoreContiguous(text, filter);

        if (contiguous > 0)
        {
            return contiguous;
        }

        var score = 0;
        var searchFrom = 0;
        var previousIndex = -2;

        foreach (var character in filter)
        {
            var index = IndexOf(text, character, searchFrom);

            if (index < 0)
            {
                return 0;
            }

            score += index == previousIndex + 1 ? 3 : 1;

            previousIndex = index;
            searchFrom = index + 1;
        }

        return score;
    }

    /// <summary>
    /// How well the filter matches, counting only a run of characters that appears as it was
    /// typed. Zero for anything looser.
    /// </summary>
    /// <remarks>
    /// This is what prose is matched with, and the difference from Score() is the whole reason
    /// both exist. Scattered characters are the right rule for a name, which is short and
    /// which people type an abbreviation of -- 'wl' should find 'widget list'. It is the wrong
    /// rule for a sentence: a description long enough to be useful contains almost any four
    /// letters somewhere in order, so 'list' matched the description of nearly two commands in
    /// three on a real tool, and the ones it found for a reason were buried among them.
    /// </remarks>
    /// <param name="text">Text to search</param>
    /// <param name="filter">What was typed</param>
    /// <returns>The score, or 0 when the filter does not appear</returns>
    public static int ScoreContiguous(string? text, string? filter)
    {
        if (string.IsNullOrEmpty(filter) == true)
        {
            return 1;
        }

        if (string.IsNullOrEmpty(text) == true)
        {
            return 0;
        }

        var index = text.IndexOf(filter, StringComparison.OrdinalIgnoreCase);

        if (index < 0)
        {
            return 0;
        }

        return 1000 + (index == 0 ? 100 : 0) + Math.Max(0, 50 - index);
    }

    /// <summary>
    /// The best score across several pieces of text, so an item can be searched by name,
    /// description, category and anything else it has.
    /// </summary>
    /// <param name="filter">What was typed</param>
    /// <param name="candidates">The text to search</param>
    /// <returns>The best score, or 0 when nothing matches</returns>
    public static int BestScore(string? filter, params string?[] candidates)
    {
        if (string.IsNullOrEmpty(filter) == true)
        {
            return 1;
        }

        if (candidates is null)
        {
            return 0;
        }

        var best = 0;

        foreach (var candidate in candidates)
        {
            var score = Score(candidate, filter);

            if (score > best)
            {
                best = score;
            }
        }

        return best;
    }

    /// <summary>
    /// The best contiguous score across several pieces of text.
    /// </summary>
    /// <param name="filter">What was typed</param>
    /// <param name="candidates">The text to search</param>
    /// <returns>The best score, or 0 when nothing matches</returns>
    public static int BestContiguousScore(string? filter, params string?[] candidates)
    {
        if (string.IsNullOrEmpty(filter) == true)
        {
            return 1;
        }

        if (candidates is null)
        {
            return 0;
        }

        var best = 0;

        foreach (var candidate in candidates)
        {
            var score = ScoreContiguous(candidate, filter);

            if (score > best)
            {
                best = score;
            }
        }

        return best;
    }

    private static int IndexOf(string text, char character, int startAt)
    {
        for (var i = startAt; i < text.Length; i++)
        {
            if (char.ToLowerInvariant(text[i]) == char.ToLowerInvariant(character) == true)
            {
                return i;
            }
        }

        return -1;
    }
}
