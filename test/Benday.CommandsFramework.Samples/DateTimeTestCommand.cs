using System;
using System.Linq;

namespace Benday.CommandsFramework.Samples;

[Command(Name = "datetimetest", Description = "Date time test")]
public class DateTimeTestCommand : Command
{
    public DateTimeTestCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddDateTime("date")
            .WithDescription("Date")
            .AsRequired();

        return args;
    }

    protected override Task OnExecute(CancellationToken cancellationToken)
    {
        var date = Arguments.GetDateTimeValue("date");

        WriteLine($"** SUCCESS **");

        WriteLine($"Date: {date.ToUniversalTime().ToString("yyyyMMddTHHmmssffffZ")}");

        return Task.CompletedTask;
    }
}