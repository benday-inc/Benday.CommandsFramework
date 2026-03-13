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
    /// Builds and runs the application synchronously.
    /// </summary>
    public void Run()
    {
        RegisterCoreServices();
        _options.ServiceCollection = _services;

        var program = new DefaultProgram(_options, _commandsAssembly);
        program.Run(_args);
    }

    /// <summary>
    /// Builds and runs the application asynchronously.
    /// </summary>
    public Task RunAsync()
    {
        Run();
        return Task.CompletedTask;
    }
}
