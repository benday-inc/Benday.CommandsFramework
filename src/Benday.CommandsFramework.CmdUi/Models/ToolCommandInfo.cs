namespace Benday.CommandsFramework.CmdUi.Models;

public class ToolCommandInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsAsync { get; set; }

    /// <summary>
    /// Alternate names that can be typed in place of Name. These are plain renames.
    /// </summary>
    public string[] Aliases { get; set; } = [];

    /// <summary>
    /// Aliases that supply argument values in addition to renaming the command.
    /// </summary>
    public List<ToolCommandAliasInfo> CommandAliases { get; set; } = new();

    public List<ToolArgumentInfo> Arguments { get; set; } = new();
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
