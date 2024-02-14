using System.Reflection;
using System.Linq;
using System.Text;

namespace Benday.CommandsFramework;

/// <summary>
/// Utility methods for accessing command attribute information on the
/// types in an assembly. 
/// </summary>
public class CommandAttributeUtility
{
    private ICommandProgramOptions _ProgramOptions;

    public CommandAttributeUtility(ICommandProgramOptions options)
    {
        _ProgramOptions = options;
    }

    /// <summary>
    /// Get the list of command names in an assembly
    /// </summary>
    /// <param name="containingAssembly">Assembly to examine</param>
    /// <returns>List of command names for all classes with a CommandAttribute in the assembly</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public List<string> GetAvailableCommandNames(Assembly containingAssembly)
    {
        if (containingAssembly is null)
        {
            throw new ArgumentNullException(nameof(containingAssembly));
        }

        var returnValue = new List<string>();

        var matchingTypes =
            from type in containingAssembly.GetTypes()
            where type.GetCustomAttributes<CommandAttribute>().Any()
            select type;

        foreach (var type in matchingTypes)
        {
            var attr = type.GetCustomAttribute<CommandAttribute>();

            if (attr != null)
            {
                returnValue.Add(attr.Name);
            }
        }

        return returnValue;
    }

    /// <summary>
    /// Get all the command attributes for all the command types in an assembly.
    /// </summary>
    /// <param name="containingAssembly">Assembly to check</param>
    /// <returns>List of command attributes from this assembly.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public List<CommandAttribute> GetAvailableCommandAttributes(Assembly containingAssembly)
    {
        if (containingAssembly is null)
        {
            throw new ArgumentNullException(nameof(containingAssembly));
        }

        var returnValue = new List<CommandAttribute>();

        var matchingTypes =
            from type in containingAssembly.GetTypes()
            where type.GetCustomAttributes<CommandAttribute>().Any()
            select type;

        foreach (var type in matchingTypes)
        {
            var attr = type.GetCustomAttribute<CommandAttribute>();

            if (attr != null)
            {
                returnValue.Add(attr);
            }
        }

        return returnValue;
    }

    /// <summary>
    /// Gets command type from an assembly by command name.
    /// </summary>
    /// <param name="containingAssembly">Assembly containing the commands</param>
    /// <param name="commandName">Command name to find. This is typically args[0] from the command line.</param>
    /// <returns>Instance of System.Type for the matching command or null if not found.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public Type? GetAvailableCommandType(Assembly containingAssembly, string commandName)
    {
        if (containingAssembly is null)
        {
            throw new ArgumentNullException(nameof(containingAssembly));
        }

        var match =
            (from type in containingAssembly.GetTypes()
            where 
                type.IsSubclassOf(typeof(CommandBase)) == true &&
                type.GetCustomAttributes<CommandAttribute>().Any(t=> t.Name == commandName)
            select type).FirstOrDefault();

        return match;
    }

    /// <summary>
    /// Get a command argument for a command by command name.
    /// </summary>
    /// <param name="containingAssembly">Assembly containing the commands</param>
    /// <param name="commandName">Command name to find. This is typically args[0] from the command line.</param>
    /// <returns>Command argument for the command or null if not found.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public CommandAttribute? GetCommandAttributeForCommandName(Assembly containingAssembly, string commandName)
    {
        if (containingAssembly is null)
        {
            throw new ArgumentNullException(nameof(containingAssembly));
        }

        var match =
            (from type in containingAssembly.GetTypes()
             where
                 type.IsSubclassOf(typeof(CommandBase)) == true &&
                 type.GetCustomAttributes<CommandAttribute>().Any(t => t.Name == commandName)
             select type.GetCustomAttribute<CommandAttribute>()).FirstOrDefault();

        return match;
    }

    /// <summary>
    /// Gets a populated instance of a command using arguments from the command line.
    /// This uses args[0] as the command name. The rest of the arguments are used 
    /// as argument values to the command.
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <param name="containingAssembly">Assembly that contains the commands</param>
    /// <returns>Populated command</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="MissingArgumentException"></exception>
    public CommandBase? GetCommand(string[] args, Assembly containingAssembly)
    {
        if (args is null)
        {
            throw new ArgumentNullException(nameof(args));
        }

        if (args.Length == 0)
        {
            throw new ArgumentException(nameof(args));
        }

        if (containingAssembly is null)
        {
            throw new ArgumentNullException(nameof(containingAssembly));
        }

        var execInfo = new ArgumentCollectionFactory().Parse(args);

        if (execInfo is null || string.IsNullOrEmpty(execInfo.CommandName) == true)
        {
            throw new MissingArgumentException("Could not locate a command name.");
        }
        else
        {
            execInfo.Options = _ProgramOptions;
            execInfo.Configuration = new FileBasedConfigurationManager(
                _ProgramOptions.ConfigurationFolderName);

            if (_ProgramOptions.UsesConfiguration == true)
            {
                var thisAssembly = this.GetType().Assembly;

                var defaultCommand = GetCommandInstance(thisAssembly, execInfo, false);

                if (defaultCommand != null)
                {
                    return defaultCommand;
                }
                else
                {
                    return GetCommandInstance(containingAssembly, execInfo);
                }
            }
            else
            {
                return GetCommandInstance(containingAssembly, execInfo);
            }

        }
    }

    private CommandBase? GetCommandInstance(Assembly containingAssembly, CommandExecutionInfo? execInfo, 
        bool throwException = true)
    {
        var commandNames = GetAvailableCommandNames(containingAssembly);

        if (commandNames.Contains(execInfo.CommandName) == false)
        {
            if (throwException == true)
            {
                throw new MissingArgumentException($"Could not locate a command named '{execInfo.CommandName}'.");
            }
            else
            {
                return null;
            }
        }
        else
        {
            var commandType = GetAvailableCommandType(containingAssembly, execInfo.CommandName);

            if (commandType is null)
            {
                throw new MissingArgumentException($"Could not locate a command data type named '{execInfo.CommandName}'.");
            }

            var ctor = commandType.GetConstructor(new Type[] { typeof(CommandExecutionInfo), typeof(ITextOutputProvider) });

            if (ctor is null)
            {
                throw new MissingArgumentException($"Could not locate a constructor on command type named '{execInfo.CommandName}'.");
            }

            var instance = ctor.Invoke(new object[] { execInfo, new ConsoleTextOutputProvider() });

            return instance as CommandBase;
        }
    }

    /// <summary>
    /// Creates a list of command usages for all the commands in an assembly
    /// </summary>
    /// <param name="asm">Assembly to check</param>
    /// <returns>List of command usages</returns>
    public List<CommandInfo> GetAllCommandUsages(Assembly asm)
    {
        var attributes = GetAvailableCommandAttributes(asm);

        var returnValues = new List<CommandInfo>();

        PopulateUsages(asm, attributes, returnValues);

        if (_ProgramOptions.UsesConfiguration == true)
        {
            var thisAssembly = this.GetType().Assembly;

            var defaultAttributes = GetAvailableCommandAttributes(thisAssembly);

            PopulateUsages(thisAssembly, defaultAttributes, returnValues);
        }

        return returnValues;
    }

    private void PopulateUsages(Assembly asm, List<CommandAttribute> attributes, List<CommandInfo> returnValues)
    {
        foreach (var item in attributes)
        {
            var info = new CommandInfo();

            info.Name = item.Name;
            info.Description = item.Description;
            info.IsAsync = item.IsAsync;
            info.Category = item.Category;

            var command = GetCommand(
                new[] { item.Name, ArgumentFrameworkConstants.ArgumentHelpString },
                asm);

            if (command != null)
            {
                var args = command.GetArguments();

                info.Arguments = args;
            }

            returnValues.Add(info);
        }
    }
}
