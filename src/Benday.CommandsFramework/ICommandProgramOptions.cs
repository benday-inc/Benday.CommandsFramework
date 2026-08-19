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
    /// <remarks>
    /// This is a default interface member so that adding it does not break anything that
    /// already implements ICommandProgramOptions. That also means it is read only here --
    /// DefaultProgramOptions declares it as a settable property, which is what test code
    /// and CommandsApp.ConfigureOptions() work with.
    /// </remarks>
    ITextInputProvider InputProvider { get => new ConsoleTextInputProvider(); }
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
