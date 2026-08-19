using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
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
    /// Gets the registry of commands for an assembly, building it if it has not been built
    /// yet and caching it on the program options so the assemblies are only scanned once.
    /// </summary>
    /// <remarks>
    /// Everything in this class goes through here. Before the registry existed, each method
    /// swept assembly.GetTypes() with its own filter, which is how two of them ended up
    /// disagreeing about what counted as a command.
    /// </remarks>
    /// <param name="containingAssembly">Assembly containing the commands</param>
    /// <returns>The registry</returns>
    public CommandRegistry GetRegistry(Assembly containingAssembly)
    {
        if (containingAssembly is null)
        {
            throw new ArgumentNullException(nameof(containingAssembly));
        }

        var cached = _ProgramOptions.CommandRegistry;

        // UsesConfiguration decides whether the built-in commands are registered, and tests
        // flip it on a shared options instance, so a cached registry is only reusable when it
        // was built for the same question
        if (cached is not null &&
            cached.WasBuiltFor(containingAssembly, _ProgramOptions.UsesConfiguration) == true)
        {
            return cached;
        }

        var registry = CommandRegistry.Build(_ProgramOptions, containingAssembly);

        _ProgramOptions.CommandRegistry = registry;

        return registry;
    }
    

    /// <summary>
    /// Get the list of command names in an assembly
    /// </summary>
    /// <param name="containingAssembly">Assembly to examine</param>
    /// <returns>Command names as they are typed, group included for a grouped command</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public List<string> GetAvailableCommandNames(Assembly containingAssembly)
    {
        if (containingAssembly is null)
        {
            throw new ArgumentNullException(nameof(containingAssembly));
        }

        // the path rather than the bare name: a grouped command is typed as 'widget list',
        // and that is also how the registry is keyed, so the two cannot disagree
        return GetRegistry(containingAssembly)
            .Registrations
            .Select(x => x.PathAsString)
            .ToList();
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

        return GetRegistry(containingAssembly)
            .Registrations
            .Select(x => x.Attribute)
            .ToList();
    }

    /// <summary>
    /// The single definition of what counts as a command. A type qualifies when it is marked
    /// with a CommandAttribute and the framework can actually create and run it, which means
    /// it also has to be a concrete subclass of CommandBase.
    /// </summary>
    /// <remarks>
    /// Every place that looks for commands goes through this so that the list of commands
    /// shown to the user cannot disagree with the list of commands that can be instantiated.
    /// When those two disagreed, a CommandAttribute on a class that was not a CommandBase was
    /// listed in the help and then took down the whole --json schema dump.
    /// </remarks>
    /// <param name="type">Type to check</param>
    /// <returns>True when the type is a runnable command</returns>
    public static bool IsCommandType(Type type)
    {
        if (type is null)
        {
            return false;
        }

        return
            type.IsAbstract == false &&
            type.IsSubclassOf(typeof(CommandBase)) == true &&
            type.GetCustomAttributes<CommandAttribute>().Any() == true;
    }

    /// <summary>
    /// Checks every command's arguments for problems that can only be seen once the argument
    /// definitions exist.
    /// </summary>
    /// <remarks>
    /// Separate from CommandRegistry.Problems on purpose: this has to instantiate every
    /// command in the tool to ask it for its arguments, which is exactly the cost the
    /// registry was built to avoid paying on every run. Call it from a unit test.
    ///
    /// What it finds today is an optional positional argument declared before a required
    /// one. Positions are ordinal over the values that were actually supplied, so an omitted
    /// optional one silently shifts every position after it and the command reads the wrong
    /// values without any error at all.
    /// </remarks>
    /// <param name="containingAssembly">Assembly containing the commands</param>
    /// <returns>Human readable descriptions of any problems found</returns>
    public List<string> GetArgumentProblems(Assembly containingAssembly)
    {
        if (containingAssembly is null)
        {
            throw new ArgumentNullException(nameof(containingAssembly));
        }

        var problems = new List<string>();

        foreach (var usage in GetAllCommandUsages(containingAssembly))
        {
            var positionals = usage.Arguments
                .Where(x => x.IsPositionalSource == true)
                .OrderBy(x => x.Alias, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var seenOptional = new List<string>();

            foreach (var argument in positionals)
            {
                if (argument.IsRequired == false)
                {
                    seenOptional.Add(argument.Name);
                }
                else if (seenOptional.Count > 0)
                {
                    var optionalNames = string.Join(
                        ", ", seenOptional.Select(x => "'" + x + "'"));

                    problems.Add(
                        $"Command '{usage.Name}' declares required positional argument " +
                        $"'{argument.Name}' after optional positional {optionalNames}. " +
                        "Positions are counted over the values that are actually supplied, so " +
                        "leaving the optional one out shifts every position after it and the " +
                        "command reads the wrong values without any error.");
                }
            }
        }

        return problems;
    }

    /// <summary>
    /// Gets the types in an assembly that are marked with a CommandAttribute but that the
    /// framework cannot run, so that a unit test can report them instead of leaving the
    /// author wondering why their command never shows up.
    /// </summary>
    /// <param name="containingAssembly">Assembly to examine</param>
    /// <returns>Types with a CommandAttribute that are not runnable commands</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public List<Type> GetUnrunnableCommandTypes(Assembly containingAssembly)
    {
        if (containingAssembly is null)
        {
            throw new ArgumentNullException(nameof(containingAssembly));
        }

        return
            (from type in containingAssembly.GetTypes()
             where
                 type.GetCustomAttributes<CommandAttribute>().Any() == true &&
                 IsCommandType(type) == false
             select type).ToList();
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

        return GetRegistry(containingAssembly)
            .Registrations
            .SelectMany(x => x.Aliases)
            .ToList();
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

        // real command names beat aliases, and an alias claimed by two commands has already
        // been rejected when the registry was built
        return GetRegistry(containingAssembly).Find(nameOrAlias)?.Name;
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
        return GetRegistry(containingAssembly).FindAlias(alias);
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

        // the registry works these out while it builds, so nothing has to sweep the assembly
        // again here
        return GetRegistry(containingAssembly).Problems.ToList();
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

        return GetRegistry(containingAssembly).Find(commandName)?.CommandType;
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

        return GetRegistry(containingAssembly).Find(commandName)?.Attribute;
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

        var registry = GetRegistry(containingAssembly);

        // the command name can be more than one token when the command declares a group, so
        // the registry decides where the name stops and the arguments begin rather than the
        // parser assuming args[0]
        var resolution = registry.Resolve(args);

        if (resolution is null)
        {
            throw new MissingArgumentException(
                $"Could not locate a command named '{args[0]}'.");
        }

        var arguments = new ArgumentCollectionFactory().GetArgsAsDictionary(
            [.. resolution.RemainingTokens], true);

        // argument values from an alias are added as though they had been typed on the
        // command line, so anything actually typed wins and the existing command line over
        // config over default order is unchanged
        foreach (var argument in resolution.PresetArguments)
        {
            arguments.TryAdd(argument.Key, argument.Value);
        }

        // everything downstream deals only in real command names, and what was actually
        // typed survives on the request rather than being overwritten
        var execInfo = new CommandExecutionInfo
        {
            Request = new CommandCallRequest(
                resolution.Registration.PathAsString, arguments, resolution.MatchedAs)
        };

        execInfo.Options = _ProgramOptions;
        execInfo.Configuration = new FileBasedConfigurationManager(
            _ProgramOptions.ConfigurationFolderName);

        // the built-in commands are ordinary registrations, so there is nothing to route --
        // this used to branch on UsesConfiguration and try one assembly and then the other
        return CreateInstance(resolution.Registration, execInfo);
    }

    /// <summary>
    /// Creates an instance of a registered command.
    /// </summary>
    /// <param name="registration">The command to create</param>
    /// <param name="execInfo">Execution information to hand it</param>
    /// <returns>The command instance</returns>
    /// <exception cref="MissingArgumentException">Thrown when the command type does not have
    /// the constructor the framework activates commands through.</exception>
    public CommandBase CreateInstance(
        CommandRegistration registration, CommandExecutionInfo execInfo)
    {
        ArgumentNullException.ThrowIfNull(registration, nameof(registration));
        ArgumentNullException.ThrowIfNull(execInfo, nameof(execInfo));

        // ActivatorUtilities rather than a hardcoded constructor lookup, so a command can
        // declare the services it needs as constructor parameters. The old two-argument
        // GetConstructor() meant any new framework parameter broke every downstream command
        // -- at run time, not compile time, because the lookup simply returned null.
        var scope = CommandFrameworkUtilities
            .GetServiceProvider(_ProgramOptions)
            .CreateScope();

        try
        {
            var instance = ActivatorUtilities.CreateInstance(
                scope.ServiceProvider,
                registration.CommandType,
                execInfo,
                _ProgramOptions.OutputProvider);

            var command = (CommandBase)instance;

            // the command owns the scope and releases it when it is disposed. Nothing used
            // to dispose a command, so the scope was never released.
            command.SetServiceScope(scope, true);

            return command;
        }
        catch
        {
            scope.Dispose();

            throw;
        }
    }

    /// <summary>
    /// Creates a list of command usages for all the commands in an assembly
    /// </summary>
    /// <param name="asm">Assembly to check</param>
    /// <returns>List of command usages</returns>
    public List<CommandInfo> GetAllCommandUsages(Assembly asm)
    {
        // one pass over the registry -- the built-in commands are registered like any other,
        // so there is no second pass over the framework assembly
        return GetRegistry(asm)
            .Registrations
            .Select(x => GetCommandUsage(x, asm))
            .ToList();
    }

    /// <summary>
    /// Creates a command for the purpose of reading its argument definitions, filling in any
    /// constructor dependency that cannot be resolved with null rather than failing.
    /// </summary>
    /// <remarks>
    /// The schema path instantiates every command in the tool, so it cannot be as strict as
    /// the run path: one command with an unregistered dependency would otherwise take down
    /// the whole --json dump, and cmdui with it. This is safe because GetArguments() cannot
    /// depend on injected state anyway -- CommandBase's constructor calls it, which runs
    /// before any derived field is assigned.
    /// </remarks>
    private CommandBase CreateInstanceForSchema(
        CommandRegistration registration, CommandExecutionInfo execInfo)
    {
        var provider = CommandFrameworkUtilities.GetServiceProvider(_ProgramOptions);

        var constructor = registration.CommandType
            .GetConstructors()
            .OrderByDescending(x => x.GetParameters().Length)
            .FirstOrDefault();

        if (constructor is null)
        {
            throw new MissingArgumentException(
                $"Could not locate a constructor on command type named '{registration.Name}'.");
        }

        var arguments = new List<object?>();

        foreach (var parameter in constructor.GetParameters())
        {
            if (parameter.ParameterType.IsAssignableFrom(typeof(CommandExecutionInfo)) == true)
            {
                arguments.Add(execInfo);
            }
            else if (parameter.ParameterType.IsInstanceOfType(_ProgramOptions.OutputProvider) == true)
            {
                arguments.Add(_ProgramOptions.OutputProvider);
            }
            else
            {
                // GetService rather than GetRequiredService: an unresolved dependency becomes
                // null instead of an exception
                arguments.Add(
                    provider.GetService(parameter.ParameterType) ??
                    GetDefaultValue(parameter.ParameterType));
            }
        }

        return (CommandBase)constructor.Invoke([.. arguments]);
    }

    private static object? GetDefaultValue(Type type)
    {
        return type.IsValueType == true ? Activator.CreateInstance(type) : null;
    }

    private CommandInfo GetCommandUsage(CommandRegistration registration, Assembly asm)
    {
        var info = new CommandInfo();

        info.Name = registration.Name;
        info.Description = registration.Description;
        info.Category = registration.Category;
        info.Group = registration.Group;
        info.Aliases = registration.Attribute.Aliases;

        // aliases that also supply argument values are reported separately so that tooling
        // can tell a plain rename apart from a preset
        info.CommandAliases = registration.Aliases
            .Where(x => x.HasArguments)
            .ToList();

        var execInfo = new CommandExecutionInfo
        {
            Request = new CommandCallRequest(registration.Name),
            Options = _ProgramOptions,
            Configuration = new FileBasedConfigurationManager(
                _ProgramOptions.ConfigurationFolderName)
        };

        using var command = CreateInstanceForSchema(registration, execInfo);

        var arguments = command.GetArguments();

        info.Arguments = arguments;
        info.Rules = [.. arguments.Rules.Select(ArgumentRuleInfo.FromRule)];

        return info;
    }
}
