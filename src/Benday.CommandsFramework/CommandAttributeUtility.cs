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

        var matchingTypes = GetCommandTypes(containingAssembly);

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

        var matchingTypes = GetCommandTypes(containingAssembly);

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
    /// Gets the types in an assembly that are marked with a CommandAttribute, including
    /// the built-in configuration commands when the program uses configuration.
    /// </summary>
    private List<Type> GetCommandTypes(Assembly containingAssembly)
    {
        var matchingTypes =
            (from type in containingAssembly.GetTypes()
             where type.GetCustomAttributes<CommandAttribute>().Any()
             select type).ToList();

        if (_ProgramOptions.UsesConfiguration == true)
        {
            var thisAssembly = this.GetType().Assembly;

            // don't add the built-in commands twice when the caller is already asking
            // about this assembly
            if (thisAssembly != containingAssembly)
            {
                matchingTypes.AddRange(
                    (from type in thisAssembly.GetTypes()
                     where type.GetCustomAttributes<CommandAttribute>().Any()
                     select type).ToList());
            }
        }

        return matchingTypes;
    }

    /// <summary>
    /// Gets every alternate name for the commands in an assembly. This covers the plain
    /// aliases declared by CommandAttribute.Aliases as well as the aliases declared by
    /// CommandAliasAttribute, which also supply argument values.
    /// </summary>
    /// <param name="containingAssembly">Assembly containing the commands</param>
    /// <returns>List of aliases</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public List<CommandAliasInfo> GetCommandAliases(Assembly containingAssembly)
    {
        if (containingAssembly is null)
        {
            throw new ArgumentNullException(nameof(containingAssembly));
        }

        var returnValue = new List<CommandAliasInfo>();

        foreach (var type in GetCommandTypes(containingAssembly))
        {
            var commandAttribute = type.GetCustomAttribute<CommandAttribute>();

            if (commandAttribute is null)
            {
                continue;
            }

            foreach (var alias in commandAttribute.Aliases)
            {
                returnValue.Add(new CommandAliasInfo
                {
                    Alias = alias,
                    CommandName = commandAttribute.Name,
                    Description = commandAttribute.Description
                });
            }

            foreach (var aliasAttribute in type.GetCustomAttributes<CommandAliasAttribute>())
            {
                returnValue.Add(new CommandAliasInfo
                {
                    Alias = aliasAttribute.Name,
                    CommandName = commandAttribute.Name,
                    Description = string.IsNullOrWhiteSpace(aliasAttribute.Description)
                        ? commandAttribute.Description
                        : aliasAttribute.Description,
                    Arguments = aliasAttribute.GetArgumentValues()
                });
            }
        }

        return returnValue;
    }

    /// <summary>
    /// Resolves a command name or command alias to the real command name.
    /// Real command names always take precedence over aliases, so an alias can never
    /// shadow an actual command.
    /// </summary>
    /// <param name="containingAssembly">Assembly containing the commands</param>
    /// <param name="nameOrAlias">Command name or alias. This is typically args[0] from the command line.</param>
    /// <returns>The real command name, or null when nothing matches</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="KnownException">Thrown when the alias is claimed by more than one command</exception>
    public string? ResolveCommandName(Assembly containingAssembly, string nameOrAlias)
    {
        if (containingAssembly is null)
        {
            throw new ArgumentNullException(nameof(containingAssembly));
        }

        if (string.IsNullOrEmpty(nameOrAlias) == true)
        {
            return null;
        }

        var attributes = GetAvailableCommandAttributes(containingAssembly);

        // a real command name always wins over any kind of alias
        var nameMatch = attributes.FirstOrDefault(x => x.Name == nameOrAlias);

        if (nameMatch != null)
        {
            return nameMatch.Name;
        }

        var aliasMatches = GetCommandAliases(containingAssembly)
            .Where(x => x.Alias == nameOrAlias)
            .Select(x => x.CommandName)
            .Distinct()
            .ToList();

        if (aliasMatches.Count > 1)
        {
            throw new KnownException(
                $"The alias '{nameOrAlias}' is ambiguous. It is claimed by these commands: " +
                $"{string.Join(", ", aliasMatches.Order())}.");
        }

        return aliasMatches.FirstOrDefault();
    }

    /// <summary>
    /// Gets the alias that matches a name, if there is one. Use this to find the argument
    /// values that an alias supplies.
    /// </summary>
    /// <param name="containingAssembly">Assembly containing the commands</param>
    /// <param name="alias">Alias to look for</param>
    /// <returns>The matching alias or null when nothing matches</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public CommandAliasInfo? GetCommandAlias(Assembly containingAssembly, string alias)
    {
        if (containingAssembly is null)
        {
            throw new ArgumentNullException(nameof(containingAssembly));
        }

        if (string.IsNullOrEmpty(alias) == true)
        {
            return null;
        }

        // a real command name always wins, so an alias that shadows one never applies
        var attributes = GetAvailableCommandAttributes(containingAssembly);

        if (attributes.Any(x => x.Name == alias) == true)
        {
            return null;
        }

        return GetCommandAliases(containingAssembly).FirstOrDefault(x => x.Alias == alias);
    }

    /// <summary>
    /// Resolves a command name or command alias to the real command name.
    /// Real command names always take precedence over aliases, so an alias can never
    /// shadow an actual command.
    /// </summary>
    /// <param name="attributes">Command attributes to search</param>
    /// <param name="nameOrAlias">Command name or alias. This is typically args[0] from the command line.</param>
    /// <returns>The real command name, or null when nothing matches</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="KnownException">Thrown when the alias is claimed by more than one command</exception>
    public string? ResolveCommandName(List<CommandAttribute> attributes, string nameOrAlias)
    {
        if (attributes is null)
        {
            throw new ArgumentNullException(nameof(attributes));
        }

        if (string.IsNullOrEmpty(nameOrAlias) == true)
        {
            return null;
        }

        // a real command name always wins over an alias
        var nameMatch = attributes.FirstOrDefault(x => x.Name == nameOrAlias);

        if (nameMatch != null)
        {
            return nameMatch.Name;
        }

        var aliasMatches = attributes
            .Where(x => x.Aliases.Contains(nameOrAlias))
            .Select(x => x.Name)
            .Distinct()
            .ToList();

        if (aliasMatches.Count > 1)
        {
            throw new KnownException(
                $"The alias '{nameOrAlias}' is ambiguous. It is claimed by these commands: " +
                $"{string.Join(", ", aliasMatches.Order())}.");
        }

        return aliasMatches.FirstOrDefault();
    }

    /// <summary>
    /// Checks the commands in an assembly for command name and alias problems.
    /// Nothing in the framework calls this automatically -- it is intended to be called
    /// from a unit test so that alias problems are caught at build time rather than
    /// showing up as a command that mysteriously cannot be run.
    /// </summary>
    /// <param name="containingAssembly">Assembly containing the commands</param>
    /// <returns>Human readable descriptions of any problems found. Empty when everything is fine.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public List<string> GetCommandNameProblems(Assembly containingAssembly)
    {
        if (containingAssembly is null)
        {
            throw new ArgumentNullException(nameof(containingAssembly));
        }

        var attributes = GetAvailableCommandAttributes(containingAssembly);
        var aliases = GetCommandAliases(containingAssembly);

        return GetCommandNameProblems(attributes, aliases);
    }

    /// <summary>
    /// Checks a set of commands for command name and alias problems.
    /// Nothing in the framework calls this automatically -- it is intended to be called
    /// from a unit test so that alias problems are caught at build time rather than
    /// showing up as a command that mysteriously cannot be run.
    /// </summary>
    /// <param name="attributes">Command attributes to check</param>
    /// <param name="aliases">Aliases to check. When null, the aliases declared by
    /// CommandAttribute.Aliases are used.</param>
    /// <returns>Human readable descriptions of any problems found. Empty when everything is fine.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public List<string> GetCommandNameProblems(
        List<CommandAttribute> attributes, List<CommandAliasInfo>? aliases = null)
    {
        if (attributes is null)
        {
            throw new ArgumentNullException(nameof(attributes));
        }

        aliases ??= attributes
            .SelectMany(x => x.Aliases.Select(alias => new CommandAliasInfo
            {
                Alias = alias,
                CommandName = x.Name
            }))
            .ToList();

        var problems = new List<string>();

        var reservedNames = new[]
        {
            ArgumentFrameworkConstants.ArgumentHelpString,
            ArgumentFrameworkConstants.ArgumentJson,
            ArgumentFrameworkConstants.ArgumentGui
        };

        var duplicateNames = attributes
            .GroupBy(x => x.Name)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key);

        foreach (var name in duplicateNames)
        {
            problems.Add($"Command name '{name}' is used by more than one command.");
        }

        var commandNames = attributes.Select(x => x.Name).ToHashSet();

        foreach (var alias in aliases)
        {
            if (string.IsNullOrWhiteSpace(alias.Alias) == true)
            {
                problems.Add($"Command '{alias.CommandName}' has an empty alias.");
            }
            else if (commandNames.Contains(alias.Alias) == true)
            {
                problems.Add(
                    $"Alias '{alias.Alias}' on command '{alias.CommandName}' is also the name of a command. " +
                    "The real command name wins, so this alias can never be used.");
            }
            else if (reservedNames.Contains(alias.Alias) == true)
            {
                problems.Add(
                    $"Alias '{alias.Alias}' on command '{alias.CommandName}' is a reserved framework keyword. " +
                    "The keyword wins, so this alias can never be used.");
            }
        }

        var duplicateAliases = aliases
            .Where(x => string.IsNullOrWhiteSpace(x.Alias) == false)
            .GroupBy(x => x.Alias)
            .Where(x => x.Select(y => y.CommandName).Distinct().Count() > 1);

        foreach (var group in duplicateAliases)
        {
            var owners = string.Join(", ", group.Select(x => x.CommandName).Distinct().Order());

            problems.Add($"Alias '{group.Key}' is claimed by more than one command: {owners}.");
        }

        return problems;
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
                 type.GetCustomAttributes<CommandAttribute>().Any(t => t.Name == commandName)
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
            // resolve aliases to the real command name up front so that everything
            // downstream only ever deals with real command names
            var alias = GetCommandAlias(containingAssembly, execInfo.CommandName);

            var resolvedCommandName = ResolveCommandName(containingAssembly, execInfo.CommandName);

            if (resolvedCommandName != null)
            {
                execInfo.CommandName = resolvedCommandName;
            }

            if (alias != null)
            {
                // argument values from an alias are added as though they had been typed
                // on the command line, so anything actually typed wins and the existing
                // command line over config over default order is unchanged
                foreach (var argument in alias.Arguments)
                {
                    execInfo.Arguments.TryAdd(argument.Key, argument.Value);
                }
            }

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

    private CommandBase? GetCommandInstance(
        Assembly containingAssembly,
        CommandExecutionInfo? execInfo,
        bool throwException = true)
    {
        ArgumentNullException.ThrowIfNull(execInfo, nameof(execInfo));

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

            var instance = ctor.Invoke(new object[] { execInfo, _ProgramOptions.OutputProvider });

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
        var aliases = GetCommandAliases(asm);

        foreach (var item in attributes)
        {
            var info = new CommandInfo();

            info.Name = item.Name;
            info.Description = item.Description;
            info.IsAsync = item.IsAsync;
            info.Category = item.Category;
            info.Aliases = item.Aliases;

            // aliases that also supply argument values are reported separately so that
            // tooling can tell a plain rename apart from a preset
            info.CommandAliases = aliases
                .Where(x => x.CommandName == item.Name && x.HasArguments)
                .ToList();

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
