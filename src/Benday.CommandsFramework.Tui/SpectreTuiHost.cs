using Benday.CommandsFramework.Tui.Model;
using Benday.CommandsFramework.Tui.Screens;

using Spectre.Console;

namespace Benday.CommandsFramework.Tui;

/// <summary>
/// The terminal interface, rendered with Spectre.Console.
/// </summary>
/// <remarks>
/// This is the thin rendering layer. Anything that decides something belongs in Model, so it
/// can be tested without a terminal.
///
/// The console is injectable so a test can drive this with Spectre's TestConsole. That is
/// also what keeps the interface honest about non interactive terminals: a TestConsole is
/// not interactive, and neither is a redirected one, and in both cases prompting would hang
/// forever waiting for input that is not coming.
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

            RenderHeader(console, session);

            var browser = new TuiCommandBrowser(session);

            if (console.Profile.Capabilities.Interactive == false)
            {
                // redirected -- a test, a pipe, a CI log. There is no input coming, so the
                // interface shows what it has to show and stops rather than hanging on a
                // prompt that can never be answered.
                RenderOverview(console, browser);

                return CommandFrameworkConstants.ExitCode_Success;
            }

            var runner = CreateRunner(console, session);

            await BrowseAsync(console, session, browser, runner, cancellationToken);
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
    /// Builds what runs a command, for this session.
    /// </summary>
    /// <remarks>
    /// One runner for the session rather than one per run, because the output provider it
    /// holds is where the width of the pane is recorded -- and the width is what usage text
    /// is wrapped against. Reading Console.WindowWidth instead would be wrong by whatever the
    /// chrome takes and would throw outright in a process with no console.
    /// </remarks>
    private static TuiCommandRunner CreateRunner(IAnsiConsole console, TuiSession session)
    {
        var runner = new TuiCommandRunner(session.Options, session.CommandsAssembly);

        runner.Output.Width = console.Profile.Width;

        return runner;
    }

    /// <summary>
    /// The browse, fill in, run, go back loop.
    /// </summary>
    private async Task BrowseAsync(
        IAnsiConsole console,
        TuiSession session,
        TuiCommandBrowser browser,
        TuiCommandRunner runner,
        CancellationToken cancellationToken)
    {
        var screen = new CommandBrowserScreen(console, browser);

        while (cancellationToken.IsCancellationRequested == false)
        {
            var action = await screen.ShowAsync(cancellationToken);

            if (action == TuiScreenAction.Quit)
            {
                return;
            }

            if (action != TuiScreenAction.OpenForm || screen.SelectedCommand is null)
            {
                continue;
            }

            await ShowFormAsync(
                console,
                session,
                screen.SelectedCommand,
                screen.SelectedPresetArguments,
                runner,
                cancellationToken);
        }
    }

    /// <summary>
    /// Opens the form for one command.
    /// </summary>
    /// <remarks>
    /// The form owns the command, and the command owns a dependency injection scope, so the
    /// 'using' is not decoration. An interface opens many forms in one process, which is
    /// exactly where a scope that is never released becomes a real leak.
    /// </remarks>
    private static async Task ShowFormAsync(
        IAnsiConsole console,
        TuiSession session,
        TuiCommandItem command,
        IReadOnlyDictionary<string, string>? presetArguments,
        TuiCommandRunner runner,
        CancellationToken cancellationToken)
    {
        try
        {
            using var form = TuiCommandForm.Open(
                session.Options, session.CommandsAssembly, command, presetArguments);

            await new CommandFormScreen(console, form, runner).ShowAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // one command that cannot be created -- an unregistered dependency, most likely
            // -- is not a reason to close the interface on everything else
            console.MarkupLine(
                $"[red]'{Markup.Escape(command.PathAsString)}' could not be opened: " +
                $"{Markup.Escape(ex.Message)}[/]");
        }
    }

    /// <summary>
    /// Draws the banner: which tool this is, and anything wrong with it.
    /// </summary>
    private static void RenderHeader(IAnsiConsole console, TuiSession session)
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
    /// Lists the commands, for a terminal that cannot be prompted.
    /// </summary>
    private static void RenderOverview(IAnsiConsole console, TuiCommandBrowser browser)
    {
        var tree = new Tree("[bold]Commands[/]");

        foreach (var category in browser.GetTree())
        {
            var categoryNode = tree.AddNode($"[bold]{Markup.Escape(category.Name)}[/]");

            foreach (var group in category.Groups)
            {
                var parent = group.HasGroup == true
                    ? categoryNode.AddNode($"[blue]{Markup.Escape(group.Name)}[/]")
                    : categoryNode;

                foreach (var command in group.Commands)
                {
                    parent.AddNode(Describe(command, group.HasGroup));
                }
            }
        }

        console.Write(tree);

        var aliases = browser.GetMatchingAliases();

        if (aliases.Count > 0)
        {
            var aliasTree = new Tree("[bold]Command aliases[/]");

            foreach (var alias in aliases)
            {
                aliasTree.AddNode(
                    $"{Markup.Escape(alias.Name)} [grey]- {Markup.Escape(alias.Description)}[/]");
            }

            console.WriteLine();
            console.Write(aliasTree);
        }

        console.WriteLine();
        console.MarkupLine(
            "[grey]This terminal is not interactive, so the list is all there is to show.[/]");
    }

    private static string Describe(TuiCommandItem command, bool insideGroup)
    {
        var label = Markup.Escape(command.GetLabel(insideGroup));

        return string.IsNullOrWhiteSpace(command.Description) == true
            ? label
            : $"{label} [grey]- {Markup.Escape(command.Description)}[/]";
    }
}
