namespace Benday.CommandsFramework;

/// <summary>
/// A name the framework claims for itself, along with what it does. These never appear in a
/// command's own argument list, which is why they used to be undiscoverable -- usage output
/// listed only the arguments a command declared, so nothing ever told anyone that --help,
/// --json, gui or quiet existed.
/// </summary>
public sealed class ReservedKeyword
{
    public ReservedKeyword(string name, string description, bool isArgument = false)
    {
        Name = name;
        Description = description;
        IsArgument = isArgument;
    }

    /// <summary>
    /// True when this is an argument rather than a bare keyword, and so is written with the
    /// program's argument prefix.
    /// </summary>
    /// <remarks>
    /// 'gui' and 'completion' are commands and are typed as they are. 'quiet' is an argument,
    /// so it is typed as '--quiet' or '/quiet' depending on the program's syntax -- it used to
    /// be listed here as a bare word, which is not something anyone could type.
    /// </remarks>
    public bool IsArgument { get; }

    /// <summary>
    /// The name as it should be typed under a given argument syntax.
    /// </summary>
    public string GetDisplayName(ArgumentSyntax syntax)
    {
        // '--help' and '--json' carry their own dashes, and keep them in every syntax --
        // they are the names the framework matches literally
        if (IsArgument == false || Name.StartsWith('-') == true)
        {
            return Name;
        }

        return syntax.FormatName(Name);
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
            "Suppress this command's status output.",
            isArgument: true)
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
            "Launch the cmdui web interface for this tool."),
        new ReservedKeyword(
            ArgumentFrameworkConstants.ArgumentTui,
            "Launch the terminal interface for this tool, when it was built with one."),
        new ReservedKeyword(
            ArgumentFrameworkConstants.CommandCompletion,
            "Print the shell completion script for this tool.")
    ];

    /// <summary>
    /// Every reserved name, with no duplicates.
    /// </summary>
    /// <remarks>
    /// Includes --complete, which is not listed in usage output because it exists for shell
    /// completion stubs rather than for people -- but it is still a name a command cannot
    /// have.
    /// </remarks>
    public static IReadOnlyList<string> AllNames { get; } =
        ForCommands.Concat(ForPrograms)
            .Select(x => x.Name)
            .Append(ArgumentFrameworkConstants.ArgumentComplete)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}
