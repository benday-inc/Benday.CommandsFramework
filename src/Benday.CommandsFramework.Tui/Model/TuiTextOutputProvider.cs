using System.Text;

namespace Benday.CommandsFramework.Tui.Model;

/// <summary>
/// Where a command's output goes while it runs inside the interface.
/// </summary>
/// <remarks>
/// This collects lines and announces them; it does not draw anything. That is what lets a
/// test run a real command and assert on what it wrote without a terminal anywhere in sight,
/// and it is why the screen that renders a run holds no logic of its own.
///
/// Width is the whole reason ITextOutputProvider.Width exists. Usage text is wrapped against
/// it, and inside an interface the width of the console window is the wrong number -- it is
/// wrong by whatever the chrome takes, and reading it at all throws in a process with no
/// console.
/// </remarks>
public sealed class TuiTextOutputProvider : ITextOutputProvider
{
    private readonly object _Gate = new();
    private readonly List<TuiOutputLine> _Lines = [];
    private readonly StringBuilder _Pending = new();
    private int _Width = CommandFrameworkConstants.DefaultOutputWidth;

    /// <summary>
    /// Raised for each line, as it is written.
    /// </summary>
    public event Action<TuiOutputLine>? LineWritten;

    /// <summary>
    /// Raised for each progress report from a running command.
    /// </summary>
    public event Action<CommandProgress>? ProgressReported;

    /// <summary>
    /// Everything that has been written since the last Clear().
    /// </summary>
    public IReadOnlyList<TuiOutputLine> Lines
    {
        get
        {
            lock (_Gate)
            {
                return _Lines.ToList();
            }
        }
    }

    /// <summary>
    /// Every progress report since the last Clear(). The most recent one is what a display
    /// shows; the rest are what a test asserts on.
    /// </summary>
    public IReadOnlyList<CommandProgress> ProgressReports { get; private set; } = [];

    /// <summary>
    /// How many characters wide the output pane is.
    /// </summary>
    public int Width
    {
        get => _Width;

        set => _Width = value < 1 ? CommandFrameworkConstants.DefaultOutputWidth : value;
    }

    /// <summary>
    /// True when something has been written with Write() that has not been ended by a newline
    /// yet. That is what a prompt looks like on the way past: the framework writes the
    /// question without a line ending and then reads.
    /// </summary>
    public bool HasPendingText
    {
        get
        {
            lock (_Gate)
            {
                return _Pending.Length > 0;
            }
        }
    }

    /// <summary>
    /// Takes whatever has been written without a line ending, and clears it.
    /// </summary>
    /// <remarks>
    /// This is how a command that asks a question gets that question rendered as the label of
    /// the prompt rather than stranded as half a line above it.
    /// </remarks>
    /// <returns>The partial line, empty when there is none</returns>
    public string TakePendingText()
    {
        lock (_Gate)
        {
            var text = _Pending.ToString();

            _Pending.Clear();

            return text;
        }
    }

    /// <summary>
    /// Forgets everything, ready for another run.
    /// </summary>
    public void Clear()
    {
        lock (_Gate)
        {
            _Lines.Clear();
            _Pending.Clear();
            ProgressReports = [];
        }
    }

    /// <summary>
    /// Everything written on one channel, in the order it was written.
    /// </summary>
    /// <param name="channel">The channel</param>
    /// <returns>The lines</returns>
    public IReadOnlyList<string> GetLines(TuiOutputChannel channel)
    {
        return Lines.Where(x => x.Channel == channel).Select(x => x.Text).ToList();
    }

    /// <summary>
    /// Everything written, on every channel, in write order.
    /// </summary>
    public string GetOutput()
    {
        return string.Join(Environment.NewLine, Lines.Select(x => x.Text));
    }

    /// <summary>
    /// Write a line of the command's result.
    /// </summary>
    /// <param name="line">Text to write</param>
    public void WriteLine(string line) => Add(TuiOutputChannel.Result, line);

    /// <summary>
    /// Write an empty line of the command's result.
    /// </summary>
    public void WriteLine() => Add(TuiOutputChannel.Result, string.Empty);

    /// <summary>
    /// Write part of a line, with no line ending. It is announced once something ends it.
    /// </summary>
    /// <param name="message">Text to write</param>
    public void Write(string message)
    {
        lock (_Gate)
        {
            _Pending.Append(message);
        }
    }

    /// <summary>
    /// Write a line of commentary about the work.
    /// </summary>
    /// <param name="line">Text to write</param>
    public void WriteStatus(string line) => Add(TuiOutputChannel.Status, line);

    /// <summary>
    /// Write an error message.
    /// </summary>
    /// <param name="line">Text to write</param>
    public void WriteError(string line) => Add(TuiOutputChannel.Error, line);

    /// <summary>
    /// Take a progress report from a running command.
    /// </summary>
    /// <remarks>
    /// Recorded rather than rendered, for the same reason the lines are. The report knows
    /// whether it is measured; a display uses that to choose between a real bar and a
    /// spinner, and does so somewhere that has a terminal to draw on.
    /// </remarks>
    /// <param name="progress">The report</param>
    public void ReportProgress(CommandProgress progress)
    {
        if (progress is null)
        {
            return;
        }

        lock (_Gate)
        {
            ProgressReports = [.. ProgressReports, progress];
        }

        ProgressReported?.Invoke(progress);
    }

    /// <summary>
    /// Records a line and announces it.
    /// </summary>
    /// <remarks>
    /// A result line that lands on top of a partial one takes the partial text with it, so
    /// 'Write("Name: ")' followed by 'WriteLine("Ann")' is one line and not two. Anything on
    /// another channel ends the partial line first and leaves it on its own channel instead
    /// of absorbing it, which is the same thing separate streams do for a console: an error
    /// is not part of the sentence the result was half way through.
    /// </remarks>
    private void Add(TuiOutputChannel channel, string text)
    {
        var written = new List<TuiOutputLine>();

        lock (_Gate)
        {
            if (_Pending.Length > 0)
            {
                var pending = _Pending.ToString();

                _Pending.Clear();

                if (channel == TuiOutputChannel.Result)
                {
                    text = pending + text;
                }
                else
                {
                    written.Add(new TuiOutputLine(TuiOutputChannel.Result, pending));
                }
            }

            written.Add(new TuiOutputLine(channel, text ?? string.Empty));

            _Lines.AddRange(written);
        }

        // announced outside the lock: a handler draws, and drawing is allowed to take its
        // time without holding up a command that is still writing
        foreach (var line in written)
        {
            LineWritten?.Invoke(line);
        }
    }
}
