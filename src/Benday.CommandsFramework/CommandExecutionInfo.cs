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
    /// The requested command line arguments parsed into key/value pairs.
    /// Argument names are matched without regard to case.
    /// </summary>
    public Dictionary<string, string> Arguments { get; set; } =
        new(ArgumentCollection.ArgumentNameComparer);

    /// <summary>
    /// How many levels deep this command execution is when commands call other commands.
    /// Zero for a command invoked from the command line.
    /// </summary>
    public int NestingDepth { get; set; }

    private ICommandConfigurationManager? _Configuration;

    /// <summary>
    /// Returns true if a configuration manager has been set.
    /// </summary>
    public bool HasConfiguration => _Configuration != null;

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