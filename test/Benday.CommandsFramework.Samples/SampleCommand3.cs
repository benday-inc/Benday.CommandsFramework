namespace Benday.CommandsFramework.Samples;

[Command(Name = ApplicationConstants.CommandName_Command3)]
public class SampleCommand3 : SynchronousCommand
{
    public SampleCommand3(CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddDateTime("thing-date").AsNotRequired().WithDescription("thingy date");
        args.AddInt32("thing-number").AsRequired().WithDescription("thingy number");
        args.AddBoolean("isThingy").AsNotRequired().WithDescription("is this a thingy?").AllowEmptyValue();
        args.AddString("bingbong").WithDescription("bing bong? what is bing bong? this is a long " +
            "description. not insanely long but long enough to need wrapping. probably. right? wrap this value? " +
            "It should wrap. I mean, i need something to test wrapped arg descriptions with.");

        return args;
    }

    protected override void OnExecute()
    {
        throw new NotImplementedException();
    }
}
