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
    void Run(string[] args);

    Assembly ImplementationAssembly { get; }
    ICommandProgramOptions Options { get; }
}
