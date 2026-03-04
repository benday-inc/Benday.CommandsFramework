namespace Benday.CommandsFramework.CmdUi.Models;

public class ToolInfo
{
    public string ToolName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool SupportsJson { get; set; }
    public List<ToolCommandInfo> Commands { get; set; } = new();
}
