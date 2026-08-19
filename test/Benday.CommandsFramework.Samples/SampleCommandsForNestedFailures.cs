namespace Benday.CommandsFramework.Samples;

/// <summary>
/// Type that looks like a command but has no CommandAttribute, so it cannot be run.
/// </summary>
public class NotACommand : Command
{
    public NotACommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {

    }

    protected override Task OnExecute(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// Sample command that runs another command without supplying the arguments that the
/// other command requires.
/// </summary>
[Command(
    Name = ApplicationConstants.CommandName_CallsOtherCommandsWithBadArgs,
    Description = "Runs the greeting command without giving it the args it needs")]
public class SampleCommandThatCallsOtherCommandsWithBadArgs : Command
{
    public SampleCommandThatCallsOtherCommandsWithBadArgs(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {

    }

    protected override async Task OnExecute(CancellationToken cancellationToken)
    {
        // 'name' is required by the greeting command but is not supplied
        await ExecuteCommandAsync<SampleGreetingCommand>(
            cancellationToken: cancellationToken);
    }
}

/// <summary>
/// Sample command that tries to run a type that has no CommandAttribute.
/// </summary>
[Command(
    Name = ApplicationConstants.CommandName_CallsATypeWithNoAttribute,
    Description = "Tries to run a type that is not a command")]
public class SampleCommandThatCallsATypeWithNoAttribute : Command
{
    public SampleCommandThatCallsATypeWithNoAttribute(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {

    }

    protected override async Task OnExecute(CancellationToken cancellationToken)
    {
        await ExecuteCommandAsync<NotACommand>(cancellationToken: cancellationToken);
    }
}
