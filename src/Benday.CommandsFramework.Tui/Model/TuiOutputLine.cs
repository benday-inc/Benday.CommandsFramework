namespace Benday.CommandsFramework.Tui.Model;

/// <summary>
/// Which of a command's three output channels a line came from.
/// </summary>
/// <remarks>
/// The framework keeps these apart so that output can be piped -- the result goes to stdout
/// and everything else goes to stderr. The interface has one pane rather than two streams, so
/// it keeps them apart visually instead: a user watching a command wants chronology, not two
/// scrolling regions.
/// </remarks>
public enum TuiOutputChannel
{
    /// <summary>
    /// What the command was asked to produce.
    /// </summary>
    Result,

    /// <summary>
    /// Commentary about the work.
    /// </summary>
    Status,

    /// <summary>
    /// An error message.
    /// </summary>
    Error
}

/// <summary>
/// One line a command wrote, and which channel it wrote it on.
/// </summary>
public sealed class TuiOutputLine
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="channel">Which channel the line came from</param>
    /// <param name="text">The line</param>
    public TuiOutputLine(TuiOutputChannel channel, string text)
    {
        Channel = channel;
        Text = text ?? string.Empty;
    }

    /// <summary>
    /// Which channel the line came from.
    /// </summary>
    public TuiOutputChannel Channel { get; }

    /// <summary>
    /// The line, without its line ending.
    /// </summary>
    public string Text { get; }

    public override string ToString() => $"{Channel}: {Text}";
}
