namespace Benday.CommandsFramework;

/// <summary>
/// Interface for defining a command as using async
/// execution.
/// </summary>
public interface IAsyncCommand
{
    /// <summary>
    /// Triggers async execution of this command
    /// </summary>
    /// <returns>The async task</returns>
    Task ExecuteAsync();
}
