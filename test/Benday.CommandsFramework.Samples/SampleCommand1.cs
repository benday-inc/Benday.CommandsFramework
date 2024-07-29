using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Benday.CommandsFramework;

namespace Benday.CommandsFramework.Samples;

[Command(Name = ApplicationConstants.CommandName_Command1,
    IsAsync = false,
    Description = "This is the description for command one.")]
public class SampleCommand1 : SynchronousCommand
{
	public SampleCommand1(CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
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

        args.AddString("arg1").AsRequired().WithDescription("argument 1").AsRequired();
        args.AddBoolean("isawesome").WithDescription("is awesome?").AsRequired().AllowEmptyValue();
        args.AddInt32("count").WithDescription("count of things").AsRequired().AllowEmptyValue();
        args.AddDateTime("dateofthingy").WithDescription("date of thingy").AsRequired();
        args.AddBoolean("verbose").AsNotRequired().AllowEmptyValue();

        return args;
    }
}
