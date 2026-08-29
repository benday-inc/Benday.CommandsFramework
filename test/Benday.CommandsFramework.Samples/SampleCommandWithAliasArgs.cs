using System.Text;

namespace Benday.CommandsFramework.Samples;

[Command(Name = ApplicationConstants.CommandName_CommandWithAliases)]
public class SampleCommandWithAliasArgs : Command
{
    public SampleCommandWithAliasArgs(CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();
             
        args.AddString("Value1").AsRequired().WithDescription("Value 1 value").WithAlias("Alias1");
        args.AddString("Value2").AsNotRequired().WithDescription("Value 2 value").WithAlias("Alias2");

        return args;
    }

    protected override Task OnExecute(CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();

        builder.AppendLine("** SUCCESS **");

        foreach (var key in Arguments.Keys)
        {
            var value = Arguments[key];

            builder.AppendLine($"{key}: {value.Value}");
        }

        _OutputProvider.WriteLine(builder.ToString());

        return Task.CompletedTask;
    }
}
