namespace Benday.CommandsFramework;

/// <summary>
/// A name the framework claims for itself, along with what it does. These never appear in a
/// command's own argument list, which is why they used to be undiscoverable -- usage output
/// listed only the arguments a command declared, so nothing ever told anyone that --help,
/// --json, gui or quiet existed.
/// </summary>
public sealed class ReservedKeyword
{
    public ReservedKeyword(string name, string description)
    {
        Name = name;
        Description = description;
    }

    /// <summary>
    /// The reserved name as it is typed on the command line.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// What it does, for usage output.
    /// </summary>
    public string Description { get; }
}

/// <summary>
/// The names the framework reserves. This is the single source for both the usage output
/// that lists them and the argument validation that has to skip them.
/// </summary>
public static class ReservedKeywords
{
    /// <summary>
    /// Reserved names that apply to any command, listed in that command's usage output.
    /// </summary>
    public static IReadOnlyList<ReservedKeyword> ForCommands { get; } =
    [
        new ReservedKeyword(
            ArgumentFrameworkConstants.ArgumentHelpString,
            "Display this usage information instead of running the command."),
        new ReservedKeyword(
            CommandFrameworkConstants.CommandArgName_QuietMode,
            "Suppress this command's status output.")
    ];

    /// <summary>
    /// Reserved names that apply to the tool rather than to a command, listed in the tool's
    /// own usage output alongside the command list.
    /// </summary>
    public static IReadOnlyList<ReservedKeyword> ForPrograms { get; } =
    [
        new ReservedKeyword(
            ArgumentFrameworkConstants.ArgumentHelpString,
            "Display this usage information."),
        new ReservedKeyword(
            ArgumentFrameworkConstants.ArgumentJson,
            "Write the full command schema as JSON. This is what cmdui reads."),
        new ReservedKeyword(
            ArgumentFrameworkConstants.ArgumentGui,
            "Launch the cmdui web interface for this tool.")
    ];

    /// <summary>
    /// Every reserved name, with no duplicates.
    /// </summary>
    public static IReadOnlyList<string> AllNames { get; } =
        ForCommands.Concat(ForPrograms)
            .Select(x => x.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}
