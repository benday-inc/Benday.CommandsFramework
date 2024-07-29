using System.Text;

namespace Benday.CommandsFramework.Samples;


[Command(Name = ApplicationConstants.CommandName_CommandWithFriendlyNameValues)]
public class SampleCommandWithFriendlyNameValues : SynchronousCommand
{
    public SampleCommandWithFriendlyNameValues(CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddDateTime("thing-date")
            .WithFriendlyName("Thing Date")
            .AsNotRequired()
            .WithDescription("thingy date")
            .WithDefaultValue(new DateTime(2023, 6, 23));
        args.AddInt32("thing-number")
            .WithFriendlyName("Thing Number")
            .AsNotRequired()
            .WithDescription("thingy number")
            .WithDefaultValue(123);
        args.AddBoolean("isThingy")
            .WithFriendlyName("Is Thingy")
            .AsNotRequired()
            .WithDescription("is this a thingy?")
            .AllowEmptyValue()
            .WithDefaultValue(true);
        args.AddString("bingbong")
            .WithFriendlyName("Bing Bong")
            .AsNotRequired()
            .WithDescription("bing bong? what is bing bong? this is a long " +
            "description. not insanely long but long enough to need wrapping. probably. right? wrap this value? " +
            "It should wrap. I mean, i need something to test wrapped arg descriptions with.")
            .WithDefaultValue("wickid awesome");


        return args;
    }

    protected override void OnExecute()
    {
        var builder = new StringBuilder();

        builder.AppendLine("** SUCCESS **");

        foreach (var key in Arguments.Keys)
        {
            var value = Arguments[key];

            builder.AppendLine($"{key}: {value.Value}");
        }

        _OutputProvider.WriteLine(builder.ToString());
    }
}
