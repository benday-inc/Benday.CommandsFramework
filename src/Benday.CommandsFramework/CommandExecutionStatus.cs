namespace Benday.CommandsFramework;

/// <summary>
/// How a command run ended.
/// </summary>
public enum CommandExecutionStatus
{
    /// <summary>
    /// The command ran and did what it was asked to do.
    /// </summary>
    Success,

    /// <summary>
    /// The command did not run because its arguments were not valid.
    /// </summary>
    ValidationFailed,

    /// <summary>
    /// The command did not run because usage information was requested instead.
    /// </summary>
    UsageDisplayed,

    /// <summary>
    /// The command ran and failed.
    /// </summary>
    Failed,

    /// <summary>
    /// The command was cancelled before it finished.
    /// </summary>
    Cancelled
}
