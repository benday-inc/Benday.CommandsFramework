namespace Benday.CommandsFramework;

/// <summary>
/// Interface for definiting a command as using synchronous execution
/// </summary>
public interface ISynchronousCommand
{
    /// <summary>
    /// Triggers execution of this command
    /// </summary>
    void Execute();
}