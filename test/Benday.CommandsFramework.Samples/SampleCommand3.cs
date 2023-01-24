namespace Benday.CommandsFramework.Samples;

[Command(Name = ApplicationConstants.CommandName_Command3)]
public class SampleCommand3 : CommandBase
{
    public SampleCommand3(CommandExecutionInfo info) : base(info)
    {

    }    
}
