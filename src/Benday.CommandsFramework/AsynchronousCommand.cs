namespace Benday.CommandsFramework;

/// <summary>
/// Base class for commands that require access to async functionality for execution.
/// </summary>
public abstract class AsynchronousCommand : CommandBase, IAsyncCommand
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="info">Command execution information</param>
    /// <param name="outputProvider">Output provider instance</param>
    public AsynchronousCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    /// <summary>
    /// This is the async method that's called to run the command.
    /// It looks to see if the help argument string (--help) is on the command line 
    /// and displays the usage if necessary. If the help string is not present, 
    /// the arguments are validated. If validation succeeds, then OnExecute() is called
    /// </summary>
    /// <returns>Async task execution status</returns>
    public virtual async Task ExecuteAsync()
    {
        if (ExecutionInfo.Arguments.ContainsKey(
                ArgumentFrameworkConstants.ArgumentHelpString) == true)
        {
            DisplayUsage();
        }
        else
        {
            var validationResult = Validate();

            if (validationResult.Count > 0)
            {
                OnValidationFailure(validationResult);
            }
            else
            {
                await OnExecute();
            }
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
    /// Abstract method. This is where the work of the command should be added.
    /// </summary>
    /// <returns></returns>
    protected abstract Task OnExecute();
}
