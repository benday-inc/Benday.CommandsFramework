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
    IServiceCollection? ServiceCollection { get; set; }

    /// <summary>
    /// When true, unknown/unrecognized command arguments will cause validation to fail.
    /// When false (default), unknown arguments are silently ignored.
    /// </summary>
    bool StrictArgumentValidation { get; set; }
}
