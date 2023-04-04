namespace Benday.CommandsFramework;

/// <summary>
/// Base class for commands that are run synchronously.
/// </summary>
public abstract class SynchronousCommand : CommandBase, ISynchronousCommand
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="info">Command execution information</param>
    /// <param name="outputProvider">Output provider instance</param>
    public SynchronousCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    /// <summary>
    /// Executes the command
    /// </summary>
    public virtual void Execute()
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
                OnExecute();
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
    protected abstract void OnExecute();
}
