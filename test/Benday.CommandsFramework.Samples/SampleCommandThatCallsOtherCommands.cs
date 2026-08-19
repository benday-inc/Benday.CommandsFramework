using System.Text;

namespace Benday.CommandsFramework.Samples;

/// <summary>
/// Sample command that reuses the logic in another command by running it in process
/// rather than shelling out to the command line.
/// </summary>
[Command(
    Name = ApplicationConstants.CommandName_CallsOtherCommands,
    Description = "Reuses the greeting command to greet several people")]
public class SampleCommandThatCallsOtherCommands : Command
{
    public SampleCommandThatCallsOtherCommands(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {

    }

    /// <summary>
    /// The greetings that were collected from the greeting command.
    /// </summary>
    public List<string> Greetings { get; } = new();

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString("names").AsRequired().WithDescription("comma separated list of names");

        args.AddString("salutation").AsNotRequired().WithDescription("salutation to use");

        return args;
    }

    protected override async Task OnExecute(CancellationToken cancellationToken)
    {
        var names = Arguments.GetStringValue("names")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var salutation = Arguments.GetStringValue("salutation");

        foreach (var name in names)
        {
            var command = await ExecuteCommandAsync<SampleGreetingCommand>(args =>
            {
                args["name"] = name;

                if (string.IsNullOrEmpty(salutation) == false)
                {
                    args["salutation"] = salutation;
                }
            }, cancellationToken: cancellationToken);

            Greetings.Add(command.Greeting);
        }

        var builder = new StringBuilder();

        builder.AppendLine("** SUCCESS **");

        foreach (var greeting in Greetings)
        {
            builder.AppendLine(greeting);
        }

        WriteLine(builder.ToString());

    }
}
