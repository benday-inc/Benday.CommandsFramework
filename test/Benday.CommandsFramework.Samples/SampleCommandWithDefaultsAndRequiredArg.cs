using System.Text;

namespace Benday.CommandsFramework.Samples;

/// <summary>
/// Sample command that mixes a required argument with an argument that has a default
/// value. Used to verify that the usage output shows the configured default value
/// rather than the value supplied on the command line when usage is displayed as a
/// result of a validation failure.
/// </summary>
[Command(Name = ApplicationConstants.CommandName_CommandWithDefaultsAndRequiredArg,
    Description = "Command with a required arg plus an arg that has a default value")]
public class SampleCommandWithDefaultsAndRequiredArg : SynchronousCommand
{
    public SampleCommandWithDefaultsAndRequiredArg(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString("required-thing").AsRequired().WithDescription("this one is required");

        args.AddString("bingbong").AsNotRequired().WithDescription(
            "optional thingy").WithDefaultValue("wickid awesome");

        // no description, but has a default value
        args.AddInt32("countish").AsNotRequired().WithDefaultValue(42);

        return args;
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
}
