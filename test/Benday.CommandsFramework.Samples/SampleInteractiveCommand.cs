using Benday.CommandsFramework;

namespace Benday.CommandsFramework.Samples;

/// <summary>
/// Sample command demonstrating prompting for input. Anything read through the command's
/// input provider can be driven by a QueuedTextInputProvider in a test, which is what makes
/// an interactive command testable.
/// </summary>
[Command(Name = ApplicationConstants.CommandName_Interactive,
    IsAsync = false,
    Description = "Sample command demonstrating prompting for input.")]
public class SampleInteractiveCommand : SynchronousCommand
{
    public const string ArgumentName_Name = "name";

    public SampleInteractiveCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString(ArgumentName_Name)
            .AsNotRequired()
            .WithDescription("Name to greet. You are asked for it if you do not supply it.");

        return args;
    }

    /// <summary>
    /// The name that was used, whether it came from the command line or from the prompt.
    /// </summary>
    public string NameUsed { get; private set; } = string.Empty;

    /// <summary>
    /// Whether the greeting was actually sent.
    /// </summary>
    public bool DidGreet { get; private set; }

    protected override void OnExecute()
    {
        var name = Arguments.GetStringValue(ArgumentName_Name);

        if (string.IsNullOrWhiteSpace(name) == true)
        {
            name = Prompt("What is your name? ");
        }

        if (string.IsNullOrWhiteSpace(name) == true)
        {
            WriteLine("No name supplied. Nothing to do.");
            return;
        }

        NameUsed = name;

        if (PromptForYesNo($"Say hello to {name}?") == false)
        {
            WriteLine("Suit yourself.");
            return;
        }

        DidGreet = true;

        WriteLine($"Hello, {name}!");
    }
}
