namespace Benday.CommandsFramework;

/// <summary>
/// Which command line argument syntax a program accepts.
/// </summary>
/// <remarks>
/// The framework shipped with <c>/name:value</c>, which is the Windows convention and is what
/// MSBuild and the older Microsoft tools use. Every modern cross platform CLI -- git, docker,
/// the dotnet CLI, anything built on System.CommandLine -- uses the GNU long option form
/// instead, so that is what the framework leads with now. The slash form still parses, and is
/// deprecated.
/// </remarks>
public enum ArgumentSyntax
{
    /// <summary>
    /// Accept both the POSIX form and the deprecated slash form. Usage output, completion and
    /// error messages all render the POSIX form, and a slash argument produces a deprecation
    /// warning on the diagnostic channel.
    /// </summary>
    /// <remarks>
    /// The default, and deliberately the zero value so that an implementation of
    /// ICommandProgramOptions that predates this setting gets it.
    /// </remarks>
    Both = 0,

    /// <summary>
    /// Accept only the POSIX form: <c>--name value</c>, <c>--name=value</c>,
    /// <c>--name:value</c>, <c>-n value</c> and the bare <c>--flag</c>.
    /// </summary>
    Posix = 1,

    /// <summary>
    /// Accept only the original <c>/name:value</c> form. For a tool that is not ready to move
    /// and does not want the deprecation warning.
    /// </summary>
    Slash = 2
}

/// <summary>
/// Renders argument names the way the program's syntax setting says they should be typed.
/// </summary>
/// <remarks>
/// This exists because usage output, shell completion, validation messages and the command
/// alias summary all used to build "/name:value" strings of their own. If any one of them
/// disagrees with the parser, the tool tells people to type something it will not accept, so
/// they all go through here.
/// </remarks>
public static class ArgumentSyntaxFormatter
{
    /// <summary>
    /// Prefix for a long option name: "--" for POSIX, "/" for slash.
    /// </summary>
    public static string GetPrefix(this ArgumentSyntax syntax) =>
        syntax == ArgumentSyntax.Slash ? "/" : "--";

    /// <summary>
    /// An argument name as it is typed, with no value. Use for a boolean flag.
    /// </summary>
    public static string FormatName(this ArgumentSyntax syntax, string name) =>
        $"{syntax.GetPrefix()}{name}";

    /// <summary>
    /// An argument name and its value as they are typed together.
    /// </summary>
    /// <remarks>
    /// The POSIX form uses a space, which is the form people type. Callers that need a single
    /// shell token -- building an argument list to hand to another process, for instance --
    /// want FormatNameValueAsSingleToken instead.
    /// </remarks>
    public static string FormatNameValue(this ArgumentSyntax syntax, string name, string value) =>
        syntax == ArgumentSyntax.Slash
            ? $"/{name}:{value}"
            : $"--{name} {value}";

    /// <summary>
    /// An argument name and its value in one token, so it survives being passed as a single
    /// element of an argument array.
    /// </summary>
    public static string FormatNameValueAsSingleToken(
        this ArgumentSyntax syntax, string name, string value) =>
        syntax == ArgumentSyntax.Slash
            ? $"/{name}:{value}"
            : $"--{name}={value}";

    /// <summary>
    /// True when this setting accepts the POSIX form.
    /// </summary>
    public static bool AllowsPosix(this ArgumentSyntax syntax) =>
        syntax is ArgumentSyntax.Both or ArgumentSyntax.Posix;

    /// <summary>
    /// True when this setting accepts the deprecated slash form.
    /// </summary>
    public static bool AllowsSlash(this ArgumentSyntax syntax) =>
        syntax is ArgumentSyntax.Both or ArgumentSyntax.Slash;

    /// <summary>
    /// True when a slash argument should produce a deprecation warning. Only Both warns --
    /// a program that has deliberately selected Slash has said what it wants.
    /// </summary>
    public static bool WarnsAboutSlash(this ArgumentSyntax syntax) =>
        syntax == ArgumentSyntax.Both;
}
