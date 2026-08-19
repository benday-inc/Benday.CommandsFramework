using Microsoft.Extensions.DependencyInjection;

namespace Benday.CommandsFramework;

public class DefaultProgramOptions : ICommandProgramOptions
{

    public string ApplicationName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public DisplayUsageOptions DisplayUsageOptions { get; set; } = new();
    private string _ConfigurationFolderName = string.Empty;
    public string ConfigurationFolderName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_ConfigurationFolderName) == true)
            {
                return ApplicationName;
            }
            else
            {
                return _ConfigurationFolderName;
            }            
        }
        set => _ConfigurationFolderName = value;
    }

    public bool UsesConfiguration { get; set; } = true;
    public ITextOutputProvider OutputProvider { get; set; } = new ConsoleTextOutputProvider();

    /// <summary>
    /// Where commands read text input from. Defaults to the console. Swap in a
    /// QueuedTextInputProvider to test a command that prompts.
    /// </summary>
    public ITextInputProvider InputProvider { get; set; } = new ConsoleTextInputProvider();

    /// <summary>
    /// Provides access to the service provider for dependency injection.
    /// This is entirely optional.
    /// </summary>
    public IServiceCollection? ServiceCollection { get; set; } = null;

    /// <summary>
    /// The service provider built from ServiceCollection. Populated on first use and
    /// then shared by every command in the process.
    /// </summary>
    public IServiceProvider? ServiceProvider { get; set; } = null;

    /// <summary>
    /// When true, unknown/unrecognized command arguments will cause validation to fail.
    /// When false (default), unknown arguments are silently ignored.
    /// </summary>
    public bool StrictArgumentValidation { get; set; } = false;
}
