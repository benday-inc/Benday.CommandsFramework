using System.Diagnostics;
using System.Reflection;

namespace Benday.CommandsFramework.Tui.Model;

/// <summary>
/// The form for one command: its fields, what is wrong with them, and the command line they
/// add up to.
/// </summary>
/// <remarks>
/// This is the only place a command is instantiated. The browser reads registrations and
/// creates nothing; opening a form creates that one command so it can be asked for its
/// arguments.
///
/// It owns the command, and the command owns a dependency injection scope, so this is
/// disposable and disposing it matters. An interface runs many commands in one process,
/// which is exactly the case where a scope that is never released becomes a real leak.
/// </remarks>
public sealed class TuiCommandForm : IDisposable
{
    private readonly CommandBase _Command;
    private readonly List<TuiField> _Fields;
    private bool _IsDisposed;

    private TuiCommandForm(
        TuiCommandItem command,
        CommandBase instance,
        string toolName,
        ArgumentSyntax syntax)
    {
        Command = command;
        _Command = instance;
        ToolName = toolName;
        Syntax = syntax;

        _Fields = instance.Arguments.Select(x => new TuiField(x)).ToList();
    }

    /// <summary>
    /// Which command this is a form for.
    /// </summary>
    public TuiCommandItem Command { get; }

    /// <summary>
    /// The name the tool is typed as, for the command line preview.
    /// </summary>
    public string ToolName { get; }

    /// <summary>
    /// Which argument syntax the tool accepts. The preview follows it, so a tool configured
    /// for Posix or Slash gets a preview it can actually parse.
    /// </summary>
    public ArgumentSyntax Syntax { get; }

    /// <summary>
    /// The fields, in the order the command declared its arguments.
    /// </summary>
    public IReadOnlyList<TuiField> Fields => _Fields;

    /// <summary>
    /// Rules about how the arguments combine. They print in the form the way they print in
    /// usage output; making a form apply them as it is filled in is a later phase.
    /// </summary>
    public IReadOnlyList<ArgumentRule> Rules => _Command.Arguments.Rules;

    /// <summary>
    /// Opens a form for a command.
    /// </summary>
    /// <param name="program">The program the command belongs to</param>
    /// <param name="command">The command to open</param>
    /// <param name="presetArguments">Argument values to start with, from a [CommandAlias]</param>
    /// <returns>The form. Dispose it when the form is closed.</returns>
    public static TuiCommandForm Open(
        ICommandProgram program,
        TuiCommandItem command,
        IReadOnlyDictionary<string, string>? presetArguments = null)
    {
        ArgumentNullException.ThrowIfNull(program);
        ArgumentNullException.ThrowIfNull(command);

        return Open(
            program.Options,
            program.ImplementationAssembly,
            command,
            presetArguments);
    }

    /// <summary>
    /// Opens a form for a command. Separate from the ICommandProgram overload so a test does
    /// not need a whole program.
    /// </summary>
    /// <param name="options">The program's options</param>
    /// <param name="commandsAssembly">Assembly holding the commands</param>
    /// <param name="command">The command to open</param>
    /// <param name="presetArguments">Argument values to start with, from a [CommandAlias]</param>
    /// <returns>The form. Dispose it when the form is closed.</returns>
    public static TuiCommandForm Open(
        ICommandProgramOptions options,
        Assembly commandsAssembly,
        TuiCommandItem command,
        IReadOnlyDictionary<string, string>? presetArguments = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(commandsAssembly);
        ArgumentNullException.ThrowIfNull(command);

        var utility = new CommandAttributeUtility(options);

        // built through the ordinary run path, so the instance the form edits is exactly the
        // instance that would run. Only the command name is passed: the values come from the
        // form, not from a command line nobody typed.
        var args = command.Registration.Path.ToArray();

        var instance = utility.GetCommand(args, commandsAssembly) ??
            throw new KnownException(
                $"Could not create the command '{command.PathAsString}'.");

        var form = new TuiCommandForm(
            command, instance, GetToolName(), options.ArgumentSyntax);

        // priming applies configuration values and any recorded defaults before the user
        // touches anything, so a value typed into the form is never overwritten afterwards
        form.Validate();

        if (presetArguments is not null)
        {
            form.ApplyPresets(presetArguments);
        }

        return form;
    }

    /// <summary>
    /// Puts the values a [CommandAlias] supplies into the fields, so a form opened from an
    /// alias starts where the alias leaves off.
    /// </summary>
    private void ApplyPresets(IReadOnlyDictionary<string, string> presetArguments)
    {
        foreach (var preset in presetArguments)
        {
            var field = FindField(preset.Key);

            if (field is null)
            {
                continue;
            }

            // an alias with a bare name means the flag is on, the same way it does on the
            // command line
            field.TrySetValue(
                string.IsNullOrEmpty(preset.Value) == true &&
                    field.Widget == TuiFieldWidget.Toggle
                    ? bool.TrueString
                    : preset.Value);
        }
    }

    /// <summary>
    /// Finds a field by argument name, without regard to case.
    /// </summary>
    /// <param name="name">The argument's name</param>
    /// <returns>The field, or null when the command has no such argument</returns>
    public TuiField? FindField(string name)
    {
        return _Fields.FirstOrDefault(
            x => ArgumentCollection.ArgumentNameComparer.Equals(x.Name, name) == true);
    }

    /// <summary>
    /// Checks the whole form and reports everything that is wrong with it.
    /// </summary>
    /// <remarks>
    /// This is the command's own validation, not a second implementation of it. Each failure
    /// already carries a written message worth showing as it is -- an argument that reads
    /// from stored configuration says which set-configuration call would supply it, and a
    /// discovery failure says how many files matched and what they were.
    /// </remarks>
    /// <returns>What is wrong, empty when nothing is</returns>
    public List<ValidationFailure> Validate()
    {
        return _Command.ValidateArguments();
    }

    /// <summary>
    /// True when the form is complete enough to run.
    /// </summary>
    public bool IsValid() => Validate().Count == 0;

    /// <summary>
    /// The command line the form's values add up to, as it would be typed.
    /// </summary>
    public string GetCommandLine()
    {
        return TuiCommandLine.GetDisplayText(
            ToolName, Command.PathAsString, _Fields, Syntax);
    }

    /// <summary>
    /// The command line the form's values add up to, as an argument array.
    /// </summary>
    /// <remarks>
    /// Running the command is a later phase, but this is what it will hand to the framework,
    /// and it is what makes the preview verifiable rather than decorative.
    /// </remarks>
    public string[] GetCommandLineTokens()
    {
        return TuiCommandLine
            .GetTokens(Command.PathAsString, _Fields, Syntax)
            .ToArray();
    }

    /// <summary>
    /// The name the tool is typed as.
    /// </summary>
    private static string GetToolName()
    {
        try
        {
            return Process.GetCurrentProcess().ProcessName;
        }
        catch
        {
            // there is always something to call it, and a preview is not worth failing over
            return Assembly.GetEntryAssembly()?.GetName().Name ?? "tool";
        }
    }

    /// <summary>
    /// Releases the command, and with it the dependency injection scope it owns.
    /// </summary>
    public void Dispose()
    {
        if (_IsDisposed == true)
        {
            return;
        }

        _IsDisposed = true;

        _Command.Dispose();
    }
}
