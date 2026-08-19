using Benday.CommandsFramework;

namespace Benday.CommandsFramework.Samples;

/// <summary>
/// Sample command whose input file is found rather than supplied, when the working directory
/// holds exactly one candidate. This is the "find the one .json here, and make me say which
/// one if there is not exactly one" shape.
/// </summary>
[Command(Name = ApplicationConstants.CommandName_CommandWithDiscovery,
    Description = "Sample command that finds its input file when there is exactly one.")]
public class SampleCommandWithDiscovery : Command
{
    public const string ArgumentName_InputFile = "inputfile";

    public SampleCommandWithDiscovery(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddFile(ArgumentName_InputFile)
            .DiscoverSingleMatch("*.json")
            .AsRequired()
            .WithDescription("File to read. Found automatically when the working directory " +
                "holds exactly one .json file.");

        return args;
    }

    /// <summary>
    /// The file that was used, whether it was supplied or found.
    /// </summary>
    public string InputFileUsed { get; private set; } = string.Empty;

    protected override Task OnExecute(CancellationToken cancellationToken)
    {
        InputFileUsed = Arguments.GetStringValue(ArgumentName_InputFile);

        WriteLine("** SUCCESS **");
        WriteLine($"{ArgumentName_InputFile}: {InputFileUsed}");

        return Task.CompletedTask;
    }
}
