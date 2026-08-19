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
