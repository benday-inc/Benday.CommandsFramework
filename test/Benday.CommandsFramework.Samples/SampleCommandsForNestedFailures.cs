namespace Benday.CommandsFramework.Samples;

/// <summary>
/// Type that looks like a command but has no CommandAttribute, so it cannot be run.
/// </summary>
public class NotACommand : SynchronousCommand
{
    public NotACommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {

    }

    protected override void OnExecute()
    {

    }
}

/// <summary>
/// Sample command that runs another command without supplying the arguments that the
/// other command requires.
/// </summary>
[Command(
    Name = ApplicationConstants.CommandName_CallsOtherCommandsWithBadArgs,
    Description = "Runs the greeting command without giving it the args it needs")]
public class SampleCommandThatCallsOtherCommandsWithBadArgs : SynchronousCommand
{
    public SampleCommandThatCallsOtherCommandsWithBadArgs(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {

    }

    protected override void OnExecute()
    {
        // 'name' is required by the greeting command but is not supplied
        ExecuteCommand<SampleGreetingCommand>();
    }
}

/// <summary>
/// Sample command that tries to run a type that has no CommandAttribute.
/// </summary>
[Command(
    Name = ApplicationConstants.CommandName_CallsATypeWithNoAttribute,
    Description = "Tries to run a type that is not a command")]
public class SampleCommandThatCallsATypeWithNoAttribute : SynchronousCommand
{
    public SampleCommandThatCallsATypeWithNoAttribute(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {

    }

    protected override void OnExecute()
    {
        ExecuteCommand<NotACommand>();
    }
}
