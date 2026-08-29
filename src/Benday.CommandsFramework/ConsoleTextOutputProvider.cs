namespace Benday.CommandsFramework;

/// <summary>
/// Implementation of ITextOutputProvider that outputs messages to the system console.
/// </summary>
public class ConsoleTextOutputProvider : ITextOutputProvider
{
    private bool _HasUnfinishedProgressLine;
    private int _LastProgressLength;

    /// <summary>
    /// Write a message to the console
    /// </summary>
    /// <param name="line"></param>
    public void WriteLine(string line)
    {
        FinishProgressLine();

        Console.WriteLine(line);
    }

    /// <summary>
    /// Write a new line to the console
    /// </summary>
    public void WriteLine()
    {
        FinishProgressLine();

        Console.WriteLine();
    }
    public void Write(string message)
    {
        FinishProgressLine();

        Console.Write(message);
    }

    /// <summary>
    /// Write a line of commentary to stderr, so that it survives -- and stays out of -- a
    /// redirect of the command's result.
    /// </summary>
    /// <param name="line">Text to write</param>
    public void WriteStatus(string line)
    {
        FinishProgressLine();

        Console.Error.WriteLine(line);
    }

    /// <summary>
    /// Write an error message to stderr.
    /// </summary>
    /// <param name="line">Text to write</param>
    public void WriteError(string line)
    {
        FinishProgressLine();

        Console.Error.WriteLine(line);
    }

    /// <summary>
    /// The width of the console window, or the default when output is redirected or there is
    /// no console attached -- reading the window width in either case is meaningless at best
    /// and throws at worst.
    /// </summary>
    /// <summary>
    /// Draws progress on one line of stderr, redrawing it in place.
    /// </summary>
    /// <remarks>
    /// Only when stderr is a terminal. Redirected, the carriage returns would fill the
    /// destination with one line per report and no way to read it, so each report becomes an
    /// ordinary status line instead. This is the same reason a progress bar survives
    /// 2&gt;/dev/null without corrupting a redirected result.
    /// </remarks>
    /// <param name="progress">The report</param>
    public void ReportProgress(CommandProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress, nameof(progress));

        var text = progress.ToString();

        if (Console.IsErrorRedirected == true)
        {
            WriteStatus(text);

            return;
        }

        // pad to cover whatever the previous, possibly longer, report left behind
        var padded = text.PadRight(_LastProgressLength);

        Console.Error.Write($"\r{padded}");

        _LastProgressLength = text.Length;
        _HasUnfinishedProgressLine = true;
    }

    /// <summary>
    /// Ends the progress line, if one is part-written, so the next thing written starts on a
    /// line of its own rather than on top of it.
    /// </summary>
    private void FinishProgressLine()
    {
        if (_HasUnfinishedProgressLine == false)
        {
            return;
        }

        _HasUnfinishedProgressLine = false;
        _LastProgressLength = 0;

        Console.Error.WriteLine();
    }

    public int Width
    {
        get
        {
            if (Console.IsOutputRedirected == true)
            {
                return CommandFrameworkConstants.DefaultOutputWidth;
            }

            try
            {
                return Console.WindowWidth;
            }
            catch (IOException)
            {
                return CommandFrameworkConstants.DefaultOutputWidth;
            }
        }
    }
}
