namespace Benday.CommandsFramework.Tui.Model;

/// <summary>
/// Keeps what a filter found by name ahead of what it found in prose.
/// </summary>
/// <remarks>
/// A tier rather than a weighting, so the two kinds of match cannot interleave: every name
/// match, however loose, sorts above every description match. Nothing depends on the size of
/// the number beyond it being larger than any score a single field can produce.
/// </remarks>
internal static class TuiMatchRanking
{
    /// <summary>
    /// Added to a score that came from a command's name, path or alias.
    /// </summary>
    public const int ByName = 10_000;
}

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
    /// called instead, what heading it is under, and what it does. The two kinds of text are
    /// matched differently and ranked apart, which is what keeps a filter usable on a tool
    /// with a lot of commands:
    ///
    /// A name is short and gets matched loosely, so 'wl' finds 'widget list'. A description is
    /// prose and gets matched only where the filter appears in it as typed -- scattered
    /// characters in a sentence are a coincidence, not a match. And anything found by name
    /// outranks anything found by description, because a user who types a word is looking for
    /// the command called that first.
    /// </remarks>
    /// <param name="filter">What was typed</param>
    /// <returns>The score, or 0 when it does not match</returns>
    public int Score(string? filter)
    {
        var byName = FuzzyMatch.BestScore(filter, PathAsString, Name);

        foreach (var alias in PlainAliases)
        {
            var score = FuzzyMatch.Score(alias, filter);

            if (score > byName)
            {
                byName = score;
            }
        }

        if (byName > 0)
        {
            return TuiMatchRanking.ByName + byName;
        }

        return FuzzyMatch.BestContiguousScore(filter, Category, Description);
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
    public int Score(string? filter)
    {
        var byName = FuzzyMatch.BestScore(filter, Name, Command.PathAsString);

        return byName > 0
            ? TuiMatchRanking.ByName + byName
            : FuzzyMatch.BestContiguousScore(filter, Description);
    }

    public override string ToString() => Name;
}
