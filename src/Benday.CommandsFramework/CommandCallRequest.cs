namespace Benday.CommandsFramework;

/// <summary>
/// What was asked for: which command, and with what argument values.
/// </summary>
/// <remarks>
/// This used to be mixed into CommandExecutionInfo along with the ambient services a command
/// runs against and the framework's own bookkeeping. Three unrelated things in one bag meant
/// the request could not be built, inspected or reused on its own -- and alias resolution
/// overwrote the command name in place, which destroyed the only record of what the user
/// actually typed.
/// </remarks>
public sealed class CommandCallRequest
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="commandName">The real command name, after any alias has been resolved</param>
    /// <param name="arguments">Argument values. Names are matched without regard to case.</param>
    /// <param name="requestedName">What was actually typed. Defaults to the command name.</param>
    public CommandCallRequest(
        string commandName,
        Dictionary<string, string>? arguments = null,
        string? requestedName = null)
    {
        CommandName = commandName ?? string.Empty;
        RequestedName = string.IsNullOrEmpty(requestedName) ? CommandName : requestedName;

        Arguments = arguments is null
            ? new Dictionary<string, string>(ArgumentCollection.ArgumentNameComparer)
            : new Dictionary<string, string>(arguments, ArgumentCollection.ArgumentNameComparer);
    }

    /// <summary>
    /// The real command name, after any alias has been resolved. Everything downstream deals
    /// only in real command names.
    /// </summary>
    public string CommandName { get; }

    /// <summary>
    /// What was actually typed to reach the command -- the real name, or the alias that was
    /// used. Kept so that messages can quote what the user wrote.
    /// </summary>
    public string RequestedName { get; }

    /// <summary>
    /// True when the command was reached through an alias rather than by its real name.
    /// </summary>
    public bool WasMatchedByAlias =>
        string.Equals(RequestedName, CommandName, StringComparison.OrdinalIgnoreCase) == false;

    /// <summary>
    /// The requested argument values, keyed without regard to case.
    /// </summary>
    public Dictionary<string, string> Arguments { get; }

    public override string ToString() =>
        WasMatchedByAlias ? $"{CommandName} (as {RequestedName})" : CommandName;
}
