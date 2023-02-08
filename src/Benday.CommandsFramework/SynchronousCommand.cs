namespace Benday.CommandsFramework;

public abstract class SynchronousCommand : CommandBase, ISynchronousCommand
{
    public SynchronousCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

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

    protected virtual void OnValidationFailure(
        List<IArgument> validationResult)
    {
        DisplayUsage();
        DisplayValidationSummary(validationResult);
    }

    protected abstract void OnExecute();
}
