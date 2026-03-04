namespace Benday.CommandsFramework.CmdUI;

public class AppState
{
    public string? ToolName { get; set; }
    public bool IsDiscoveryMode => ToolName == null;

    public event Action? OnToolChanged;

    public void SetTool(string toolName)
    {
        ToolName = toolName;
        OnToolChanged?.Invoke();
    }
}
