using System.Text;

namespace Benday.CommandsFramework.Samples;

[Command(Name = ApplicationConstants.CommandName_CommandThatUsesConfig)]
public class SampleCommandThatUsesConfig : Command
{
    public SampleCommandThatUsesConfig(CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        return args;
    }

    protected override Task OnExecute(CancellationToken cancellationToken)
    {
        var config = base.ExecutionInfo.Configuration;

        var keyname = "testkey";
        var value = DateTime.Now.Ticks.ToString();

        config.SetValue(keyname, value);

        var actualValue = config.GetValue(keyname);

        if (value != actualValue)
        {
            throw new InvalidOperationException($"Config set didn't work");
        }
        else
        {
            WriteLine("** SUCCESS **");
        }

        return Task.CompletedTask;
    }
}
