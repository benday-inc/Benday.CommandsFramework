using Benday.CommandsFramework.Tui.Model;

using Spectre.Console;

namespace Benday.CommandsFramework.Tui;

/// <summary>
/// The terminal interface, rendered with Spectre.Console.
/// </summary>
/// <remarks>
/// This is the thin rendering layer. Anything that decides something belongs in
/// <see cref="TuiSession"/> and the rest of Model, so it can be tested without a terminal.
///
/// The console is injectable so a test can drive this with Spectre's TestConsole. That is
/// also what keeps the interface honest about non interactive terminals: a TestConsole is
/// not interactive, and neither is a redirected one, and in both cases waiting for a key
/// press would hang forever.
/// </remarks>
public sealed class SpectreTuiHost : ITuiHost
{
    private readonly IAnsiConsole? _Console;

    /// <summary>
    /// Constructor. Renders to the real terminal.
    /// </summary>
    public SpectreTuiHost()
        : this(null)
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="console">Where to render, or null for the real terminal</param>
    public SpectreTuiHost(IAnsiConsole? console)
    {
        _Console = console;
    }

    /// <summary>
    /// The console this renders to.
    /// </summary>
    /// <remarks>
    /// Resolved on each use rather than captured in the constructor, because
    /// AnsiConsole.Console is settable and a caller that swaps it should be obeyed.
    /// </remarks>
    private IAnsiConsole Console => _Console ?? AnsiConsole.Console;

    /// <summary>
    /// Runs the interface until the user exits it.
    /// </summary>
    /// <param name="program">The program whose commands are shown</param>
    /// <param name="cancellationToken">Stops the interface</param>
    /// <returns>The exit code the process should use if it is exiting</returns>
    public async Task<int> RunAsync(
        ICommandProgram program, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(program);

        var session = TuiSession.FromProgram(program);

        return await RunAsync(session, cancellationToken);
    }

    /// <summary>
    /// Runs the interface for an already built session.
    /// </summary>
    /// <param name="session">What to show</param>
    /// <param name="cancellationToken">Stops the interface</param>
    /// <returns>The exit code the process should use if it is exiting</returns>
    public async Task<int> RunAsync(
        TuiSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        var console = Console;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            Render(console, session);

            await WaitForExitAsync(console, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // closing the interface is how it is meant to end, and being cancelled out of it
            // is not a failure of the tool
            console.WriteLine();
        }

        // nothing here assigns Environment.ExitCode. The console entry point does that, and
        // only with what this hands back.
        return CommandFrameworkConstants.ExitCode_Success;
    }

    /// <summary>
    /// Draws the interface.
    /// </summary>
    private static void Render(IAnsiConsole console, TuiSession session)
    {
        console.Write(new Rule($"[bold]{Markup.Escape(session.Title)}[/]")
        {
            Justification = Justify.Left
        });

        console.WriteLine();

        foreach (var line in GetHeaderLines(session))
        {
            console.MarkupLine(line);
        }

        console.WriteLine();

        if (session.HasProblems == true)
        {
            RenderProblems(console, session);
        }

        console.MarkupLine(
            "[grey]The command browser, the argument form and running a command are not " +
            "built yet.[/]");
    }

    /// <summary>
    /// The header lines, in the order they are drawn. A value that was never configured is
    /// skipped rather than drawn as an empty line, which is the same rule the console usage
    /// output follows.
    /// </summary>
    internal static List<string> GetHeaderLines(TuiSession session)
    {
        var lines = new List<string>();

        if (string.IsNullOrWhiteSpace(session.Version) == false)
        {
            lines.Add($"[grey]Version:[/] {Markup.Escape(session.Version)}");
        }

        if (string.IsNullOrWhiteSpace(session.Website) == false)
        {
            lines.Add($"[grey]Website:[/] {Markup.Escape(session.Website)}");
        }

        lines.Add(
            session.CommandCount == 1
                ? "[grey]Commands:[/] 1"
                : $"[grey]Commands:[/] {session.CommandCount}");

        return lines;
    }

    /// <summary>
    /// Shows what makes a command unreachable. This is the diagnostic a tool author wants
    /// and it is otherwise only visible from a unit test.
    /// </summary>
    private static void RenderProblems(IAnsiConsole console, TuiSession session)
    {
        var rows = string.Join(
            Environment.NewLine,
            session.Problems.Select(x => $"[red]-[/] {Markup.Escape(x)}"));

        console.Write(
            new Panel(new Markup(rows))
            {
                Header = new PanelHeader("[red]Problems[/]"),
                Border = BoxBorder.Rounded
            });

        console.WriteLine();
    }

    /// <summary>
    /// Waits for the user to close the interface.
    /// </summary>
    /// <remarks>
    /// Only when the terminal is interactive. Redirected -- a test, a pipe, a CI log -- there
    /// is no key press coming and waiting for one would hang the tool forever.
    /// </remarks>
    private static async Task WaitForExitAsync(
        IAnsiConsole console, CancellationToken cancellationToken)
    {
        if (console.Profile.Capabilities.Interactive == false)
        {
            return;
        }

        console.WriteLine();
        console.Markup("[grey]Press any key to exit.[/]");

        await console.Input.ReadKeyAsync(true, cancellationToken);

        console.WriteLine();
    }
}
