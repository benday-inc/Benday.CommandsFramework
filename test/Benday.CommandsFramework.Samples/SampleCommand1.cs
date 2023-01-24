using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benday.CommandsFramework.Samples;

[Command(Name = ApplicationConstants.CommandName_Command1,
    IsAsync = false,
    Description = "This is the description for command one.")]
public class SampleCommand1 : CommandBase, ISynchronousCommand
{
	public SampleCommand1(CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
	{

	}

    public void Execute()
    {
        if (Arguments.ContainsKey("--help") == true)
        {
            DisplayUsage();
        }

        var validationResult = Validate();

        if (validationResult.Count > 0)
        {
            DisplayUsage();
            DisplayValidationSummary(validationResult);            
        }
        else
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
