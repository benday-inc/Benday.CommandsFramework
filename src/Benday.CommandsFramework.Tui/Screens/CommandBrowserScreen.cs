using Benday.CommandsFramework.Tui.Model;

using Spectre.Console;

namespace Benday.CommandsFramework.Tui.Screens;

/// <summary>
/// The list of commands. Read only -- picking one opens its form, and nothing runs from
/// here.
/// </summary>
/// <remarks>
/// Everything that decides what appears lives in TuiCommandBrowser. This draws it.
/// </remarks>
internal sealed class CommandBrowserScreen
{
    private readonly IAnsiConsole _Console;

    public CommandBrowserScreen(IAnsiConsole console, TuiCommandBrowser browser)
    {
        _Console = console;
        Browser = browser;
    }

    /// <summary>
    /// What the screen is showing.
    /// </summary>
    public TuiCommandBrowser Browser { get; }

    /// <summary>
    /// The command the user picked, when they picked one.
    /// </summary>
    public TuiCommandItem? SelectedCommand { get; private set; }

    /// <summary>
    /// The argument values that come with the selection, when it was reached through an
    /// alias that supplies them.
    /// </summary>
    public IReadOnlyDictionary<string, string>? SelectedPresetArguments { get; private set; }

    /// <summary>
    /// One entry in the list. A class rather than the command itself because the list also
    /// holds the filter entry and the way out.
    /// </summary>
    private sealed class Choice
    {
        public Choice(string label, TuiScreenAction action)
        {
            Label = label;
            Action = action;
        }

        public string Label { get; }

        public TuiScreenAction Action { get; }

        public TuiCommandItem? Command { get; init; }

        public IReadOnlyDictionary<string, string>? PresetArguments { get; init; }

        public bool IsFilterEntry { get; init; }

        public bool IsHeading { get; init; }
    }

    /// <summary>
    /// Draws the list and waits for a choice.
    /// </summary>
    /// <param name="cancellationToken">Stops the interface</param>
    /// <returns>What to do next</returns>
    public async Task<TuiScreenAction> ShowAsync(CancellationToken cancellationToken)
    {
        SelectedCommand = null;
        SelectedPresetArguments = null;

        var quit = new Choice("Quit", TuiScreenAction.Quit);

        var prompt = new SelectionPrompt<Choice>()
            .Title(GetTitle())
            .PageSize(20)
            .MoreChoicesText("[grey](move up and down for more)[/]")
            .UseConverter(x => x.Label)
            .AddCancelResult(quit);

        // group headers exist to be read, not chosen
        prompt.Mode = SelectionMode.Leaf;

        // typing jumps the highlight to the first command that matches, which is what a row
        // called 'Filter...' at the top of a list leads everyone to try first -- and what a
        // selection list otherwise swallows without a word. It moves rather than narrows: the
        // rest of the list stays on screen. The filter below is the one that narrows, keeps
        // what it was given across screens, ranks what it finds, and searches descriptions
        // and aliases as well as names.
        prompt.EnableSearch();

        // says what the entry does, because it does not look like what it is: a row that reads
        // 'Filter...' looks like somewhere to type, and a selection list silently swallows
        // anything typed at it, so there is no feedback saying the keystrokes went nowhere
        var filterLabel = string.IsNullOrWhiteSpace(Browser.Filter) == true
            ? "[blue]Filter...[/] [grey](press enter to narrow the list)[/]"
            : $"[blue]Filter:[/] {Markup.Escape(Browser.Filter)} " +
                "[grey](enter to change it, blank clears it)[/]";

        prompt.AddChoices(new Choice(filterLabel, TuiScreenAction.Stay) { IsFilterEntry = true });

        var anything = AddCommands(prompt) | AddAliases(prompt);

        prompt.AddChoices(quit);

        if (anything == false)
        {
            _Console.MarkupLine(
                $"[yellow]Nothing matches '{Markup.Escape(Browser.Filter)}'.[/]");
        }

        var choice = await _Console.PromptAsync(prompt, cancellationToken);

        if (choice.IsFilterEntry == true)
        {
            await PromptForFilterAsync(cancellationToken);

            return TuiScreenAction.Stay;
        }

        if (choice.Action == TuiScreenAction.OpenForm)
        {
            SelectedCommand = choice.Command;
            SelectedPresetArguments = choice.PresetArguments;
        }

        return choice.Action;
    }

    private string GetTitle()
    {
        var total = Browser.AllCommands.Count;
        var shown = Browser.GetMatchingCommands().Count;

        var counted = shown == total
            ? $"[bold]{total}[/] commands."
            : $"[bold]{shown}[/] of {total} commands.";

        return $"{counted} Pick one to fill in its arguments, or type to jump to one.";
    }

    /// <summary>
    /// Adds the commands, grouped by category and nested by the group each declares.
    /// </summary>
    /// <returns>True when anything was added</returns>
    private bool AddCommands(SelectionPrompt<Choice> prompt)
    {
        var tree = Browser.GetTree();

        if (tree.Count == 0)
        {
            return false;
        }

        foreach (var category in tree)
        {
            var children = new List<Choice>();

            foreach (var group in category.Groups)
            {
                foreach (var command in group.Commands)
                {
                    children.Add(ToChoice(command, group.HasGroup));
                }
            }

            prompt.AddChoiceGroup(
                new Choice($"[bold]{Markup.Escape(category.Name)}[/]", TuiScreenAction.Stay)
                {
                    IsHeading = true
                },
                children);
        }

        return true;
    }

    /// <summary>
    /// Adds the aliases that supply argument values. They get their own section because they
    /// are a different thing from a rename -- picking one opens a pre-filled form.
    /// </summary>
    /// <returns>True when anything was added</returns>
    private bool AddAliases(SelectionPrompt<Choice> prompt)
    {
        var aliases = Browser.GetMatchingAliases();

        if (aliases.Count == 0)
        {
            return false;
        }

        prompt.AddChoiceGroup(
            new Choice("[bold]Command aliases[/]", TuiScreenAction.Stay) { IsHeading = true },
            aliases.Select(alias => new Choice(
                Describe(Markup.Escape(alias.Name), alias.Description),
                TuiScreenAction.OpenForm)
            {
                Command = alias.Command,
                PresetArguments = alias.PresetArguments
            }));

        return true;
    }

    private static Choice ToChoice(TuiCommandItem command, bool insideGroup)
    {
        var label = Markup.Escape(command.GetLabel(insideGroup));

        if (insideGroup == true)
        {
            label = $"  {label}";
        }

        return new Choice(Describe(label, command.Description), TuiScreenAction.OpenForm)
        {
            Command = command
        };
    }

    private static string Describe(string label, string description)
    {
        return string.IsNullOrWhiteSpace(description) == true
            ? label
            : $"{label} [grey]- {Markup.Escape(description)}[/]";
    }

    /// <summary>
    /// Asks for the text to narrow the list by. An empty answer clears it.
    /// </summary>
    private async Task PromptForFilterAsync(CancellationToken cancellationToken)
    {
        var prompt = new TextPrompt<string>("Filter [grey](blank clears it)[/]:")
            .AllowEmpty();

        var filter = await _Console.PromptAsync(prompt, cancellationToken);

        Browser.Filter = filter?.Trim() ?? string.Empty;
    }
}
