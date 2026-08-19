using Benday.CommandsFramework;

namespace Benday.CommandsFramework.Samples;

/// <summary>
/// Sample command demonstrating file and directory arguments. Every other sample uses
/// string, int, boolean or date arguments, so this is the only sample that exercises
/// AddFile() and AddDirectory() through the real --help, --json and cmdui paths rather
/// than through a unit test on the argument class alone.
/// </summary>
[Command(Name = ApplicationConstants.CommandName_CommandWithFileAndDirectoryArgs,
    IsAsync = false,
    Description = "Sample command demonstrating file and directory arguments.")]
public class SampleCommandWithFileAndDirectoryArgs : SynchronousCommand
{
    public const string ArgumentName_InputFile = "inputfile";
    public const string ArgumentName_OptionalFile = "optionalfile";
    public const string ArgumentName_OutputDirectory = "outputdir";
    public const string ArgumentName_OptionalDirectory = "optionaldir";

    public SampleCommandWithFileAndDirectoryArgs(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddFile(ArgumentName_InputFile)
            .MustExist()
            .AsRequired()
            .WithDescription("File to read. Has to exist.")
            .WithFriendlyName("Input file");

        args.AddFile(ArgumentName_OptionalFile)
            .ExistenceOptional()
            .AsNotRequired()
            .WithDescription("File to write. Does not have to exist yet.")
            .WithFriendlyName("Output file");

        args.AddDirectory(ArgumentName_OutputDirectory)
            .MustExist()
            .AsRequired()
            .WithDescription("Directory to write results to. Has to exist.")
            .WithFriendlyName("Output directory");

        args.AddDirectory(ArgumentName_OptionalDirectory)
            .ExistenceOptional()
            .AsNotRequired()
            .WithDescription("Directory for temporary files. Does not have to exist yet.")
            .WithFriendlyName("Temp directory");

        return args;
    }

    protected override void OnExecute()
    {
        WriteLine("** SUCCESS **");

        // the file and directory extension methods resolve the value to a full path
        WriteLine($"{ArgumentName_InputFile}: {Arguments.GetPathToFile(ArgumentName_InputFile, true)}");
        WriteLine($"{ArgumentName_OutputDirectory}: {Arguments.GetPathToDirectory(ArgumentName_OutputDirectory, true)}");

        if (Arguments.HasValue(ArgumentName_OptionalFile) == true)
        {
            WriteLine($"{ArgumentName_OptionalFile}: {Arguments.GetPathToFile(ArgumentName_OptionalFile)}");
        }

        if (Arguments.HasValue(ArgumentName_OptionalDirectory) == true)
        {
            WriteLine($"{ArgumentName_OptionalDirectory}: {Arguments.GetPathToDirectory(ArgumentName_OptionalDirectory)}");
        }
    }
}
