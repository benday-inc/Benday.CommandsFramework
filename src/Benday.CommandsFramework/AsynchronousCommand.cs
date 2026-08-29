namespace Benday.CommandsFramework;

/// <summary>
/// Base class for commands that require access to async functionality for execution.
/// </summary>
/// <remarks>
/// There is only one command base class now. This is kept so that existing commands do not
/// have to be renamed all at once; it adds nothing. The one thing that does have to change
/// is the signature of OnExecute(), which now takes a CancellationToken -- the compiler will
/// point at every one of them.
/// </remarks>
[Obsolete("Derive from Command instead. AsynchronousCommand adds nothing and will be removed in v6.")]
public abstract class AsynchronousCommand : Command
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="info">Command execution information</param>
    /// <param name="outputProvider">Output provider instance</param>
    protected AsynchronousCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {
    }
}
