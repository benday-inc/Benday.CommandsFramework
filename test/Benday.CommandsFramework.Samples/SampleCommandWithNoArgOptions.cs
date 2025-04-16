using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Benday.CommandsFramework;

namespace Benday.CommandsFramework.Samples;

[Command(Name = ApplicationConstants.CommandName_CommandWithNoArgs,
    IsAsync = false,
    Description = "This is the description for command one.")]
public class SampleCommandWithNoArgOptions : SynchronousCommand
{
	public SampleCommandWithNoArgOptions(CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
	{

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

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        // no args

        return args;
    }
}
