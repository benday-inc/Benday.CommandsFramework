using Benday.CommandsFramework.Tui.Model;

using Spectre.Console;

namespace Benday.CommandsFramework.Tui.Screens;

/// <summary>
/// The form for one command: its fields, what is wrong with them, and the command line they
/// add up to.
/// </summary>
/// <remarks>
/// The form is useful for more than running: it fills a command line in correctly, in
/// whichever syntax the tool accepts, and hands it over to be typed or pasted. The interface
/// teaching the command line is the point, not a consolation, which is why the preview and
/// the copy are there even now that the command can be run from here.
/// </remarks>
internal sealed class CommandFormScreen
{
    private readonly IAnsiConsole _Console;
    private readonly TuiCommandForm _Form;
    private readonly TuiCommandRunner? _Runner;

    public CommandFormScreen(
        IAnsiConsole console, TuiCommandForm form, TuiCommandRunner? runner = null)
    {
        _Console = console;
        _Form = form;
        _Runner = runner;
    }

    /// <summary>
    /// What a line of the menu does.
    /// </summary>
    private enum MenuAction
    {
        EditField,
        Run,
        Copy,
        Back
    }

    private sealed class MenuItem
    {
        public MenuItem(MenuAction action, string label)
        {
            Action = action;
            Label = label;
        }

        public MenuAction Action { get; }

        public string Label { get; }

        public TuiField? Field { get; init; }
    }

    /// <summary>
    /// Draws the form and waits for a choice.
    /// </summary>
    /// <param name="cancellationToken">Stops the interface</param>
    /// <returns>What to do next</returns>
    public async Task<TuiScreenAction> ShowAsync(CancellationToken cancellationToken)
    {
        while (cancellationToken.IsCancellationRequested == false)
        {
            Render();

            var back = new MenuItem(
                MenuAction.Back, "Back to the commands [grey](or press esc)[/]");

            var prompt = new SelectionPrompt<MenuItem>()
                .Title(
                    "Pick a field to fill in." + Environment.NewLine +
                    "[grey]Press esc to go back to the commands.[/]")
                .PageSize(TuiLayout.GetPageSize(_Console.Profile.Height))
                .MoreChoicesText("[grey](move up and down for more, esc goes back)[/]")
                .UseConverter(x => x.Label)
                .AddCancelResult(back);

            foreach (var field in _Form.Fields)
            {
                prompt.AddChoices(
                    new MenuItem(MenuAction.EditField, GetFieldLabel(field)) { Field = field });
            }

            if (_Runner is not null)
            {
                prompt.AddChoices(new MenuItem(MenuAction.Run, GetRunLabel()));
            }

            prompt.AddChoices(
                new MenuItem(MenuAction.Copy, "Copy the command line"),
                back);

            var choice = await _Console.PromptAsync(prompt, cancellationToken);

            switch (choice.Action)
            {
                case MenuAction.EditField when choice.Field is not null:
                    await EditAsync(choice.Field, cancellationToken);
                    break;

                case MenuAction.Run when _Runner is not null:
                    await RunAsync(cancellationToken);
                    break;

                case MenuAction.Copy:
                    Copy();
                    break;

                case MenuAction.Back:
                    return TuiScreenAction.Back;
            }
        }

        return TuiScreenAction.Back;
    }

    /// <summary>
    /// Draws the whole form: what the command is, what its fields hold, what is wrong, and
    /// the command line it all adds up to.
    /// </summary>
    private void Render()
    {
        _Console.WriteLine();
        _Console.Write(new Rule($"[bold]{Markup.Escape(_Form.Command.PathAsString)}[/]")
        {
            Justification = Justify.Left
        });

        if (string.IsNullOrWhiteSpace(_Form.Command.Description) == false)
        {
            _Console.MarkupLine($"[grey]{Markup.Escape(_Form.Command.Description)}[/]");
        }

        _Console.WriteLine();

        RenderFields();
        RenderRules();
        RenderProblems();
        RenderCommandLine();
    }

    private void RenderFields()
    {
        if (_Form.Fields.Count == 0)
        {
            _Console.MarkupLine("[grey]This command takes no arguments.[/]");
            _Console.WriteLine();

            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Argument")
            .AddColumn("Value")
            .AddColumn("Notes");

        foreach (var field in _Form.Fields)
        {
            table.AddRow(
                new Markup(GetFieldName(field)),
                new Markup(GetFieldValue(field)),
                new Markup(GetFieldNotes(field)));
        }

        _Console.Write(table);
        _Console.WriteLine();
    }

    /// <summary>
    /// Shows the rules about how the arguments combine. Making the form apply them as it is
    /// filled in comes later; showing them is what usage output already does.
    /// </summary>
    private void RenderRules()
    {
        if (_Form.Rules.Count == 0)
        {
            return;
        }

        _Console.MarkupLine("[bold]Rules[/]");

        foreach (var rule in _Form.Rules)
        {
            _Console.MarkupLine($"[grey]-[/] {Markup.Escape(rule.Describe())}");
        }

        _Console.WriteLine();
    }

    /// <summary>
    /// Shows what is wrong with the form as it stands.
    /// </summary>
    /// <remarks>
    /// The messages are the command's own and are shown as they are. A required argument that
    /// reads from stored configuration already says which set-configuration call would supply
    /// it, and a discovery failure already says how many files matched and what they were --
    /// rewording either of those here could only make them worse.
    /// </remarks>
    private void RenderProblems()
    {
        var failures = _Form.Validate();

        if (failures.Count == 0)
        {
            _Console.MarkupLine("[green]Ready to run.[/]");
            _Console.WriteLine();

            return;
        }

        foreach (var failure in failures)
        {
            _Console.MarkupLine($"[yellow]![/] {Markup.Escape(failure.Message)}");
        }

        _Console.WriteLine();
    }

    private void RenderCommandLine()
    {
        _Console.Write(
            new Panel(new Markup($"[bold]{Markup.Escape(_Form.GetCommandLine())}[/]"))
            {
                Header = new PanelHeader("Command line"),
                Border = BoxBorder.Rounded,
                Expand = true
            });
    }

    private static string GetFieldName(TuiField field)
    {
        var name = Markup.Escape(field.Label);

        return field.IsRequired == true ? $"{name} [red]*[/]" : name;
    }

    private static string GetFieldValue(TuiField field)
    {
        if (field.Widget == TuiFieldWidget.Toggle)
        {
            return field.IsFlagSet == true ? "[green]on[/]" : "[grey]off[/]";
        }

        return field.HasValue == true && string.IsNullOrEmpty(field.Value) == false
            ? Markup.Escape(field.Value)
            : "[grey](not set)[/]";
    }

    /// <summary>
    /// Everything about the argument that is worth knowing while filling it in: what it is
    /// for, what it will accept, and where a value would come from if it is left blank.
    /// </summary>
    private static string GetFieldNotes(TuiField field)
    {
        var notes = new List<string>();

        if (string.IsNullOrWhiteSpace(field.Description) == false &&
            string.Equals(field.Description, field.Name, StringComparison.Ordinal) == false)
        {
            notes.Add(Markup.Escape(field.Description));
        }

        if (field.AllowedValues.Count > 0)
        {
            notes.Add($"[grey]one of: {Markup.Escape(string.Join(", ", field.AllowedValues))}[/]");
        }

        if (field.HasDefaultValue == true &&
            string.IsNullOrWhiteSpace(field.DefaultValue) == false)
        {
            notes.Add($"[grey]default: {Markup.Escape(field.DefaultValue)}[/]");
        }

        if (field.IsFromConfig == true)
        {
            notes.Add("[grey]reads from stored configuration[/]");
        }

        if (field.IsDiscoverable == true)
        {
            notes.Add($"[grey]{Markup.Escape(field.DiscoveryHint)}[/]");
        }

        if (field.IsPositional == true)
        {
            notes.Add($"[grey]positional {field.Position}[/]");
        }

        if (field.MustExist == true)
        {
            notes.Add("[grey]has to exist[/]");
        }

        return notes.Count == 0 ? string.Empty : string.Join(Environment.NewLine, notes);
    }

    private static string GetFieldLabel(TuiField field)
    {
        var value = field.Widget == TuiFieldWidget.Toggle
            ? (field.IsFlagSet == true ? "on" : "off")
            : field.Value;

        var shown = string.IsNullOrEmpty(value) == true
            ? "[grey](not set)[/]"
            : Markup.Escape(value);

        var required = field.IsRequired == true ? " [red]*[/]" : string.Empty;

        return $"{Markup.Escape(field.Label)}{required} = {shown}";
    }

    /// <summary>
    /// Asks for a value for one field, drawn the way the argument's own type calls for.
    /// </summary>
    private async Task EditAsync(TuiField field, CancellationToken cancellationToken)
    {
        var value = field.Widget switch
        {
            TuiFieldWidget.Selection => await PromptForChoiceAsync(field, cancellationToken),
            TuiFieldWidget.Toggle => await PromptForToggleAsync(field, cancellationToken),
            _ => await PromptForTextAsync(field, cancellationToken)
        };

        if (value is null)
        {
            return;
        }

        if (field.TrySetValue(value) == false)
        {
            _Console.MarkupLine(
                $"[red]{Markup.Escape(field.Label)} would not take '{Markup.Escape(value)}'.[/]");
        }
    }

    private async Task<string?> PromptForChoiceAsync(
        TuiField field, CancellationToken cancellationToken)
    {
        const string Cancel = "(leave it as it is)";

        var prompt = new SelectionPrompt<string>()
            .Title($"{Markup.Escape(field.Label)}:")
            .PageSize(15)
            .AddCancelResult(Cancel);

        prompt.AddChoices(field.AllowedValues);
        prompt.AddChoices(Cancel);

        var choice = await _Console.PromptAsync(prompt, cancellationToken);

        return choice == Cancel ? null : choice;
    }

    private async Task<string?> PromptForToggleAsync(
        TuiField field, CancellationToken cancellationToken)
    {
        var prompt = new ConfirmationPrompt($"{Markup.Escape(field.Label)}?")
        {
            DefaultValue = field.IsFlagSet
        };

        var answer = await _Console.PromptAsync(prompt, cancellationToken);

        return answer ? bool.TrueString : bool.FalseString;
    }

    private async Task<string?> PromptForTextAsync(
        TuiField field, CancellationToken cancellationToken)
    {
        var prompt = new TextPrompt<string>($"{Markup.Escape(field.Label)}:")
            .AllowEmpty()
            .Validate(value =>
            {
                // an empty answer means leaving it unset rather than a value that is wrong,
                // and it is the only way out of a prompt. Required-ness is reported by the
                // form, where the user can see it and still get to another field.
                if (string.IsNullOrEmpty(value) == true)
                {
                    return ValidationResult.Success();
                }

                var problem = field.GetProblemWithValue(value);

                return string.IsNullOrEmpty(problem) == true
                    ? ValidationResult.Success()
                    : ValidationResult.Error($"[red]{Markup.Escape(problem)}[/]");
            });

        if (string.IsNullOrEmpty(field.Value) == false)
        {
            prompt.DefaultValue(field.Value);
        }

        var value = await _Console.PromptAsync(prompt, cancellationToken);

        return string.IsNullOrEmpty(value) == true ? null : value;
    }

    /// <summary>
    /// What the run line says. A form that is not filled in still offers to run: the command
    /// is the authority on whether its arguments are valid, and saying so in its own words is
    /// more use than a menu item that refuses to be picked.
    /// </summary>
    private string GetRunLabel()
    {
        return _Form.IsValid() == true
            ? "Run it"
            : "Run it [grey](something is still missing)[/]";
    }

    /// <summary>
    /// Runs the command and comes back to the form afterwards, so a run can be adjusted and
    /// run again.
    /// </summary>
    private async Task RunAsync(CancellationToken cancellationToken)
    {
        await new CommandRunScreen(_Console, _Runner!).ShowAsync(_Form, cancellationToken);

        if (_Console.Profile.Capabilities.Interactive == true)
        {
            _Console.Prompt(
                new TextPrompt<string>("[grey]Press enter to go back to the form.[/]")
                    .AllowEmpty());
        }
    }

    /// <summary>
    /// Puts the command line on the clipboard, and says so either way.
    /// </summary>
    private void Copy()
    {
        var commandLine = _Form.GetCommandLine();

        if (Clipboard.TryCopy(commandLine) == true)
        {
            _Console.MarkupLine("[green]Copied to the clipboard.[/]");

            return;
        }

        // the line is on screen either way, so a machine with no clipboard tool loses
        // nothing but the shortcut
        _Console.MarkupLine(
            "[yellow]There is no clipboard tool here, so copy it from the line above.[/]");
    }
}
