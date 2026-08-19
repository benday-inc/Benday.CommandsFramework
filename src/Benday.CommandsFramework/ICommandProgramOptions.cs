using Microsoft.Extensions.DependencyInjection;

namespace Benday.CommandsFramework;

public interface ICommandProgramOptions
{
    string ApplicationName { get; set; }
    DisplayUsageOptions DisplayUsageOptions { get; set; }
    string Version { get; set; }
    string Website { get; set; }
    string ConfigurationFolderName { get; set; }
    bool UsesConfiguration { get; set; }
    ITextOutputProvider OutputProvider { get; set; }

    /// <summary>
    /// Where commands read text input from. The counterpart to OutputProvider.
    /// </summary>
    ITextInputProvider InputProvider { get; set; }

    /// <summary>
    /// The set of commands this program can run. Built the first time it is needed and then
    /// shared, the same way ServiceProvider is, so the assemblies are only scanned once.
    /// </summary>
    CommandRegistry? CommandRegistry { get; set; }
    IServiceCollection? ServiceCollection { get; set; }

    /// <summary>
    /// The service provider built from ServiceCollection. This is populated the first
    /// time a command needs it and is then shared by every command in the process so
    /// that singleton services really are singletons and the container is only built once.
    /// </summary>
    IServiceProvider? ServiceProvider { get; set; }

    /// <summary>
    /// When true, unknown/unrecognized command arguments will cause validation to fail.
    /// When false (default), unknown arguments are silently ignored.
    /// </summary>
    bool StrictArgumentValidation { get; set; }
}
