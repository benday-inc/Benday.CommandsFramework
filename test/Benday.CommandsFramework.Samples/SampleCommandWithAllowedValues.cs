using Benday.CommandsFramework;

namespace Benday.CommandsFramework.Samples;

[Command(Name = ApplicationConstants.CommandName_CommandWithAllowedValues,
    IsAsync = false,
    Description = "Sample command demonstrating allowed values validation.")]
public class SampleCommandWithAllowedValues : SynchronousCommand
{
    public SampleCommandWithAllowedValues(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    protected override void OnExecute()
    {
        WriteLine("** SUCCESS **");
        WriteLine($"environment: {Arguments.GetStringValue("environment")}");
        WriteLine($"mode: {Arguments.GetStringValue("mode")}");
    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString("environment")
            .WithAllowedValues("dev", "staging", "prod")
            .AsRequired()
            .WithDescription("Target environment");

        args.AddString("mode")
            .WithAllowedValues("fast", "slow")
            .AsNotRequired()
            .WithDescription("Optional run mode");

        return args;
    }
}
