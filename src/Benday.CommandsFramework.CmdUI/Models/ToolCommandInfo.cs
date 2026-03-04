namespace Benday.CommandsFramework.CmdUI.Models;

public class ToolCommandInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsAsync { get; set; }
    public List<ToolArgumentInfo> Arguments { get; set; } = new();
}
