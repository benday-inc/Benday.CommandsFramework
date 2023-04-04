namespace Benday.CommandsFramework;

/// <summary>
/// Information about the requested command execution.
/// </summary>
public class CommandExecutionInfo
{
    /// <summary>
    /// Requested command name. This is the first arg (args[0]) on the command line
    /// </summary>
    public string CommandName { get; set; } = string.Empty;

    /// <summary>
    /// The requested command line arguments parsed into key/value pairs
    /// </summary>
    public Dictionary<string, string> Arguments { get; set; } = new();
}