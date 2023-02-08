namespace Benday.CommandsFramework.Samples;

[Command(Name = ApplicationConstants.CommandName_Command3)]
public class SampleCommand3 : SynchronousCommand
{
    public SampleCommand3(CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {

    }

    protected override void OnExecute()
    {
        throw new NotImplementedException();
    }
}
