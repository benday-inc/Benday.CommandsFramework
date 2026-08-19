namespace Benday.CommandsFramework;

/// <summary>
/// Information about a command including name, description, and
/// required/optional arguments.
/// </summary>
public class CommandInfo
{
    public string Name { get; internal set; } = string.Empty;
    public string Description { get; internal set; } = string.Empty;
    public string Category { get; internal set; } = string.Empty;

    /// <summary>
    /// Group this command belongs to, which is part of how the command is typed. Empty for
    /// a flat command name. Unlike Category, which is only a display heading.
    /// </summary>
    public string Group { get; internal set; } = string.Empty;
    public bool IsAsync { get; internal set; }

    /// <summary>
    /// Alternate names that can be used on the command line in place of Name.
    /// These come from CommandAttribute.Aliases and are plain renames.
    /// </summary>
    public string[] Aliases { get; internal set; } = [];

    /// <summary>
    /// Aliases that supply argument values in addition to renaming the command.
    /// These come from CommandAliasAttribute.
    /// </summary>
    public List<CommandAliasInfo> CommandAliases { get; internal set; } = new();
    public ArgumentCollection Arguments { get; internal set; } = new ArgumentCollection();
}
