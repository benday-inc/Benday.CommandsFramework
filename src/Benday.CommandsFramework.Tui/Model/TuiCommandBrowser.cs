namespace Benday.CommandsFramework.Tui.Model;

/// <summary>
/// The list of commands, filtered and arranged the way the browser draws it.
/// </summary>
/// <remarks>
/// Built from CommandRegistry.Registrations, so opening the browser instantiates nothing.
/// Everything that decides what appears and in what order is here; the renderer only draws
/// the result.
/// </remarks>
public sealed class TuiCommandBrowser
{
    private readonly List<TuiCommandItem> _AllCommands;
    private readonly List<TuiCommandAliasItem> _AllAliases;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="session">The tool being browsed</param>
    public TuiCommandBrowser(TuiSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        Session = session;

        _AllCommands = session.Registry.Registrations
            .Select(x => new TuiCommandItem(x))
            .ToList();

        // an alias that supplies argument values is a different thing from a rename and gets
        // its own section, the same way the console usage output separates them
        _AllAliases = _AllCommands
            .SelectMany(command => command.Registration.Aliases
                .Where(alias => alias.HasArguments == true)
                .Select(alias => new TuiCommandAliasItem(alias, command)))
            .OrderBy(x => x.Name, ArgumentCollection.ArgumentNameComparer)
            .ToList();
    }

    /// <summary>
    /// The tool being browsed.
    /// </summary>
    public TuiSession Session { get; }

    /// <summary>
    /// What the user has typed to narrow the list. Empty shows everything.
    /// </summary>
    public string Filter { get; set; } = string.Empty;

    /// <summary>
    /// When true, the framework's own configuration commands are included.
    /// </summary>
    /// <remarks>
    /// On by default. They are ordinary commands and a user in a terminal has as much reason
    /// to run check-configuration as any other command.
    /// </remarks>
    public bool IncludeBuiltInCommands { get; set; } = true;

    /// <summary>
    /// Every command, before filtering.
    /// </summary>
    public IReadOnlyList<TuiCommandItem> AllCommands => _AllCommands;

    /// <summary>
    /// Every alias that supplies argument values, before filtering.
    /// </summary>
    public IReadOnlyList<TuiCommandAliasItem> AllAliases => _AllAliases;

    /// <summary>
    /// The commands the current filter leaves, best match first.
    /// </summary>
    public IReadOnlyList<TuiCommandItem> GetMatchingCommands()
    {
        return _AllCommands
            .Where(x => IncludeBuiltInCommands == true || x.IsBuiltIn == false)
            .Select(x => (Item: x, Score: x.Score(Filter)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Item.PathAsString, ArgumentCollection.ArgumentNameComparer)
            .Select(x => x.Item)
            .ToList();
    }

    /// <summary>
    /// The argument-supplying aliases the current filter leaves, best match first.
    /// </summary>
    public IReadOnlyList<TuiCommandAliasItem> GetMatchingAliases()
    {
        return _AllAliases
            .Where(x => IncludeBuiltInCommands == true || x.Command.IsBuiltIn == false)
            .Select(x => (Item: x, Score: x.Score(Filter)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Item.Name, ArgumentCollection.ArgumentNameComparer)
            .Select(x => x.Item)
            .ToList();
    }

    /// <summary>
    /// True when the filter has left nothing to show.
    /// </summary>
    public bool HasNothingToShow =>
        GetMatchingCommands().Count == 0 && GetMatchingAliases().Count == 0;

    /// <summary>
    /// The matching commands arranged for display: by category, and within a category by the
    /// group a command declares.
    /// </summary>
    /// <remarks>
    /// Category and Group are different things and both matter. Category is a display heading
    /// like "Work Items"; Group is part of how the command is typed, which is why
    /// 'widget list' and 'widget show' nest under 'widget' rather than appearing as two
    /// unrelated names.
    /// </remarks>
    public IReadOnlyList<TuiCommandCategory> GetTree()
    {
        var matching = GetMatchingCommands();

        // a filter ranks by relevance, so ordering inside a category follows the same ranking
        // rather than re-sorting alphabetically and throwing the ranking away
        var ordering = matching
            .Select((item, index) => (item, index))
            .ToDictionary(x => x.item, x => x.index);

        return matching
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Category) == true ? "Commands" : x.Category)
            .Select(categoryGroup => new TuiCommandCategory(
                categoryGroup.Key,
                categoryGroup
                    .GroupBy(x => x.Group ?? string.Empty, ArgumentCollection.ArgumentNameComparer)
                    .Select(g => new TuiCommandGroup(
                        g.Key,
                        g.OrderBy(x => ordering[x]).ToList()))
                    // a flat command sits above the groups, which read as sub-headings
                    .OrderBy(x => x.HasGroup == true ? 1 : 0)
                    .ThenBy(x => x.Name, ArgumentCollection.ArgumentNameComparer)
                    .ToList()))
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

/// <summary>
/// One display heading in the browser, holding the commands filed under it.
/// </summary>
public sealed class TuiCommandCategory
{
    internal TuiCommandCategory(string name, IReadOnlyList<TuiCommandGroup> groups)
    {
        Name = name;
        Groups = groups;
    }

    /// <summary>
    /// The heading.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The commands under it, arranged by the group each declares.
    /// </summary>
    public IReadOnlyList<TuiCommandGroup> Groups { get; }

    /// <summary>
    /// How many commands are under this heading in total.
    /// </summary>
    public int CommandCount => Groups.Sum(x => x.Commands.Count);
}

/// <summary>
/// Commands that share a declared group, or the ungrouped ones when Name is empty.
/// </summary>
public sealed class TuiCommandGroup
{
    internal TuiCommandGroup(string name, IReadOnlyList<TuiCommandItem> commands)
    {
        Name = name ?? string.Empty;
        Commands = commands;
    }

    /// <summary>
    /// The first segment of a multi-level command name, or empty for flat commands.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// True when these commands are run as 'group name' rather than as a bare name.
    /// </summary>
    public bool HasGroup => string.IsNullOrWhiteSpace(Name) == false;

    /// <summary>
    /// The commands.
    /// </summary>
    public IReadOnlyList<TuiCommandItem> Commands { get; }
}
