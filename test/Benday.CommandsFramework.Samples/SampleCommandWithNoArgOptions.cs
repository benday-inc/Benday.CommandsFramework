using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Benday.CommandsFramework;

namespace Benday.CommandsFramework.Samples;

[Command(Name = ApplicationConstants.CommandName_CommandWithNoArgs,
    Description = "This is the description for command one.")]
public class SampleCommandWithNoArgOptions : Command
{
	public SampleCommandWithNoArgOptions(CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
	{

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

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        // no args

        return args;
    }
}
