using Microsoft.VisualBasic;

namespace Benday.CommandsFramework.Samples;

[Command(Name = ApplicationConstants.CommandName_Command3)]
public class SampleCommand3 : SynchronousCommand
{
    public SampleCommand3(CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {

    }

    protected override ArgumentCollection GetAvailableArguments()
    {
        var args = new ArgumentCollection();

        args.AddDateTime("thing-date").AsNotRequired().WithDescription("thingy date");
        args.AddInt32("thing-number").AsRequired().WithDescription("thingy number");
        args.AddBoolean("isThingy").AsNotRequired().WithDescription("is this a thingy?").AllowEmptyValue();
        args.AddString("bingbong").WithDescription("bing bong?");

        return args;
    }

    protected override void OnExecute()
    {
        throw new NotImplementedException();
    }
}
