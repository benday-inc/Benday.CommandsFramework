namespace Benday.CommandsFramework;

/// <summary>
/// Interface for handling text output from commands.
/// </summary>
/// <remarks>
/// There are three channels, following the convention every other command line tool uses:
/// the <b>result</b> is what the command was asked to produce and goes to stdout; <b>status</b>
/// is commentary about the work -- progress, notes, "found 3 items" -- and goes to stderr; and
/// <b>errors</b> go to stderr. Keeping them apart is what lets a command's output be piped
/// somewhere useful: a command that grows a /json flag emits invalid JSON the moment anything
/// else has written a message to the same stream.
///
/// WriteStatus() and WriteError() are default interface members that fall back to WriteLine(),
/// so an existing implementation of this interface keeps working unchanged and keeps putting
/// everything on one channel until it opts in.
/// </remarks>
public interface ITextOutputProvider
{
    /// <summary>
    /// Write a line of text to output
    /// </summary>
    /// <param name="line">Text to write</param>
    void WriteLine(string line);

    /// <summary>
    /// Write a new line to the output
    /// </summary>
    void WriteLine();

    /// <summary>
    /// Write text to output without an ending newline
    /// </summary>
    /// <param name="line">Text to write</param>
    void Write(string message);

    /// <summary>
    /// Write a line of commentary about the work -- progress, notes, anything that is not
    /// the result the command was asked to produce. Goes to the diagnostic channel, which
    /// means a caller redirecting the result never sees it mixed in.
    /// </summary>
    /// <param name="line">Text to write</param>
    void WriteStatus(string line) => WriteLine(line);

    /// <summary>
    /// Write an error message. Goes to the diagnostic channel, so a command that fails while
    /// producing machine readable output does not land its error text inside the payload.
    /// </summary>
    /// <param name="line">Text to write</param>
    void WriteError(string line) => WriteLine(line);

    /// <summary>
    /// How many characters wide the output is, for wrapping usage text.
    /// </summary>
    /// <remarks>
    /// This belongs to the output provider because the terminal is not always where the
    /// output is going. Inside a pane of a terminal UI, or in a web page, the width of the
    /// console window is the wrong number -- and reading Console.WindowWidth from a process
    /// with no console throws.
    ///
    /// Default interface member, so an existing provider keeps working and reports the same
    /// width the framework used to compute for itself.
    /// </remarks>
    int Width => CommandFrameworkConstants.DefaultOutputWidth;

    /// <summary>
    /// Report progress from a running command.
    /// </summary>
    /// <remarks>
    /// Progress is commentary, so the default writes it to the status channel -- which means
    /// a provider that does nothing special still behaves correctly. A console provider can
    /// redraw one line in place; a user interface can move a real progress bar.
    /// </remarks>
    /// <param name="progress">The report</param>
    void ReportProgress(CommandProgress progress) => WriteStatus(progress.ToString());
}
