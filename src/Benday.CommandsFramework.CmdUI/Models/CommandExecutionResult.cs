namespace Benday.CommandsFramework.CmdUI.Models;

public class CommandExecutionResult
{
    public string StandardOutput { get; set; } = string.Empty;
    public string StandardError { get; set; } = string.Empty;
    public int ExitCode { get; set; }
    public bool IsSuccess => ExitCode == 0;
    public string CommandLine { get; set; } = string.Empty;
    public DateTime ExecutedAt { get; set; } = DateTime.Now;
}
