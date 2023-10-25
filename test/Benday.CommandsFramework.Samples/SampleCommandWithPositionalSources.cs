using System.Text;

namespace Benday.CommandsFramework.Samples;

[Command(Name = ApplicationConstants.CommandName_CommandWithPositionalSources)]
public class SampleCommandWithPositionalSources : SynchronousCommand
{
    public SampleCommandWithPositionalSources(CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddDateTime("thing-date").AsNotRequired().WithDescription("thingy date").WithDefaultValue(new DateTime(2023, 6, 23));
        args.AddInt32("thing-number").AsNotRequired().WithDescription("thingy number").WithDefaultValue(123);
        args.AddBoolean("isThingy").AsNotRequired().WithDescription("is this a thingy?").AllowEmptyValue().WithDefaultValue(true);
        args.AddString("Value1").AsRequired().WithDescription("Value 1 value").FromPositionalArgument(1);
        args.AddString("Value2").AsNotRequired().WithDescription("Value 2 value").FromPositionalArgument(2);

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
