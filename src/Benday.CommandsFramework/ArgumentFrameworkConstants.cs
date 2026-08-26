namespace Benday.CommandsFramework;

/// <summary>
/// Constant strings used by the argument framework.
/// </summary>
public static class ArgumentFrameworkConstants
{
    /// <summary>
    /// Help request string argument
    /// </summary>
    public const string ArgumentHelpString = "--help";
    public const string ArgumentJson = "--json";
    public const string ArgumentGui = "gui";

    /// <summary>
    /// Command that launches the terminal user interface for this tool.
    /// </summary>
    /// <remarks>
    /// Reserved whether or not a tool was built with TUI support. A tool that defined its own
    /// command named 'tui' would find that the keyword won and the command could never run,
    /// so the name is claimed here and CommandRegistry.Problems reports the collision.
    /// </remarks>
    public const string ArgumentTui = "tui";

    /// <summary>
    /// Hidden keyword that a shell completion stub calls back into with the command line so
    /// far. Not listed as a reserved keyword in usage output, because it is for shells rather
    /// than for people.
    /// </summary>
    public const string ArgumentComplete = "--complete";

    /// <summary>
    /// Command that prints the shell completion stub for a shell.
    /// </summary>
    public const string CommandCompletion = "completion";
}
