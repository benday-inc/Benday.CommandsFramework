using System.Reflection;

namespace Benday.CommandsFramework;

/// <summary>
/// The set of commands a program can run, built once from a set of assemblies.
/// </summary>
/// <remarks>
/// Before this existed, seven separate methods each swept assembly.GetTypes() with their own
/// filter, and three places branched on UsesConfiguration to decide whether to look in the
/// framework assembly as well as the tool's. That is why two of those filters could disagree
/// about what counted as a command, and why the built-in configuration commands needed
/// special routing everywhere. Here the built-ins are ordinary registrations and there is
/// one lookup.
///
/// The registry is keyed with ArgumentCollection.ArgumentNameComparer, so command names are
/// matched without regard to case -- the same rule argument names have followed since v4.18.
/// </remarks>
public sealed class CommandRegistry
{
    private readonly Dictionary<string, CommandRegistration> _ByPath;
    private readonly Dictionary<string, CommandAliasInfo> _AliasesByName;
    private readonly Dictionary<string, CommandRegistration> _RegistrationsByAlias;

    private CommandRegistry(
        IReadOnlyList<CommandRegistration> registrations,
        IReadOnlyList<string> problems,
        Assembly? primaryAssembly,
        bool includesBuiltIns,
        IReadOnlyList<Type> serviceRegistrarTypes)
    {
        Registrations = registrations;
        Problems = problems;
        PrimaryAssembly = primaryAssembly;
        IncludesBuiltIns = includesBuiltIns;
        ServiceRegistrarTypes = serviceRegistrarTypes;

        _ByPath = new Dictionary<string, CommandRegistration>(
            ArgumentCollection.ArgumentNameComparer);

        _AliasesByName = new Dictionary<string, CommandAliasInfo>(
            ArgumentCollection.ArgumentNameComparer);

        _RegistrationsByAlias = new Dictionary<string, CommandRegistration>(
            ArgumentCollection.ArgumentNameComparer);

        foreach (var registration in registrations)
        {
            _ByPath.TryAdd(registration.PathAsString, registration);
        }

        foreach (var registration in registrations)
        {
            foreach (var alias in registration.Aliases)
            {
                if (string.IsNullOrWhiteSpace(alias.Alias) == true)
                {
                    continue;
                }

                // a real command name always beats an alias, and an alias claimed by two
                // commands is reported as a problem rather than silently going to whichever
                // was registered first
                if (_ByPath.ContainsKey(alias.Alias) == true)
                {
                    continue;
                }

                if (_RegistrationsByAlias.TryAdd(alias.Alias, registration) == true)
                {
                    _AliasesByName[alias.Alias] = alias;
                }
            }
        }
    }

    /// <summary>
    /// Every command that can be run, in the order they were discovered.
    /// </summary>
    public IReadOnlyList<CommandRegistration> Registrations { get; }

    /// <summary>
    /// Problems found while building the registry: duplicate command names, aliases that can
    /// never be used, classes marked with CommandAttribute that the framework cannot run.
    /// </summary>
    /// <remarks>
    /// Anything that makes resolution genuinely ambiguous throws while building. Everything
    /// else lands here, because a tool with one unusable alias should still run its other 63
    /// commands. Assert this is empty from a unit test.
    /// </remarks>
    public IReadOnlyList<string> Problems { get; }

    /// <summary>
    /// The tool's own assembly, when the registry was built by the overload that takes one.
    /// Used to tell whether a cached registry answers the question being asked.
    /// </summary>
    public Assembly? PrimaryAssembly { get; }

    /// <summary>
    /// Whether the framework's built-in commands are registered.
    /// </summary>
    public bool IncludesBuiltIns { get; }

    /// <summary>
    /// Types implementing IServiceRegistrar that were found while scanning. CommandsApp
    /// invokes these before it builds the service provider, so an assembly of commands can
    /// declare its own dependencies without Program.cs enumerating them.
    /// </summary>
    public IReadOnlyList<Type> ServiceRegistrarTypes { get; }

    /// <summary>
    /// True when this registry was built for the given assembly and configuration setting,
    /// so a cached copy can be reused instead of scanning again.
    /// </summary>
    /// <param name="commandsAssembly">Assembly containing the tool's commands</param>
    /// <param name="usesConfiguration">Whether the built-in commands should be registered</param>
    public bool WasBuiltFor(Assembly commandsAssembly, bool usesConfiguration)
    {
        var frameworkAssembly = typeof(CommandRegistry).Assembly;

        var expectedBuiltIns = usesConfiguration == true && frameworkAssembly != commandsAssembly;

        return PrimaryAssembly == commandsAssembly && IncludesBuiltIns == expectedBuiltIns;
    }

    /// <summary>
    /// Builds a registry from a tool's own assembly, adding the framework's built-in commands
    /// when the program uses configuration.
    /// </summary>
    /// <param name="options">Program options</param>
    /// <param name="commandsAssembly">Assembly containing the tool's commands</param>
    /// <returns>The registry</returns>
    public static CommandRegistry Build(
        ICommandProgramOptions options, Assembly commandsAssembly)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (commandsAssembly is null)
        {
            throw new ArgumentNullException(nameof(commandsAssembly));
        }

        var frameworkAssembly = typeof(CommandRegistry).Assembly;

        var assemblies = new List<Assembly> { commandsAssembly };

        // the built-in configuration commands are ordinary registrations -- the only thing
        // special about them is that they are only registered when the program wants them
        if (options.UsesConfiguration == true && frameworkAssembly != commandsAssembly)
        {
            assemblies.Add(frameworkAssembly);
        }

        return Build(assemblies, frameworkAssembly, commandsAssembly);
    }

    /// <summary>
    /// Builds a registry from an explicit set of assemblies. Nothing is added implicitly.
    /// </summary>
    /// <param name="assemblies">Assemblies to scan, in priority order</param>
    /// <param name="builtInAssembly">Assembly whose commands should be marked as built in,
    /// if any</param>
    /// <returns>The registry</returns>
    /// <exception cref="KnownException">Thrown when two commands claim the same name, which
    /// makes resolution ambiguous.</exception>
    public static CommandRegistry Build(
        IEnumerable<Assembly> assemblies,
        Assembly? builtInAssembly = null,
        Assembly? primaryAssembly = null)
    {
        if (assemblies is null)
        {
            throw new ArgumentNullException(nameof(assemblies));
        }

        var seenAssemblies = new HashSet<Assembly>();
        var types = new List<Type>();

        foreach (var assembly in assemblies)
        {
            if (assembly is null || seenAssemblies.Add(assembly) == false)
            {
                continue;
            }

            types.AddRange(assembly.GetTypes());
        }

        return BuildFromTypes(types, builtInAssembly, primaryAssembly);
    }

    /// <summary>
    /// Builds a registry from an explicit set of types rather than by scanning assemblies.
    /// </summary>
    /// <param name="types">Types to consider. Anything without a CommandAttribute is
    /// ignored.</param>
    /// <param name="builtInAssembly">Assembly whose commands should be marked as built in,
    /// if any</param>
    /// <param name="primaryAssembly">The tool's own assembly, for cache identity</param>
    /// <returns>The registry</returns>
    /// <exception cref="KnownException">Thrown when two commands claim the same name or the
    /// same alias, which makes resolution ambiguous.</exception>
    public static CommandRegistry BuildFromTypes(
        IEnumerable<Type> types,
        Assembly? builtInAssembly = null,
        Assembly? primaryAssembly = null)
    {
        if (types is null)
        {
            throw new ArgumentNullException(nameof(types));
        }

        var registrations = new List<CommandRegistration>();
        var problems = new List<string>();
        var registrarTypes = new List<Type>();

        foreach (var type in types)
        {
            if (IsServiceRegistrarType(type) == true)
            {
                registrarTypes.Add(type);
            }

            if (type.GetCustomAttributes<CommandAttribute>().Any() == false)
            {
                continue;
            }

            if (CommandAttributeUtility.IsCommandType(type) == false)
            {
                var name = type.GetCustomAttribute<CommandAttribute>()?.Name;

                problems.Add(
                    $"Type '{type.FullName}' has a CommandAttribute for command " +
                    $"'{name}' but is not a concrete subclass of CommandBase, " +
                    "so it is skipped and the command cannot be run.");

                continue;
            }

            var attribute = type.GetCustomAttribute<CommandAttribute>()!;

            registrations.Add(new CommandRegistration(
                type,
                attribute,
                type.Assembly,
                builtInAssembly is not null && type.Assembly == builtInAssembly,
                GetAliases(type, attribute)));
        }

        ThrowOnAmbiguity(registrations);

        problems.AddRange(GetProblems(registrations));

        var includesBuiltIns =
            builtInAssembly is not null &&
            registrations.Any(x => x.SourceAssembly == builtInAssembly);

        return new CommandRegistry(
            registrations, problems, primaryAssembly, includesBuiltIns, registrarTypes);
    }

    /// <summary>
    /// True when a type can be used to register services at startup.
    /// </summary>
    private static bool IsServiceRegistrarType(Type type)
    {
        return
            type.IsAbstract == false &&
            type.IsInterface == false &&
            typeof(IServiceRegistrar).IsAssignableFrom(type) == true &&
            type.GetConstructor(Type.EmptyTypes) is not null;
    }

    /// <summary>
    /// Throws for the problems that make resolution genuinely ambiguous -- two commands
    /// claiming the same name, or two commands claiming the same alias. There is no
    /// defensible way to pick a winner, and picking one silently is how a command ends up
    /// mysteriously running the wrong code.
    /// </summary>
    private static void ThrowOnAmbiguity(IReadOnlyList<CommandRegistration> registrations)
    {
        var duplicatePaths = registrations
            .GroupBy(x => x.PathAsString, ArgumentCollection.ArgumentNameComparer)
            .Where(x => x.Count() > 1)
            .ToList();

        foreach (var group in duplicatePaths)
        {
            throw new KnownException(
                $"The command name '{group.Key}' is claimed by more than one command: " +
                $"{string.Join(", ", group.Select(x => x.CommandType.FullName).Order())}.");
        }

        var duplicateAliases = registrations
            .SelectMany(x => x.Aliases.Select(alias => new { Registration = x, Alias = alias }))
            .Where(x => string.IsNullOrWhiteSpace(x.Alias.Alias) == false)
            .GroupBy(x => x.Alias.Alias, ArgumentCollection.ArgumentNameComparer)
            .Where(x => x.Select(y => y.Registration.Name).Distinct().Count() > 1)
            .ToList();

        foreach (var group in duplicateAliases)
        {
            var owners = string.Join(", ", group.Select(x => x.Registration.Name).Distinct().Order());

            throw new KnownException(
                $"The alias '{group.Key}' is ambiguous. It is claimed by these commands: {owners}.");
        }
    }

    private static IReadOnlyList<CommandAliasInfo> GetAliases(
        Type type, CommandAttribute attribute)
    {
        var returnValues = new List<CommandAliasInfo>();

        foreach (var alias in attribute.Aliases)
        {
            returnValues.Add(new CommandAliasInfo
            {
                Alias = alias,
                CommandName = attribute.Name,
                Description = attribute.Description
            });
        }

        foreach (var aliasAttribute in type.GetCustomAttributes<CommandAliasAttribute>())
        {
            returnValues.Add(new CommandAliasInfo
            {
                Alias = aliasAttribute.Name,
                CommandName = attribute.Name,
                Description = string.IsNullOrWhiteSpace(aliasAttribute.Description)
                    ? attribute.Description
                    : aliasAttribute.Description,
                Arguments = aliasAttribute.GetArgumentValues()
            });
        }

        return returnValues;
    }

    /// <summary>
    /// Checks a set of registrations for the problems that used to only be found by running
    /// the tool and noticing that a command could not be reached.
    /// </summary>
    private static List<string> GetProblems(IReadOnlyList<CommandRegistration> registrations)
    {
        var problems = new List<string>();

        var paths = registrations
            .Select(x => x.PathAsString)
            .ToHashSet(ArgumentCollection.ArgumentNameComparer);

        var reserved = ReservedKeywords.AllNames
            .ToHashSet(ArgumentCollection.ArgumentNameComparer);

        foreach (var registration in registrations)
        {
            if (reserved.Contains(registration.PathAsString) == true)
            {
                problems.Add(
                    $"Command name '{registration.PathAsString}' is a reserved framework " +
                    "keyword. The keyword wins, so the command can never be run.");
            }

            foreach (var alias in registration.Aliases)
            {
                if (string.IsNullOrWhiteSpace(alias.Alias) == true)
                {
                    problems.Add($"Command '{registration.Name}' has an empty alias.");
                }
                else if (paths.Contains(alias.Alias) == true)
                {
                    problems.Add(
                        $"Alias '{alias.Alias}' on command '{registration.Name}' is also the " +
                        "name of a command. The real command name wins, so this alias can " +
                        "never be used.");
                }
                else if (reserved.Contains(alias.Alias) == true)
                {
                    problems.Add(
                        $"Alias '{alias.Alias}' on command '{registration.Name}' is a " +
                        "reserved framework keyword. The keyword wins, so this alias can " +
                        "never be used.");
                }
            }
        }

        return problems;
    }

    /// <summary>
    /// Finds a command by its path or by an alias. Real command names always win.
    /// </summary>
    /// <param name="nameOrAlias">Name or alias, matched without regard to case</param>
    /// <returns>The registration, or null when nothing matches</returns>
    public CommandRegistration? Find(string nameOrAlias)
    {
        if (string.IsNullOrWhiteSpace(nameOrAlias) == true)
        {
            return null;
        }

        if (_ByPath.TryGetValue(nameOrAlias, out var byPath) == true)
        {
            return byPath;
        }

        if (_RegistrationsByAlias.TryGetValue(nameOrAlias, out var byAlias) == true)
        {
            return byAlias;
        }

        return null;
    }

    /// <summary>
    /// Finds the alias that matches a name, if there is one. A name that is also a real
    /// command name is not an alias, because the real command name wins.
    /// </summary>
    /// <param name="alias">Name to look for</param>
    /// <returns>The alias, or null when nothing matches</returns>
    public CommandAliasInfo? FindAlias(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias) == true || _ByPath.ContainsKey(alias) == true)
        {
            return null;
        }

        return _AliasesByName.TryGetValue(alias, out var match) ? match : null;
    }

    /// <summary>
    /// Matches command line tokens against the registry.
    /// </summary>
    /// <remarks>
    /// Matching is greedy longest first, so a two segment command name is preferred over a
    /// one segment name that happens to match the first token. A real command name is always
    /// tried before any alias, at every length.
    /// </remarks>
    /// <param name="tokens">Command line tokens, starting with the command name</param>
    /// <returns>The resolution, or null when no command matches</returns>
    public CommandResolution? Resolve(IReadOnlyList<string> tokens)
    {
        if (tokens is null || tokens.Count == 0)
        {
            return null;
        }

        var longestPath = _ByPath.Count == 0 ? 1 : Registrations.Max(x => x.Path.Count);

        for (var length = Math.Min(longestPath, tokens.Count); length >= 1; length--)
        {
            var candidate = string.Join(" ", tokens.Take(length));

            if (_ByPath.TryGetValue(candidate, out var registration) == true)
            {
                return new CommandResolution(
                    registration,
                    tokens.Skip(length).ToList(),
                    new Dictionary<string, string>(ArgumentCollection.ArgumentNameComparer),
                    candidate);
            }

            if (_RegistrationsByAlias.TryGetValue(candidate, out var byAlias) == true)
            {
                var alias = _AliasesByName[candidate];

                return new CommandResolution(
                    byAlias,
                    tokens.Skip(length).ToList(),
                    new Dictionary<string, string>(
                        alias.Arguments, ArgumentCollection.ArgumentNameComparer),
                    candidate);
            }
        }

        return null;
    }
}
