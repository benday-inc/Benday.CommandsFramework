namespace Benday.CommandsFramework.CmdUi;

public class AppState
{
    public string? ToolName { get; set; }
    public string WorkingDirectory { get; set; } = Environment.CurrentDirectory;
    public bool IsDiscoveryMode => ToolName == null;

    public event Action? OnToolChanged;
    public event Action? OnWorkingDirectoryChanged;

    public void SetTool(string toolName)
    {
        ToolName = toolName;
        OnToolChanged?.Invoke();
    }

    public void ClearTool()
    {
        ToolName = null;
        OnToolChanged?.Invoke();
    }

    public void SetWorkingDirectory(string path)
    {
        WorkingDirectory = path;
        OnWorkingDirectoryChanged?.Invoke();
    }
}
