using Benday.CommandsFramework;

namespace Benday.CommandsFramework.Samples;

[Command(Name = ApplicationConstants.CommandName_CommandWithAllowedValues,
    Description = "Sample command demonstrating allowed values validation.")]
public class SampleCommandWithAllowedValues : Command
{
    public SampleCommandWithAllowedValues(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    protected override Task OnExecute(CancellationToken cancellationToken)
    {
        WriteLine("** SUCCESS **");
        WriteLine($"environment: {Arguments.GetStringValue("environment")}");
        WriteLine($"mode: {Arguments.GetStringValue("mode")}");

        return Task.CompletedTask;
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
