using System.Diagnostics;
using System.Reflection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benday.CommandsFramework;

/// <summary>
/// Builder for creating and running a CommandsFramework application with simplified DI setup.
/// </summary>
public class CommandsApp
{
    private readonly string[] _args;
    private readonly Assembly _commandsAssembly;
    private readonly IServiceCollection _services;
    private readonly DefaultProgramOptions _options;
    private IConfigurationBuilder? _configBuilder;
    private IConfiguration? _configuration;

    private CommandsApp(string[] args, Assembly commandsAssembly)
    {
        _args = args;
        _commandsAssembly = commandsAssembly;
        _services = new ServiceCollection();
        _options = new DefaultProgramOptions();
    }

    /// <summary>
    /// Creates a new CommandsApp builder. The assembly containing the command type T
    /// will be used to discover available commands.
    /// </summary>
    /// <typeparam name="TCommand">Any command type from the assembly containing your commands</typeparam>
    /// <param name="args">Command line arguments</param>
    /// <returns>A CommandsApp builder instance</returns>
    public static CommandsApp Create<TCommand>(string[] args) where TCommand : class
    {
        return new CommandsApp(args, typeof(TCommand).Assembly);
    }

    /// <summary>
    /// Creates a new CommandsApp builder using the specified assembly for command discovery.
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <param name="commandsAssembly">Assembly containing command implementations</param>
    /// <returns>A CommandsApp builder instance</returns>
    public static CommandsApp Create(string[] args, Assembly commandsAssembly)
    {
        return new CommandsApp(args, commandsAssembly);
    }

    /// <summary>
    /// Creates a new CommandsApp builder that discovers commands in the entry assembly --
    /// the assembly holding Main(). Use one of the other Create overloads when the commands
    /// live in a different assembly than the executable.
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>A CommandsApp builder instance</returns>
    /// <exception cref="InvalidOperationException">Thrown when there is no entry assembly,
    /// which happens when the process was not started from managed code.</exception>
    public static CommandsApp Create(string[] args)
    {
        var entryAssembly = Assembly.GetEntryAssembly() ??
            throw new InvalidOperationException(
                "There is no entry assembly, so the assembly containing the commands cannot be " +
                "guessed. Use Create<TCommand>(args) or Create(args, commandsAssembly) instead.");

        return new CommandsApp(args, entryAssembly);
    }

    /// <summary>
    /// Creates, configures from assembly metadata, and runs an application in one call.
    /// Commands are discovered in the entry assembly, and the application name, version and
    /// website come from that assembly's metadata.
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>The exit code</returns>
    /// <exception cref="InvalidOperationException">Thrown when there is no entry assembly.</exception>
    public static Task<int> RunAsync(string[] args)
    {
        return Create(args).WithAppInfoFromAssembly().RunAsync();
    }

    /// <summary>
    /// Creates, configures from assembly metadata, and runs an application in one call.
    /// Commands are discovered in the assembly containing TCommand; the application name,
    /// version and website still come from the entry assembly when there is one.
    /// </summary>
    /// <typeparam name="TCommand">Any type from the assembly containing your commands</typeparam>
    /// <param name="args">Command line arguments</param>
    /// <returns>The exit code</returns>
    public static Task<int> RunAsync<TCommand>(string[] args) where TCommand : class
    {
        return Create<TCommand>(args).WithAppInfoFromAssembly().RunAsync();
    }

    /// <summary>
    /// Sets the application name, version and website from assembly metadata, leaving any
    /// value that has already been set alone. This is what the one line
    /// CommandsApp.RunAsync(args) bootstrap uses.
    /// </summary>
    /// <remarks>
    /// The values come from the entry assembly when there is one, otherwise from the
    /// assembly containing the commands. Name comes from AssemblyTitle, then AssemblyProduct,
    /// then the assembly's simple name. Version comes from AssemblyInformationalVersion with
    /// any source revision suffix trimmed, then the file version. Website comes from an
    /// AssemblyMetadata entry named PackageProjectUrl, RepositoryUrl or Website -- none of
    /// which the SDK emits by default, so it usually stays empty and simply is not displayed.
    /// </remarks>
    public CommandsApp WithAppInfoFromAssembly()
    {
        var assembly = Assembly.GetEntryAssembly() ?? _commandsAssembly;

        if (string.IsNullOrWhiteSpace(_options.ApplicationName) == true)
        {
            _options.ApplicationName = GetApplicationNameFromAssembly(assembly);
        }

        if (string.IsNullOrWhiteSpace(_options.Version) == true)
        {
            _options.Version = GetVersionFromAssembly(assembly);
        }

        if (string.IsNullOrWhiteSpace(_options.Website) == true)
        {
            _options.Website = GetWebsiteFromAssembly(assembly);
        }

        return this;
    }

    private static string GetApplicationNameFromAssembly(Assembly assembly)
    {
        var title = assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;

        if (string.IsNullOrWhiteSpace(title) == false)
        {
            return title;
        }

        var product = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product;

        if (string.IsNullOrWhiteSpace(product) == false)
        {
            return product;
        }

        return assembly.GetName().Name ?? string.Empty;
    }

    private static string GetVersionFromAssembly(Assembly assembly)
    {
        var informational =
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (string.IsNullOrWhiteSpace(informational) == false)
        {
            // the SDK appends "+<commit sha>" to the informational version, which is noise
            // in a usage header
            var plusIndex = informational.IndexOf('+');

            if (plusIndex > 0)
            {
                informational = informational.Substring(0, plusIndex);
            }

            return $"v{informational}";
        }

        if (string.IsNullOrWhiteSpace(assembly.Location) == false)
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

            if (string.IsNullOrWhiteSpace(versionInfo.FileVersion) == false)
            {
                return $"v{versionInfo.FileVersion}";
            }
        }

        var assemblyVersion = assembly.GetName().Version;

        return assemblyVersion is null ? string.Empty : $"v{assemblyVersion}";
    }

    private static string GetWebsiteFromAssembly(Assembly assembly)
    {
        var metadata = assembly.GetCustomAttributes<AssemblyMetadataAttribute>();

        var keys = new[] { "PackageProjectUrl", "RepositoryUrl", "Website" };

        foreach (var key in keys)
        {
            var match = metadata.FirstOrDefault(
                x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(match?.Value) == false)
            {
                return match.Value;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Sets the application name and website.
    /// </summary>
    public CommandsApp WithAppInfo(string applicationName, string website)
    {
        _options.ApplicationName = applicationName;
        _options.Website = website;
        return this;
    }

    /// <summary>
    /// Sets the application name, version, and website.
    /// </summary>
    public CommandsApp WithAppInfo(string applicationName, string version, string website)
    {
        _options.ApplicationName = applicationName;
        _options.Version = version;
        _options.Website = website;
        return this;
    }

    /// <summary>
    /// Sets the application version string.
    /// </summary>
    public CommandsApp WithVersion(string version)
    {
        _options.Version = version;
        return this;
    }

    /// <summary>
    /// Automatically sets the version from the entry assembly's file version.
    /// </summary>
    public CommandsApp WithVersionFromAssembly()
    {
        var assembly = Assembly.GetEntryAssembly() ?? _commandsAssembly;
        var versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
        _options.Version = $"v{versionInfo.FileVersion}";
        return this;
    }

    /// <summary>
    /// Loads configuration from appsettings.json in the assembly's directory.
    /// Also loads environment variables.
    /// </summary>
    public CommandsApp WithAppSettings(bool optional = false)
    {
        var assembly = Assembly.GetEntryAssembly() ?? _commandsAssembly;
        var baseDirectory = Path.GetDirectoryName(assembly.Location)
            ?? throw new InvalidOperationException("Could not determine base directory.");

        _configBuilder = new ConfigurationBuilder()
            .SetBasePath(baseDirectory)
            .AddJsonFile("appsettings.json", optional: optional, reloadOnChange: true)
            .AddEnvironmentVariables();

        return this;
    }

    /// <summary>
    /// Loads configuration from a custom JSON file in the assembly's directory.
    /// </summary>
    public CommandsApp WithConfigFile(string filename, bool optional = false)
    {
        var assembly = Assembly.GetEntryAssembly() ?? _commandsAssembly;
        var baseDirectory = Path.GetDirectoryName(assembly.Location)
            ?? throw new InvalidOperationException("Could not determine base directory.");

        _configBuilder ??= new ConfigurationBuilder().SetBasePath(baseDirectory);
        _configBuilder.AddJsonFile(filename, optional: optional, reloadOnChange: true);

        return this;
    }

    /// <summary>
    /// Adds environment variables to the configuration.
    /// </summary>
    public CommandsApp WithEnvironmentVariables()
    {
        var assembly = Assembly.GetEntryAssembly() ?? _commandsAssembly;
        var baseDirectory = Path.GetDirectoryName(assembly.Location)
            ?? throw new InvalidOperationException("Could not determine base directory.");

        _configBuilder ??= new ConfigurationBuilder().SetBasePath(baseDirectory);
        _configBuilder.AddEnvironmentVariables();

        return this;
    }

    /// <summary>
    /// Configures the configuration builder directly, allowing you to add
    /// custom configuration sources such as in-memory collections,
    /// additional JSON files, or any other IConfigurationSource.
    /// Call WithAppSettings() or WithConfigFile() before this method to
    /// initialize the configuration builder, or this method will create one.
    /// </summary>
    public CommandsApp ConfigureConfiguration(Action<IConfigurationBuilder> configure)
    {
        if (_configBuilder == null)
        {
            var assembly = Assembly.GetEntryAssembly() ?? _commandsAssembly;
            var baseDirectory = Path.GetDirectoryName(assembly.Location)
                ?? throw new InvalidOperationException("Could not determine base directory.");

            _configBuilder = new ConfigurationBuilder().SetBasePath(baseDirectory);
        }

        configure(_configBuilder);
        return this;
    }

    /// <summary>
    /// Configures the service collection for dependency injection.
    /// </summary>
    public CommandsApp ConfigureServices(Action<IServiceCollection> configure)
    {
        configure(_services);
        return this;
    }

    /// <summary>
    /// Configures the service collection with access to configuration.
    /// Call WithAppSettings() or WithConfigFile() before this method.
    /// </summary>
    public CommandsApp ConfigureServices(Action<IServiceCollection, IConfiguration> configure)
    {
        BuildConfiguration();

        if (_configuration == null)
        {
            throw new InvalidOperationException(
                "Configuration not available. Call WithAppSettings() or WithConfigFile() before ConfigureServices with IConfiguration.");
        }

        configure(_services, _configuration);
        return this;
    }

    /// <summary>
    /// Configures the program options directly.
    /// </summary>
    public CommandsApp ConfigureOptions(Action<DefaultProgramOptions> configure)
    {
        configure(_options);
        return this;
    }

    /// <summary>
    /// Sets whether the application uses the built-in configuration storage.
    /// </summary>
    public CommandsApp UsesConfiguration(bool usesConfiguration)
    {
        _options.UsesConfiguration = usesConfiguration;
        return this;
    }

    /// <summary>
    /// Configures how usage information is displayed.
    /// </summary>
    public CommandsApp ConfigureUsageDisplay(Action<DisplayUsageOptions> configure)
    {
        configure(_options.DisplayUsageOptions);
        return this;
    }

    private void BuildConfiguration()
    {
        if (_configuration == null && _configBuilder != null)
        {
            _configuration = _configBuilder.Build();
        }
    }

    /// <summary>
    /// Runs every IServiceRegistrar found in the assemblies that hold the commands, so an
    /// assembly can declare its own dependencies without Program.cs enumerating them.
    /// </summary>
    /// <remarks>
    /// This has to happen before the provider is built. Microsoft.Extensions.DependencyInjection
    /// seals its registrations at BuildServiceProvider(), and the provider is cached so that
    /// singletons really are singletons -- so a registration hook that ran any later would
    /// compile, run, and silently do nothing.
    /// </remarks>
    private void RunServiceRegistrars()
    {
        var registry = CommandRegistry.Build(_options, _commandsAssembly);

        _options.CommandRegistry = registry;

        foreach (var registrarType in registry.ServiceRegistrarTypes)
        {
            if (Activator.CreateInstance(registrarType) is IServiceRegistrar registrar)
            {
                registrar.Register(_services);
            }
        }
    }

    private void RegisterCoreServices()
    {
        // Register ITextOutputProvider if not already registered
        var outputProviderRegistered = _services.Any(s => s.ServiceType == typeof(ITextOutputProvider));
        if (!outputProviderRegistered)
        {
            _services.AddSingleton<ITextOutputProvider>(_options.OutputProvider);
        }

        // Register IConfiguration if we have one and it's not already registered
        BuildConfiguration();
        if (_configuration != null)
        {
            var configRegistered = _services.Any(s => s.ServiceType == typeof(IConfiguration));
            if (!configRegistered)
            {
                _services.AddSingleton<IConfiguration>(_configuration);
            }
        }
    }

    /// <summary>
    /// Builds and runs the application, and sets Environment.ExitCode from the result.
    /// </summary>
    /// <remarks>
    /// This is the console entry point, so this is the one place in the framework that
    /// assigns the process exit code. Everything below here returns a result instead, which
    /// is what lets the same commands run in a host that outlives any one of them.
    /// </remarks>
    /// <returns>The exit code</returns>
    public int Run()
    {
        return RunAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Builds and runs the application asynchronously, and sets Environment.ExitCode from
    /// the result.
    /// </summary>
    /// <param name="cancellationToken">Cancels the command being run</param>
    /// <returns>The exit code</returns>
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        RunServiceRegistrars();
        RegisterCoreServices();

        _options.ServiceCollection = _services;

        var program = new DefaultProgram(_options, _commandsAssembly);

        var exitCode = await program.RunAsync(_args, cancellationToken);

        Environment.ExitCode = exitCode;

        return exitCode;
    }
}
