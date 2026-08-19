namespace Benday.CommandsFramework.CmdUi.Models;

public class ToolCommandInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Group this command belongs to, which is part of how the command is typed -- unlike
    /// Category, which is only a display heading. Empty for a flat command name and for any
    /// tool built against a framework version that predates it.
    /// </summary>
    public string Group { get; set; } = string.Empty;

    public bool IsAsync { get; set; }

    /// <summary>
    /// The command as it is typed, group included.
    /// </summary>
    public string PathAsString =>
        string.IsNullOrWhiteSpace(Group) ? Name : $"{Group} {Name}";

    /// <summary>
    /// Alternate names that can be typed in place of Name. These are plain renames.
    /// </summary>
    public string[] Aliases { get; set; } = [];

    /// <summary>
    /// Aliases that supply argument values in addition to renaming the command.
    /// </summary>
    public List<ToolCommandAliasInfo> CommandAliases { get; set; } = new();

    public List<ToolArgumentInfo> Arguments { get; set; } = new();

    /// <summary>
    /// Rules about the combination of argument values -- which arguments go together, which
    /// cannot. Empty for a tool built against a framework version that predates them.
    /// </summary>
    public List<ToolArgumentRuleInfo> Rules { get; set; } = new();
}

/// <summary>
/// A rule about the combination of argument values. Switch on RuleType.
/// </summary>
public class ToolArgumentRuleInfo
{
    public string RuleType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string[] ArgumentNames { get; set; } = [];
    public string WhenArgumentName { get; set; } = string.Empty;
    public string WhenValue { get; set; } = string.Empty;
    public string[] RequiredNames { get; set; } = [];
    public string[] ForbiddenNames { get; set; } = [];
}

/// <summary>
/// An alias that runs a command with a preset set of argument values.
/// </summary>
public class ToolCommandAliasInfo
{
    public string Alias { get; set; } = string.Empty;
    public string CommandName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, string> Arguments { get; set; } = new();
}
