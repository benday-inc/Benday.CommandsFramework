namespace Benday.CommandsFramework;

/// <summary>
/// Everything a command needs in order to run: what was asked for, the ambient services it
/// runs against, and the framework's own bookkeeping.
/// </summary>
/// <remarks>
/// These three things used to be flat properties on one class, so there was no way to build
/// or inspect a request on its own, and alias resolution overwrote the command name in place.
/// The request is now a CommandCallRequest, which is immutable and remembers what was typed.
/// </remarks>
public class CommandExecutionInfo
{
    private CommandCallRequest _Request = new(string.Empty);
    private ICommandConfigurationManager? _Configuration;

    /// <summary>
    /// What was asked for: the command name and the argument values.
    /// </summary>
    public CommandCallRequest Request
    {
        get => _Request;
        set => _Request = value ?? new CommandCallRequest(string.Empty);
    }

    /// <summary>
    /// Ambient program options.
    /// </summary>
    public ICommandProgramOptions Options { get; set; } = new DefaultProgramOptions();

    /// <summary>
    /// Requested command name. This is the first arg (args[0]) on the command line, resolved
    /// to the real command name when an alias was used.
    /// </summary>
    /// <remarks>
    /// Reads through to Request.CommandName. It is read only now: alias resolution used to
    /// assign it, which destroyed the record of what the user typed. Build a new
    /// CommandCallRequest instead, and read Request.RequestedName for the typed name.
    /// </remarks>
    public string CommandName => Request.CommandName;

    /// <summary>
    /// The requested command line arguments parsed into key/value pairs.
    /// Argument names are matched without regard to case.
    /// </summary>
    /// <remarks>
    /// Reads through to Request.Arguments.
    /// </remarks>
    public Dictionary<string, string> Arguments => Request.Arguments;

    /// <summary>
    /// How many levels deep this command execution is when commands call other commands.
    /// Zero for a command invoked from the command line.
    /// </summary>
    public int NestingDepth { get; set; }

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
