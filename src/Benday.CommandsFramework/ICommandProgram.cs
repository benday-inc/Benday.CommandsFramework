using System.Reflection;

namespace Benday.CommandsFramework;

public interface ICommandProgram
{
    /// <summary>
    /// Displays the usage information for the program and all available commands displayed by category.
    /// </summary>
    /// <param name="commands">List of commands</param>
    void DisplayCommandsWithCategories(List<CommandAttribute> commands);

    /// <summary>
    /// Displays the usage information for the program and all available commands displayed alphabetically.
    /// </summary>
    /// <param name="commands">List of commands</param>
    void DisplayCommandsWithoutCategories(List<CommandAttribute> commands);

    /// <summary>
    /// Displays the usage information for the program and all available commands.
    /// </summary>
    /// <param name="util"></param>
    void DisplayUsage(CommandAttributeUtility util);
    /// <summary>
    /// Runs whatever the command line asked for.
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <param name="cancellationToken">Cancels the command being run</param>
    /// <returns>The exit code the process should use if it is exiting. Nothing here assigns
    /// Environment.ExitCode -- that belongs to the console entry point.</returns>
    Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default);

    Assembly ImplementationAssembly { get; }
    ICommandProgramOptions Options { get; }
}
