namespace Benday.CommandsFramework;

/// <summary>
/// Information about the requested command execution.
/// </summary>
public class CommandExecutionInfo
{
    public ICommandProgramOptions Options { get; set; } = new DefaultProgramOptions();

    /// <summary>
    /// Requested command name. This is the first arg (args[0]) on the command line
    /// </summary>
    public string CommandName { get; set; } = string.Empty;

    /// <summary>
    /// The requested command line arguments parsed into key/value pairs
    /// </summary>
    public Dictionary<string, string> Arguments { get; set; } = new();

    private ICommandConfigurationManager? _Configuration;
    public ICommandConfigurationManager Configuration
    {
        get
        {
            if (_Configuration == null)
            {
                throw new InvalidOperationException($"No configuration manager set");
            }

            return _Configuration;
        }

        set => _Configuration = value;
    }
}