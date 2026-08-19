namespace Benday.CommandsFramework;

/// <summary>
/// Base class for commands.
/// </summary>
/// <remarks>
/// There used to be two of these, SynchronousCommand and AsynchronousCommand, with identical
/// lifecycle logic, a second interface each, about forty lines of branching in
/// DefaultProgram.Run() to tell them apart, and a CommandAttribute.IsAsync flag whose only
/// job was saying which was which -- a flag that re-declared what the type system already
/// knew and that could lie, since nothing checked it against the class it was on.
///
/// Anything that touches the network, async LINQ, or any async API needs an async
/// environment whether or not the command's own logic is sequential, which is why the
/// synchronous base class was nearly unused in practice. A command whose work really is
/// sequential just returns Task.CompletedTask.
/// </remarks>
public abstract class Command : CommandBase
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="info">Command execution information</param>
    /// <param name="outputProvider">Output provider instance</param>
    protected Command(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {
    }

    /// <summary>
    /// Runs the command. Displays the usage information if it was asked for, otherwise
    /// validates the arguments and runs the command when they are valid.
    /// </summary>
    /// <param name="cancellationToken">Cancels this command. Cancelling one command does not
    /// have to mean stopping the process it is running in, which is the whole reason this
    /// exists.</param>
    /// <returns>What happened</returns>
    public virtual async Task<CommandResult> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        if (ExecutionInfo.Arguments.ContainsKey(
                ArgumentFrameworkConstants.ArgumentHelpString) == true)
        {
            DisplayUsage();

            return CommandResult.UsageDisplayed();
        }

        var validationResult = Validate();

        if (validationResult.Count > 0)
        {
            OnValidationFailure(validationResult);

            return CommandResult.ValidationFailed(validationResult);
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            await OnExecute(cancellationToken);

            return CommandResult.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return CommandResult.Cancelled();
        }
    }

    /// <summary>
    /// Template method for handling validation failures. The default implementation
    /// displays the usage info for the command and the summary of validation errors.
    /// </summary>
    /// <param name="validationResult"></param>
    protected virtual void OnValidationFailure(
        List<IArgument> validationResult)
    {
        DisplayUsage();
        DisplayValidationSummary(validationResult);
    }

    /// <summary>
    /// This is where the work of the command goes.
    /// </summary>
    /// <param name="cancellationToken">Pass this to anything that accepts one, and check it
    /// between units of work in a long running loop.</param>
    protected abstract Task OnExecute(CancellationToken cancellationToken);
}
