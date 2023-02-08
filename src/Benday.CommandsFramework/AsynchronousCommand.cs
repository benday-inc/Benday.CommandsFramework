namespace Benday.CommandsFramework;

public abstract class AsynchronousCommand : CommandBase, IAsyncCommand
{
    public AsynchronousCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public virtual async Task ExecuteAsync()
    {
        if (Arguments.ContainsKey("--help") == true)
        {
            DisplayUsage();
        }

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

    protected virtual void OnValidationFailure(
        List<IArgument> validationResult)
    {
        DisplayUsage();
        DisplayValidationSummary(validationResult);
    }

    protected abstract Task OnExecute();
}
