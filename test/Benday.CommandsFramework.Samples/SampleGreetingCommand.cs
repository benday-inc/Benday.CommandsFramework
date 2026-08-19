namespace Benday.CommandsFramework.Samples;

/// <summary>
/// Sample command that produces a result which other commands can reuse. The result is
/// exposed as a property so that a command that runs this one can read it back.
/// </summary>
[Command(
    Name = ApplicationConstants.CommandName_Greeting,
    Description = "Builds a greeting for a person")]
public class SampleGreetingCommand : Command
{
    public SampleGreetingCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {

    }

    /// <summary>
    /// The greeting that was built. Populated by OnExecute().
    /// </summary>
    public string Greeting { get; private set; } = string.Empty;

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString("name").AsRequired().WithDescription("name of the person to greet");

        args.AddString("salutation").AsNotRequired().WithDescription(
            "salutation to use").WithDefaultValue("Hello");

        return args;
    }

    protected override Task OnExecute(CancellationToken cancellationToken)
    {
        Greeting = $"{Arguments.GetStringValue("salutation")}, {Arguments.GetStringValue("name")}!";

        WriteLine(Greeting);

        return Task.CompletedTask;
    }
}
