namespace Benday.CommandsFramework.Samples;

/// <summary>
/// Sample command that fails. A KnownException is a failure the tool expected and reports as
/// a message; anything else is a defect and reaches the caller as an exception.
/// </summary>
/// <remarks>
/// The difference matters most to a host that outlives the command. A console entry point
/// treats both as the end of the process, but a terminal interface has a screen full of work
/// to protect, so it catches the second kind too and costs the user that command rather than
/// everything they had open.
/// </remarks>
[Command(Name = ApplicationConstants.CommandName_Throws,
    Description = "Sample command that fails, for exercising how failures are reported.")]
public class SampleThrowingCommand : Command
{
    public const string ArgumentName_Expected = "expected";

    public SampleThrowingCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddBoolean(ArgumentName_Expected)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription(
                "Fail the way a tool reports a problem it expected, rather than by defect");

        return args;
    }

    protected override Task OnExecute(CancellationToken cancellationToken)
    {
        WriteLine("About to fail.");

        if (Arguments.GetBooleanValue(ArgumentName_Expected) == true)
        {
            throw new KnownException("That did not work.");
        }

        throw new InvalidOperationException("Something nobody planned for.");
    }
}
