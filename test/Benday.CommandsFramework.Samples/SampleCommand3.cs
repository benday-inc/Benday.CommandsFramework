namespace Benday.CommandsFramework.Samples;

[Command(Name = ApplicationConstants.CommandName_Command3)]
public class SampleCommand3 : CommandBase
{
    public SampleCommand3(CommandExecutionInfo info) : base(info)
    {

    }

    protected override List<IArgument> GetAvailableArguments()
    {
        var expectedArgs = new List<IArgument>();

        expectedArgs.Add(new StringArgument("arg1", true, "argument 1", true, false));
        expectedArgs.Add(new BooleanArgument("isawesome", true, true));
        expectedArgs.Add(new Int32Argument("count", true, true));
        expectedArgs.Add(new DateTimeArgument("dateofthingy", true, true));
        expectedArgs.Add(new BooleanArgument("verbose", false, true));

        return expectedArgs;
    }
}
