using Benday.CommandsFramework.Tui.Model;

using Spectre.Console;

namespace Benday.CommandsFramework.Tui.Screens;

/// <summary>
/// Runs a command and shows what it does while it does it.
/// </summary>
/// <remarks>
/// The three output channels stay visually distinct but land in one pane in write order,
/// because a user watching a command wants chronology rather than two scrolling regions.
///
/// Nothing here decides anything: the runner builds and runs the command and says what
/// happened, and this draws it. That is what lets a run be tested without a terminal.
/// </remarks>
internal sealed class CommandRunScreen
{
    private readonly IAnsiConsole _Console;
    private readonly TuiCommandRunner _Runner;
    private bool _ProgressLineIsOpen;

    public CommandRunScreen(IAnsiConsole console, TuiCommandRunner runner)
    {
        _Console = console;
        _Runner = runner;
    }

    /// <summary>
    /// Runs what the form has been filled in with, and shows how it went.
    /// </summary>
    /// <param name="form">The filled in form</param>
    /// <param name="cancellationToken">Closes the interface</param>
    /// <returns>What happened</returns>
    public async Task<TuiRunResult> ShowAsync(
        TuiCommandForm form, CancellationToken cancellationToken)
    {
        var interactive = _Console.Profile.Capabilities.Interactive;

        _Console.WriteLine();
        _Console.Write(new Rule("[bold]Running[/]") { Justification = Justify.Left });
        _Console.MarkupLine($"[grey]{Markup.Escape(form.GetCommandLine())}[/]");
        _Console.WriteLine();

        // cancelling the command is not cancelling the interface, which is the whole reason
        // the framework hands a command its own token
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken);

        _ProgressLineIsOpen = false;

        _Runner.Output.LineWritten += RenderLine;
        _Runner.Output.ProgressReported += RenderProgress;

        // no reader when there is nothing to ask on. ReadLine then returns null, which is
        // what Console.ReadLine() returns at the end of input and what a command that
        // prompts already handles.
        _Runner.Input.Reader = interactive == true ? Ask : null;

        using var interrupt = interactive == true
            ? CtrlCHandler.Hook(_Console, cancellation)
            : null;

        try
        {
            var result = await _Runner.RunAsync(form, cancellation.Token);

            EndProgressLine();
            RenderSummary(result);

            return result;
        }
        finally
        {
            _Runner.Output.LineWritten -= RenderLine;
            _Runner.Output.ProgressReported -= RenderProgress;
            _Runner.Input.Reader = null;
        }
    }

    /// <summary>
    /// Draws one line of output, styled by the channel it came from.
    /// </summary>
    private void RenderLine(TuiOutputLine line)
    {
        EndProgressLine();

        var text = Markup.Escape(line.Text);

        switch (line.Channel)
        {
            case TuiOutputChannel.Error:
                _Console.MarkupLine($"[red]{text}[/]");
                break;

            case TuiOutputChannel.Status:
                _Console.MarkupLine($"[grey]{text}[/]");
                break;

            default:
                _Console.MarkupLine(text);
                break;
        }
    }

    /// <summary>
    /// Draws a progress report.
    /// </summary>
    /// <remarks>
    /// Redrawn in place on a terminal, and written as an ordinary line when there is no
    /// terminal to redraw on -- the same rule the console output provider follows, and for
    /// the same reason: a run whose output is being read later wants one line per report, and
    /// a run being watched wants one line.
    /// </remarks>
    private void RenderProgress(CommandProgress progress)
    {
        var text = progress.ToString();

        if (_Console.Profile.Capabilities.Interactive == false)
        {
            _Console.MarkupLine($"[grey]{Markup.Escape(text)}[/]");

            return;
        }

        if (_ProgressLineIsOpen == true)
        {
            _Console.Cursor.MoveUp();
        }

        // padded to the width of the pane so that a shorter report does not leave the tail
        // of a longer one behind it, and truncated so it cannot wrap onto a second line --
        // which would put the cursor somewhere other than where the next report expects it
        _Console.MarkupLine($"[grey]{Markup.Escape(Fit(text))}[/]");

        _ProgressLineIsOpen = true;
    }

    /// <summary>
    /// Leaves the last progress report on screen and stops drawing over it.
    /// </summary>
    private void EndProgressLine()
    {
        _ProgressLineIsOpen = false;
    }

    /// <summary>
    /// Trims or pads a line to exactly the width of the pane.
    /// </summary>
    private string Fit(string text)
    {
        var width = Math.Max(_Console.Profile.Width - 1, 1);

        if (text.Length > width)
        {
            return width <= 1 ? text[..width] : string.Concat(text.AsSpan(0, width - 1), "…");
        }

        return text.PadRight(width);
    }

    /// <summary>
    /// Asks a running command's question.
    /// </summary>
    /// <remarks>
    /// The question is whatever the command wrote just before it read, which is how
    /// CommandBase.Prompt() has always written it. A command that asks questions therefore
    /// works in here with no knowledge that it is in here at all.
    /// </remarks>
    private string? Ask(string question)
    {
        EndProgressLine();

        var label = string.IsNullOrWhiteSpace(question) == true
            ? "Input"
            : question.TrimEnd();

        return _Console.Prompt(
            new TextPrompt<string>(Markup.Escape(label)).AllowEmpty());
    }

    /// <summary>
    /// Says how the run ended.
    /// </summary>
    private void RenderSummary(TuiRunResult result)
    {
        _Console.WriteLine();

        var elapsed = FormatDuration(result.Duration);

        switch (result.Status)
        {
            case CommandExecutionStatus.Success:
                _Console.MarkupLine($"[green]Done[/] [grey]in {elapsed}.[/]");
                break;

            case CommandExecutionStatus.UsageDisplayed:
                // asking for usage and getting it is not a failure
                _Console.MarkupLine("[green]Usage shown.[/]");
                break;

            case CommandExecutionStatus.Cancelled:
                _Console.MarkupLine($"[yellow]Cancelled[/] [grey]after {elapsed}.[/]");
                break;

            case CommandExecutionStatus.ValidationFailed:
                // the command has already written its usage and the reasons through its
                // output provider, so repeating them here would say everything twice
                _Console.MarkupLine("[yellow]It did not run: the arguments are not valid.[/]");
                break;

            default:
                _Console.MarkupLine(
                    string.IsNullOrWhiteSpace(result.Message) == true
                        ? "[red]It failed.[/]"
                        : $"[red]It failed:[/] {Markup.Escape(result.Message)}");
                break;
        }

        _Console.WriteLine();
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds < 1)
        {
            return $"{duration.TotalMilliseconds:0}ms";
        }

        return duration.TotalMinutes < 1
            ? $"{duration.TotalSeconds:0.0}s"
            : $"{(int)duration.TotalMinutes}m {duration.Seconds}s";
    }
}
