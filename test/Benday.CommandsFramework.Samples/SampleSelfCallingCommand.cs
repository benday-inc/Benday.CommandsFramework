namespace Benday.CommandsFramework.Samples;

/// <summary>
/// Sample command that calls itself. Used to verify that the nesting depth guard turns
/// an accidental loop into a clear exception rather than a stack overflow.
/// </summary>
[Command(
    Name = ApplicationConstants.CommandName_SelfCalling,
    Description = "Command that calls itself forever")]
public class SampleSelfCallingCommand : Command
{
    public SampleSelfCallingCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {

    }

    protected override async Task OnExecute(CancellationToken cancellationToken)
    {
        await ExecuteCommandAsync<SampleSelfCallingCommand>(
            cancellationToken: cancellationToken);
    }
}
