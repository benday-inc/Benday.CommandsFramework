using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

namespace Benday.CommandsFramework;

/// <summary>
/// Base class for all command implementations
/// </summary>
public abstract class CommandBase : IDisposable
{
    private readonly CommandExecutionInfo _Info;
    protected readonly ITextOutputProvider _OutputProvider;
    private ArgumentCollection _Arguments;
    private bool _HaveValuesBeenSet = false;
    private IServiceScope? _ServiceScope;
    private bool _OwnsServiceScope;
    private bool _IsDisposed;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="info">Execution information for the requested command</param>
    /// <param name="outputProvider">Output provider</param>
    /// <exception cref="ArgumentNullException"></exception>
    public CommandBase(CommandExecutionInfo info, ITextOutputProvider outputProvider)
    {
        if (outputProvider is null)
        {
            throw new ArgumentNullException(nameof(outputProvider));
        }

        _Info = info ?? throw new ArgumentNullException(nameof(info));
        _OutputProvider = outputProvider;
        _Arguments = GetArguments();
    }

    /// <summary>
    /// Gives this command a dependency injection scope to resolve services from.
    /// </summary>
    /// <remarks>
    /// The scope belongs to whoever created the command. A command run from the command line
    /// gets its own and disposes it when it is disposed; a command run by another command
    /// shares the caller's, because a call chain that is logically one operation should see
    /// one set of scoped services. Before this, every command created its own scope lazily
    /// and nothing ever disposed it -- harmless in a one shot CLI and a real leak in a host
    /// that runs many commands in one process.
    /// </remarks>
    /// <param name="scope">Scope to use</param>
    /// <param name="ownsScope">True when disposing this command should dispose the scope</param>
    internal void SetServiceScope(IServiceScope scope, bool ownsScope)
    {
        _ServiceScope = scope;
        _OwnsServiceScope = ownsScope;
    }

    /// <summary>
    /// The dependency injection scope this command resolves services from. Created on first
    /// use when nobody handed one in, which is what happens when a command is constructed
    /// directly rather than through the framework.
    /// </summary>
    private IServiceScope Scope
    {
        get
        {
            if (_ServiceScope is null)
            {
                _ServiceScope = CommandFrameworkUtilities
                    .GetServiceProvider(ExecutionInfo.Options)
                    .CreateScope();

                _OwnsServiceScope = true;
            }

            return _ServiceScope;
        }
    }

    /// <summary>
    /// Get a required service instance from the service provider.
    /// </summary>
    /// <remarks>
    /// Prefer declaring the dependency as a constructor parameter -- commands are created
    /// through ActivatorUtilities, so anything registered can be injected. This is the escape
    /// hatch for a service that can only be resolved once the command knows its arguments.
    /// </remarks>
    /// <typeparam name="T">Service type</typeparam>
    /// <returns>The service</returns>
    protected T GetRequiredService<T>() where T : notnull
    {
        try
        {
            return Scope.ServiceProvider.GetRequiredService<T>();
        }
        catch (InvalidOperationException ex)
        {
            // "no service for type X" is the right message when the program registered other
            // things and forgot this one. When it registered nothing at all, the real problem
            // is one level up and saying so saves a hunt.
            var services = ExecutionInfo.Options.ServiceCollection;

            if (services is null || services.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Could not resolve '{typeof(T).Name}' because the service collection was " +
                    "not populated. HINT: check Program.cs", ex);
            }

            throw;
        }
    }

    /// <summary>
    /// Releases the dependency injection scope, when this command owns one.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_IsDisposed == true)
        {
            return;
        }

        if (disposing == true && _OwnsServiceScope == true)
        {
            _ServiceScope?.Dispose();
        }

        _ServiceScope = null;
        _IsDisposed = true;
    }

    /// <summary>
    /// Property for accessing the raw execution info for the command
    /// </summary>
    public CommandExecutionInfo ExecutionInfo
    {
        get
        {
            return _Info;
        }
    }

    /// <summary>
    /// Arguments and values for the command. These are the combination of the argument definitions
    /// with the values from the command line. The values are set when the command is executed.
    /// </summary>
    public ArgumentCollection Arguments 
    { 
        get
        {
            return _Arguments; 
        }        
    }

    public string GetArgValueOrRequiredConfigValue(string argumentName, string configValueName)
    {
        string returnValue;

        if (Arguments.HasValue(argumentName) == false)
        {
            returnValue = ExecutionInfo.GetRequiredConfigurationValue(configValueName);
        }
        else
        {
            returnValue = Arguments.GetStringValue(argumentName);
        }

        return returnValue;
    }

    /// <summary>
    /// Get the argument definitions for the command execution. These are used to validate the execution
    /// but do not have any actual values.
    /// </summary>
    /// <returns></returns>
    public virtual ArgumentCollection GetArguments()
    {
        return new();
    }

    /// <summary>
    /// Human readable description of this command
    /// </summary>
    public string Description
    {
        get
        {
            var attribute = 
                Attribute.GetCustomAttribute(GetType(), typeof(CommandAttribute)) as CommandAttribute;

            if (attribute != null)
            {
                return attribute.Description;
            }
            else
            {
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// Is this command running in quiet mode? Quiet mode suppresses the output written
    /// by WriteLine(). It is set by the reserved 'quiet' argument and is applied
    /// automatically to commands that are run by another command.
    /// </summary>
    public bool IsQuietMode
    {
        get
        {
            if (ExecutionInfo.Arguments.TryGetValue(
                CommandFrameworkConstants.CommandArgName_QuietMode, out var value) == false)
            {
                return false;
            }

            // '/quiet' on its own means quiet, as does '/quiet:true'
            if (string.IsNullOrEmpty(value) == true)
            {
                return true;
            }

            return bool.TryParse(value, out var parsed) == false || parsed;
        }
    }

    /// <summary>
    /// Write a message to the output provider. Does nothing in quiet mode.
    /// </summary>
    /// <param name="text">Message to write</param>
    protected virtual void WriteLine(string text)
    {
        if (IsQuietMode == true)
        {
            return;
        }

        _OutputProvider.WriteLine(text);
    }

    /// <summary>
    /// Write a new line to the output provider. Does nothing in quiet mode.
    /// </summary>
    protected virtual void WriteLine()
    {
        if (IsQuietMode == true)
        {
            return;
        }

        _OutputProvider.WriteLine();
    }

    /// <summary>
    /// Write a message to the output provider without a trailing newline. Does nothing in
    /// quiet mode. Use this for a prompt, so that the answer is typed on the same line.
    /// </summary>
    /// <param name="text">Message to write</param>
    protected virtual void Write(string text)
    {
        if (IsQuietMode == true)
        {
            return;
        }

        _OutputProvider.Write(text);
    }

    /// <summary>
    /// Write a line of commentary about the work -- progress, notes, anything that is not
    /// the result this command was asked to produce. Goes to the diagnostic channel, so a
    /// caller redirecting the result never sees it mixed in. Does nothing in quiet mode.
    /// </summary>
    /// <param name="text">Message to write</param>
    protected virtual void WriteStatus(string text)
    {
        if (IsQuietMode == true)
        {
            return;
        }

        _OutputProvider.WriteStatus(text);
    }

    /// <summary>
    /// Write an error message. Goes to the diagnostic channel, and is <b>not</b> suppressed
    /// by quiet mode -- silencing the chatter should never silence a failure.
    /// </summary>
    /// <param name="text">Message to write</param>
    protected virtual void WriteError(string text)
    {
        _OutputProvider.WriteError(text);
    }

    /// <summary>
    /// Where this command reads text input from. Comes from the program options, so a test
    /// can hand the command a QueuedTextInputProvider and drive an interactive command
    /// without a console.
    /// </summary>
    protected ITextInputProvider InputProvider
    {
        get
        {
            return ExecutionInfo.Options.InputProvider;
        }
    }

    /// <summary>
    /// Read a line of input.
    /// </summary>
    /// <returns>The line that was read, or null when there is no more input.</returns>
    protected string? ReadLine()
    {
        return InputProvider.ReadLine();
    }

    /// <summary>
    /// Write a prompt and read the answer.
    /// </summary>
    /// <param name="prompt">Prompt to display. Written without a trailing newline, so the
    /// answer is typed on the same line.</param>
    /// <returns>The answer with surrounding whitespace trimmed, or null when there is no
    /// more input.</returns>
    protected string? Prompt(string prompt)
    {
        Write(prompt);

        return ReadLine()?.Trim();
    }

    /// <summary>
    /// Write a prompt and read a yes or no answer.
    /// </summary>
    /// <param name="prompt">Prompt to display, without the "(Y/n)" part -- that is added
    /// from defaultAnswer.</param>
    /// <param name="defaultAnswer">Answer to use when the user presses enter without typing
    /// anything, and when there is no more input.</param>
    /// <returns>True for yes, false for no</returns>
    protected bool PromptForYesNo(string prompt, bool defaultAnswer = true)
    {
        var suffix = defaultAnswer == true ? " (Y/n): " : " (y/N): ";

        var answer = Prompt($"{prompt}{suffix}");

        if (string.IsNullOrWhiteSpace(answer) == true)
        {
            return defaultAnswer;
        }

        return
            string.Equals(answer, "y", StringComparison.OrdinalIgnoreCase) == true ||
            string.Equals(answer, "yes", StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Creates another command so that its logic can be reused from inside this command.
    /// The new command shares this command's program options, configuration and output
    /// provider, and runs in quiet mode by default so that it does not write over the
    /// calling command's output.
    /// The command is created but not run -- use ExecuteCommand() or ExecuteCommandAsync()
    /// to create and run it in one step.
    /// </summary>
    /// <typeparam name="T">Type of the command to create</typeparam>
    /// <param name="configureArguments">Callback for populating the arguments for the command</param>
    /// <param name="quiet">Run the command in quiet mode. Defaults to true.</param>
    /// <returns>The new command instance</returns>
    /// <exception cref="KnownException">Thrown when the command type cannot be used</exception>
    protected T CreateCommand<T>(
        Action<CommandArgumentValues>? configureArguments = null,
        bool quiet = true) where T : CommandBase
    {
        var commandType = typeof(T);

        if (ExecutionInfo.NestingDepth >= CommandFrameworkConstants.MaxCommandNestingDepth)
        {
            throw new KnownException(
                $"Commands are nested more than {CommandFrameworkConstants.MaxCommandNestingDepth} " +
                $"levels deep while trying to create '{commandType.Name}'. " +
                "This usually means two commands are calling each other in a loop.");
        }

        var attribute =
            Attribute.GetCustomAttribute(commandType, typeof(CommandAttribute)) as CommandAttribute;

        if (attribute is null)
        {
            throw new KnownException(
                $"Type '{commandType.Name}' does not have a CommandAttribute so it cannot be run as a command.");
        }

        var values = new CommandArgumentValues();

        configureArguments?.Invoke(values);

        var arguments = values.Values;

        if (quiet == true)
        {
            arguments.TryAdd(CommandFrameworkConstants.CommandArgName_QuietMode, "true");
        }

        var info = new CommandExecutionInfo
        {
            Request = new CommandCallRequest(attribute.Name, arguments),
            Options = ExecutionInfo.Options,
            NestingDepth = ExecutionInfo.NestingDepth + 1
        };

        if (ExecutionInfo.HasConfiguration == true)
        {
            info.Configuration = ExecutionInfo.Configuration;
        }

        // the calling command's output provider is passed along rather than the one from
        // the program options so that output from the command that gets run lands
        // wherever the calling command's output is going
        var instance = ActivatorUtilities.CreateInstance(
            Scope.ServiceProvider, commandType, info, _OutputProvider);

        if (instance is not T returnValue)
        {
            throw new KnownException($"Could not create an instance of command type '{commandType.Name}'.");
        }

        // the command being called shares this command's scope and does not own it. A call
        // chain that is logically one operation should see one set of scoped services, and
        // giving the child its own scope made them inconsistent across the chain.
        returnValue.SetServiceScope(Scope, false);

        return returnValue;
    }

    /// <summary>
    /// Creates another command, validates it, and runs it. The command instance is
    /// returned so that results can be read back off it.
    /// Unlike running a command from the command line, a validation failure here throws
    /// rather than printing the usage information, because the calling command needs to
    /// know that the command did not run.
    /// </summary>
    /// <remarks>
    /// This used to save and restore Environment.ExitCode around the call, so that a command
    /// run by another command could not decide the exit code of the process. Commands return
    /// a CommandResult now and nothing here touches the process exit code, so there is
    /// nothing to contain.
    /// </remarks>
    /// <typeparam name="T">Type of the command to run</typeparam>
    /// <param name="configureArguments">Callback for populating the arguments for the command</param>
    /// <param name="quiet">Run the command in quiet mode. Defaults to true.</param>
    /// <param name="cancellationToken">Cancels the command being run</param>
    /// <returns>The command instance after it has run</returns>
    /// <exception cref="KnownException">Thrown when the arguments for the command are not valid</exception>
    protected async Task<T> ExecuteCommandAsync<T>(
        Action<CommandArgumentValues>? configureArguments = null,
        bool quiet = true,
        CancellationToken cancellationToken = default) where T : Command
    {
        var command = CreateCommand<T>(configureArguments, quiet);

        ThrowOnValidationFailure(command);

        await command.ExecuteAsync(cancellationToken);

        return command;
    }

    private static void ThrowOnValidationFailure(CommandBase command)
    {
        var invalidArguments = command.Validate();

        if (invalidArguments.Count == 0)
        {
            return;
        }

        var names = invalidArguments.Select(x => x.Name).Order();

        throw new KnownException(
            $"Could not run command '{command.ExecutionInfo.CommandName}'. " +
            $"These arguments are not valid or missing: {string.Join(", ", names)}.");
    }

    /// <summary>
    /// Displays the command usage description
    /// </summary>
    protected virtual void DisplayUsage()
    {
        var builder = new StringBuilder();

        DisplayUsage(builder);

        _OutputProvider.WriteLine(builder.ToString());
    }

    private string GetKeyString(IArgument arg)
    {
        if (arg.IsPositionalSource == true)
        {
            if (arg.IsRequired == true)
            {
                return $"{{{arg.Name}:{arg.DataType}}}";
            }
            else
            {
                return $"[{{{arg.Name}:{arg.DataType}}}]";
            }
        }
        else
        {
            if (arg.IsRequired == true)
            {
                return $"/{arg.Name}:{arg.DataType}";
            }
            else
            {
                return $"[/{arg.Name}:{arg.DataType}]";
            }
        }
        
    }

    /// <summary>
    /// Adds the command usage description to the provided string builder
    /// </summary>
    /// <param name="builder">StringBuilder instance</param>
    protected void DisplayUsage(StringBuilder builder)
    {
        builder.AppendLine($"Command name: {ExecutionInfo.CommandName}");

        if (string.IsNullOrWhiteSpace(Description) == false)
        {
            builder.AppendLine(Description);
        }

        // Separate arguments by type
        var commandLineArgs = new List<IArgument>();
        var positionalArgs = new List<IArgument>();
        var configArgs = new List<IArgument>();

        foreach (var key in Arguments.Keys)
        {
            var arg = Arguments[key];

            if (arg.IsFromConfig)
            {
                configArgs.Add(arg);
            }
            else if (arg.IsPositionalSource)
            {
                positionalArgs.Add(arg);
            }
            else
            {
                commandLineArgs.Add(arg);
            }
        }

        int longestNameLength;

        if (Arguments.Count < 1)
        {
            longestNameLength = 0;
        }
        else
        {
            longestNameLength = Arguments.Keys.Max(x =>
                    {
                        return GetKeyString(Arguments[x]).Length;
                    });
        }

        int consoleWidth;

        if (Console.IsOutputRedirected == true)
        {
            consoleWidth = 60;
        }
        else
        {
            consoleWidth = Console.WindowWidth;
        }

        var separator = " - ";
        int argNameColumnWidth = (longestNameLength + separator.Length);

        // Always display USAGE section with command name
        builder.AppendLine("** USAGE **");
        builder.AppendLine(ExecutionInfo.CommandName);

        // Display command line arguments
        foreach (var arg in commandLineArgs)
        {
            DisplayArgumentUsage(builder, arg, longestNameLength, separator, consoleWidth, argNameColumnWidth);
        }

        // Display positional arguments
        if (positionalArgs.Count > 0)
        {
            var argsSortedByPosition = positionalArgs.OrderBy(a => a.Alias);

            foreach (var arg in argsSortedByPosition)
            {
                DisplayArgumentUsage(builder, arg, longestNameLength, separator, consoleWidth, argNameColumnWidth);
            }
        }

        // Display configuration arguments in separate section
        if (configArgs.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("** CONFIGURATION **");
            builder.AppendLine("(set via 'set-configuration', can override via command line)");

            foreach (var arg in configArgs)
            {
                DisplayArgumentUsage(builder, arg, longestNameLength, separator, consoleWidth, argNameColumnWidth);
            }
        }

        DisplayReservedKeywords(builder, consoleWidth);
    }

    /// <summary>
    /// Adds the framework's own reserved names to the usage output. They are not part of any
    /// command's argument list, so without this nothing ever tells anyone they exist.
    /// </summary>
    private static void DisplayReservedKeywords(StringBuilder builder, int consoleWidth)
    {
        var keywords = ReservedKeywords.ForCommands;

        if (keywords.Count == 0)
        {
            return;
        }

        var separator = " - ";
        var longestNameLength = keywords.Max(x => x.Name.Length);
        var nameColumnWidth = longestNameLength + separator.Length;

        builder.AppendLine();
        builder.AppendLine("** ALSO AVAILABLE **");
        builder.AppendLine("(these work on any command)");

        foreach (var keyword in keywords)
        {
            builder.Append(LineWrapUtilities.GetValueWithPadding(keyword.Name, longestNameLength));
            builder.Append(separator);
            builder.AppendWrappedValue(keyword.Description, consoleWidth, nameColumnWidth);
            builder.AppendLine();
        }
    }

    /// <summary>
    /// Displays a single argument's usage information
    /// </summary>
    private void DisplayArgumentUsage(
        StringBuilder builder,
        IArgument arg,
        int longestNameLength,
        string separator,
        int consoleWidth,
        int argNameColumnWidth)
    {
        if (arg.Name == arg.Description || string.IsNullOrEmpty(arg.Description) == true)
        {
            // description has an empty value or the value is the same as the arg name
            builder.AppendWithPadding(GetKeyString(arg), longestNameLength);
            builder.AppendLine();
        }
        else
        {
            // description has an actual value
            var paddedKeyString = LineWrapUtilities.GetValueWithPadding(
                GetKeyString(arg), longestNameLength);

            builder.Append(paddedKeyString);
            builder.Append(separator);
            builder.AppendWrappedValue(arg.Description,
                consoleWidth, argNameColumnWidth);

            builder.AppendLine();
        }

        DisplayArgumentDefaultValue(builder, arg, consoleWidth, argNameColumnWidth);
    }

    /// <summary>
    /// Adds the '(default: value)' line for an argument that has an explicitly configured
    /// default value. The line is indented to align with the description column.
    /// </summary>
    private void DisplayArgumentDefaultValue(
        StringBuilder builder,
        IArgument arg,
        int consoleWidth,
        int argNameColumnWidth)
    {
        if (arg.HasDefaultValue == false)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(arg.DefaultValue) == true)
        {
            // nothing worth showing for an empty default
            return;
        }

        builder.Append(' ', argNameColumnWidth);
        builder.AppendWrappedValue($"(default: {arg.DefaultValue})",
            consoleWidth, argNameColumnWidth);

        builder.AppendLine();
    }

    /// <summary>
    /// Creates and displays the validation summary when there are failed argument validations
    /// </summary>
    /// <param name="invalidArguments">Collection of invalid arguments</param>
    protected virtual void DisplayValidationSummary(List<IArgument> invalidArguments)
    {
        if (invalidArguments.Count == 1)
        {
            _OutputProvider.WriteLine("** INVALID ARGUMENT **");
        }
        else if (invalidArguments.Count > 1)
        {
            _OutputProvider.WriteLine("** INVALID ARGUMENTS **");
        }

        foreach (var item in invalidArguments)
        {
            if (item is UnknownArgument)
            {
                _OutputProvider.WriteLine($"Unknown argument: {item.Name}");
            }
            else
            {
                _OutputProvider.WriteLine($"{item.Name} is not valid or missing");
            }
        }
    }

    /// <summary>
    /// Validate the arguments provided using the required argument definition for the
    /// command.
    /// </summary>
    /// <returns>List of invalid arguments</returns>
    protected virtual List<IArgument> Validate()
    {
        var returnValue = new List<IArgument>();

        SetValuesFromExecutionInfo();

        foreach (var key in Arguments.Keys)
        {
            var temp = Arguments[key];

            if (temp != null)
            {
                var result = temp.Validate();

                if (result == false)
                    returnValue.Add(temp);
            }
        }

        // Only validate unknown arguments if strict validation is enabled
        if (ExecutionInfo.Options.StrictArgumentValidation)
        {
            foreach (var unknownKey in Arguments.UnrecognizedKeys)
            {
                returnValue.Add(new UnknownArgument(unknownKey));
            }
        }

        return returnValue;
    }

    /// <summary>
    /// Reads the arguments from the execution info and
    /// sets the values on to the argument definitions for the command.
    /// For arguments marked with FromConfig(), values are loaded from
    /// configuration with command line taking precedence.
    /// </summary>
    protected virtual void SetValuesFromExecutionInfo()
    {
        if (_HaveValuesBeenSet == false)
        {
            // First, set values from config for FromConfig arguments
            SetValuesFromConfig();

            // Then set values from command line (overrides config values)
            Arguments.SetValues(_Info.Arguments);

            _HaveValuesBeenSet = true;
        }
    }

    /// <summary>
    /// Sets values from configuration for arguments marked with FromConfig().
    /// </summary>
    private void SetValuesFromConfig()
    {
        if (!_Info.HasConfiguration)
        {
            return;
        }

        var config = _Info.Configuration;

        foreach (var key in Arguments.Keys)
        {
            var arg = Arguments[key];

            if (arg.IsFromConfig && !arg.HasValue)
            {
                if (config.HasValue(arg.Name))
                {
                    var configValue = config.GetValue(arg.Name);
                    arg.TrySetValue(configValue);
                }
            }
        }
    }
}
