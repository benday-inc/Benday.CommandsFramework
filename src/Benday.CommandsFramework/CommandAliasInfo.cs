namespace Benday.CommandsFramework;

/// <summary>
/// A single alternate name for a command. This covers both the plain aliases declared
/// by CommandAttribute.Aliases and the aliases declared by CommandAliasAttribute, which
/// also supply argument values.
/// </summary>
public class CommandAliasInfo
{
    /// <summary>
    /// The alias that gets typed on the command line.
    /// </summary>
    public string Alias { get; internal set; } = string.Empty;

    /// <summary>
    /// The real name of the command that this alias runs.
    /// </summary>
    public string CommandName { get; internal set; } = string.Empty;

    /// <summary>
    /// Human readable description of what this alias does.
    /// </summary>
    public string Description { get; internal set; } = string.Empty;

    /// <summary>
    /// Argument values supplied by this alias. Empty for a plain alias that only
    /// renames the command.
    /// </summary>
    public Dictionary<string, string> Arguments { get; internal set; } = new();

    /// <summary>
    /// True when this alias supplies argument values in addition to renaming the command.
    /// </summary>
    public bool HasArguments => Arguments.Count > 0;
}
