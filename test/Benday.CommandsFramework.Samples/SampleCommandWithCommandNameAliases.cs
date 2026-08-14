using System.Text;

namespace Benday.CommandsFramework.Samples;

/// <summary>
/// Sample command that can also be run using a short alias instead of its real name.
/// </summary>
[Command(
    Name = ApplicationConstants.CommandName_CommandWithCommandNameAliases,
    Aliases = new[] { "mc", "mycmd" },
    Description = "Command that has short aliases for its name")]
public class SampleCommandWithCommandNameAliases : SynchronousCommand
{
    public SampleCommandWithCommandNameAliases(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString("message").AsNotRequired().WithDescription(
            "message to display").WithDefaultValue("hello");

        return args;
    }

    protected override void OnExecute()
    {
        var builder = new StringBuilder();

        builder.AppendLine("** SUCCESS **");
        builder.AppendLine($"command name: {ExecutionInfo.CommandName}");
        builder.AppendLine($"message: {Arguments.GetStringValue("message")}");

        _OutputProvider.WriteLine(builder.ToString());
    }
}
