namespace Benday.CommandsFramework.Samples;

[Command(Name = ApplicationConstants.CommandName_Command2)]
public class SampleCommand2 : CommandBase
{
    public SampleCommand2(CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {

    }
}
