namespace Benday.CommandsFramework;

public class CommandExecutionInfo
{
    public string CommandName { get; set; } = string.Empty;
    public Dictionary<string, string> Arguments { get; set; } = new();
}