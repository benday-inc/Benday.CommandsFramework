namespace Benday.CommandsFramework.Tui.Model;

/// <summary>
/// One command as the browser shows it. A view over the registration -- no command is
/// instantiated to build this.
/// </summary>
public sealed class TuiCommandItem
{
    internal TuiCommandItem(CommandRegistration registration)
    {
        Registration = registration;

        // a plain alias is another name for the command and belongs next to the name. An
        // alias that supplies argument values is a different thing and gets its own section.
        PlainAliases = registration.Aliases
            .Where(x => x.HasArguments == false)
            .Select(x => x.Alias)
            .Where(x => string.IsNullOrWhiteSpace(x) == false)
            .ToList();
    }

    /// <summary>
    /// What the registry knows about this command.
    /// </summary>
    public CommandRegistration Registration { get; }

    /// <summary>
    /// The command's name as it is typed, group included.
    /// </summary>
    public string PathAsString => Registration.PathAsString;

    /// <summary>
    /// The command's own name -- the last segment of the path.
    /// </summary>
    public string Name => Registration.Name;

    /// <summary>
    /// The group the command is run under, or empty for a flat command name. This is part of
    /// how the command is typed, unlike Category.
    /// </summary>
    public string Group => Registration.Group;

    /// <summary>
    /// The display heading the command is filed under. Not part of the name.
    /// </summary>
    public string Category => Registration.Category;

    /// <summary>
    /// What the command does.
    /// </summary>
    public string Description => Registration.Description;

    /// <summary>
    /// True for the framework's own configuration commands.
    /// </summary>
    public bool IsBuiltIn => Registration.IsBuiltIn;

    /// <summary>
    /// Other names this command answers to that only rename it.
    /// </summary>
    public IReadOnlyList<string> PlainAliases { get; }

    /// <summary>
    /// The label the browser shows, which is the same shape the console command list uses:
    /// 'name (alias1, alias2)'.
    /// </summary>
    /// <remarks>
    /// Inside a group the group segment is already the heading above it, so only the last
    /// segment is repeated here.
    /// </remarks>
    public string GetLabel(bool insideGroup)
    {
        var name = insideGroup == true ? Name : PathAsString;

        return PlainAliases.Count == 0
            ? name
            : $"{name} ({string.Join(", ", PlainAliases)})";
    }

    /// <summary>
    /// How well a filter matches this command.
    /// </summary>
    /// <remarks>
    /// Across everything the user might remember about it -- what it is called, what it is
    /// called instead, what heading it is under, and what it does.
    /// </remarks>
    /// <param name="filter">What was typed</param>
    /// <returns>The score, or 0 when it does not match</returns>
    public int Score(string? filter)
    {
        var best = FuzzyMatch.BestScore(filter, PathAsString, Name, Category, Description);

        foreach (var alias in PlainAliases)
        {
            var score = FuzzyMatch.Score(alias, filter);

            if (score > best)
            {
                best = score;
            }
        }

        return best;
    }

    public override string ToString() => PathAsString;
}

/// <summary>
/// A [CommandAlias] preset as the browser shows it: a name that runs a command with some of
/// its arguments already filled in.
/// </summary>
public sealed class TuiCommandAliasItem
{
    internal TuiCommandAliasItem(CommandAliasInfo alias, TuiCommandItem command)
    {
        Alias = alias;
        Command = command;
    }

    /// <summary>
    /// What the alias supplies.
    /// </summary>
    public CommandAliasInfo Alias { get; }

    /// <summary>
    /// The command it runs.
    /// </summary>
    public TuiCommandItem Command { get; }

    /// <summary>
    /// The name that gets typed.
    /// </summary>
    public string Name => Alias.Alias;

    /// <summary>
    /// What the alias does, falling back to the command's own description.
    /// </summary>
    public string Description =>
        string.IsNullOrWhiteSpace(Alias.Description) == true
            ? Command.Description
            : Alias.Description;

    /// <summary>
    /// The argument values the alias supplies. A form opened from here starts pre-filled
    /// with these.
    /// </summary>
    public IReadOnlyDictionary<string, string> PresetArguments => Alias.Arguments;

    /// <summary>
    /// How well a filter matches.
    /// </summary>
    /// <param name="filter">What was typed</param>
    /// <returns>The score, or 0 when it does not match</returns>
    public int Score(string? filter) =>
        FuzzyMatch.BestScore(filter, Name, Description, Command.PathAsString);

    public override string ToString() => Name;
}
