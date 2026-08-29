using System.Reflection;

using Benday.CommandsFramework;

namespace Benday.CommandsFramework.Tui.Model;

/// <summary>
/// What the terminal interface knows about the tool it is showing. No rendering happens
/// here, which is what makes it testable without a terminal.
/// </summary>
/// <remarks>
/// Every decision the interface makes belongs in this layer. If a test needs a terminal in
/// order to check something, the something is in the wrong place.
///
/// Building this instantiates no commands. The registry is a list of registrations read from
/// attributes, so opening the interface costs about what shell completion costs rather than
/// what --json costs, and --json instantiates every command in the tool.
/// </remarks>
public sealed class TuiSession
{
    private TuiSession(
        string title,
        ICommandProgramOptions options,
        Assembly commandsAssembly,
        CommandRegistry registry)
    {
        Title = title;
        Options = options;
        CommandsAssembly = commandsAssembly;
        Registry = registry;
    }

    /// <summary>
    /// The tool's options. A form needs these to build the command it is a form for, and to
    /// know which argument syntax the command line preview should be written in.
    /// </summary>
    public ICommandProgramOptions Options { get; }

    /// <summary>
    /// The assembly holding the tool's commands.
    /// </summary>
    public Assembly CommandsAssembly { get; }

    /// <summary>
    /// The tool's name, or a stand-in when it has none. A blank title would render as an
    /// empty banner, which is worse than saying nothing useful is known.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// The tool's version. Empty when it was never configured.
    /// </summary>
    public string Version => Options.Version ?? string.Empty;

    /// <summary>
    /// The tool's website. Empty when it was never configured.
    /// </summary>
    public string Website => Options.Website ?? string.Empty;

    /// <summary>
    /// Which argument syntax the tool accepts. The command line preview follows it, so a
    /// tool configured for Posix or Slash gets a preview it can actually parse.
    /// </summary>
    public ArgumentSyntax ArgumentSyntax => Options.ArgumentSyntax;

    /// <summary>
    /// The commands this tool can run.
    /// </summary>
    public CommandRegistry Registry { get; }

    /// <summary>
    /// How many commands there are, the built-in configuration commands included.
    /// </summary>
    public int CommandCount => Registry.Registrations.Count;

    /// <summary>
    /// Anything that makes a command unreachable -- an alias shadowed by a real name, a
    /// reserved keyword collision, a [Command] on a class the framework cannot run.
    /// </summary>
    /// <remarks>
    /// Worth surfacing. Today these are only visible to a tool author who thought to assert
    /// on CommandRegistry.Problems from a unit test.
    /// </remarks>
    public IReadOnlyList<string> Problems => Registry.Problems;

    /// <summary>
    /// True when there is something the tool author should know about.
    /// </summary>
    public bool HasProblems => Problems.Count > 0;

    /// <summary>
    /// Reads everything the interface needs off the program.
    /// </summary>
    /// <param name="program">The program being shown</param>
    /// <returns>The session</returns>
    public static TuiSession FromProgram(ICommandProgram program)
    {
        ArgumentNullException.ThrowIfNull(program);

        return Create(
            program.Options,
            program.ImplementationAssembly);
    }

    /// <summary>
    /// Reads everything the interface needs off a set of options and the assembly holding
    /// the commands. Separate from FromProgram so a test does not need a whole program.
    /// </summary>
    /// <param name="options">The program's options</param>
    /// <param name="commandsAssembly">Assembly holding the command implementations</param>
    /// <returns>The session</returns>
    public static TuiSession Create(
        ICommandProgramOptions options, Assembly commandsAssembly)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(commandsAssembly);

        // GetRegistry caches onto options.CommandRegistry, so this shares the registry the
        // rest of the process already built rather than scanning the assembly again
        var registry = new CommandAttributeUtility(options).GetRegistry(commandsAssembly);

        var title = string.IsNullOrWhiteSpace(options.ApplicationName) == true
            ? commandsAssembly.GetName().Name ?? "Commands"
            : options.ApplicationName;

        return new TuiSession(title, options, commandsAssembly, registry);
    }
}
