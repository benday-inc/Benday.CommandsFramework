namespace Benday.CommandsFramework.Tui.Model;

/// <summary>
/// How much of the terminal a list of commands is allowed to use.
/// </summary>
/// <remarks>
/// Sizing is a decision, so it lives here and gets tested, rather than being a number typed
/// into the renderer. It used to be one: the list showed twenty rows whatever the terminal
/// was, which on a tall window left most of the screen empty and made a long list feel far
/// longer than it is.
/// </remarks>
public static class TuiLayout
{
    /// <summary>
    /// Lines the prompt draws around the list: two of title, the search line, the
    /// more-choices line, and a margin so the whole thing does not sit flush against the
    /// bottom of the window.
    /// </summary>
    private const int Chrome = 6;

    /// <summary>
    /// The fewest rows worth showing. Below this a list stops being a list, and Spectre
    /// refuses a page size under three outright.
    /// </summary>
    private const int MinimumRows = 5;

    /// <summary>
    /// How many rows of a list to show in a terminal of a given height.
    /// </summary>
    /// <param name="consoleHeight">Height of the terminal, in lines</param>
    /// <returns>The page size</returns>
    public static int GetPageSize(int consoleHeight)
    {
        return Math.Max(MinimumRows, consoleHeight - Chrome);
    }

    /// <summary>
    /// Trims a description so that a row takes exactly one line.
    /// </summary>
    /// <remarks>
    /// The page size counts rows, not the lines they take, so a description long enough to
    /// wrap makes the list overflow the window it was measured against -- and the longest
    /// descriptions belong to the commands that need the most explaining, so this is the
    /// normal case rather than the odd one. A row that has to be cut says so with an ellipsis;
    /// the whole description is on the form once the command is opened.
    /// </remarks>
    /// <param name="label">The command's name, as it will be shown</param>
    /// <param name="description">What the command does</param>
    /// <param name="width">How many characters wide the terminal is</param>
    /// <returns>The description, trimmed to fit, or empty when there is no room for one</returns>
    public static string FitDescription(string label, string description, int width)
    {
        if (string.IsNullOrWhiteSpace(description) == true)
        {
            return string.Empty;
        }

        // a description written across several lines is one line here
        var text = description
            .Replace("\r\n", " ")
            .Replace('\n', ' ')
            .Replace('\r', ' ')
            .Trim();

        // what the row costs before a single character of description: four for the indent
        // drawn in front of a row inside a group, three for the ' - ', and one spare so a row
        // that fills the line exactly does not tip over onto a second one. Measured against a
        // real terminal, where five was two short and every long row still wrapped.
        const int Decoration = 8;

        var room = width - (label ?? string.Empty).Length - Decoration;

        if (room < MinimumDescription)
        {
            // no room worth having: the name on its own is more use than three letters of a
            // sentence
            return string.Empty;
        }

        if (text.Length <= room)
        {
            return text;
        }

        return string.Concat(text.AsSpan(0, room - 1).TrimEnd(), "…");
    }

    /// <summary>
    /// The shortest description worth showing. Less than this and the row is all ellipsis.
    /// </summary>
    private const int MinimumDescription = 12;
}
