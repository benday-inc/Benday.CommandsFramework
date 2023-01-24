using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benday.CommandsFramework.Samples;

[Command(Name = ApplicationConstants.CommandName_Command1, IsAsync = false)]
public class SampleCommand1 : CommandBase, ISynchronousCommand
{
	public SampleCommand1(CommandExecutionInfo info) : base(info)
	{

	}

    public void Execute()
    {
        var validationResult = Validate();

        if (validationResult.Count > 0)
        {
            throw new InvalidOperationException($"Invalid. Nope.");
        }
        else
        {
            var builder = new StringBuilder();

            foreach (var key in Arguments.Keys)
            {
                var value = Arguments[key];

                builder.AppendLine($"{key}: {value.Value}");
            }
        }
    }

    protected override ArgumentCollection GetAvailableArguments()
    {
        var expectedArgs = new Dictionary<string, IArgument>();

        expectedArgs.Add("arg1", new StringArgument("arg1", true, "argument 1", true, false));
        expectedArgs.Add("isawesome", new BooleanArgument("isawesome", true, true));
        expectedArgs.Add("count", new Int32Argument("count", true, true));
        expectedArgs.Add("dateofthingy", new DateTimeArgument("dateofthingy", true, true));
        expectedArgs.Add("verbose", new BooleanArgument("verbose", false, true));

        return new(expectedArgs);
    }
}
