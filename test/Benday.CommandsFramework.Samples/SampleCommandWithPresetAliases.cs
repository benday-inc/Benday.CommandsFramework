using System.Text;

namespace Benday.CommandsFramework.Samples;

/// <summary>
/// Sample command with aliases that supply argument values, so that a commonly used
/// combination of arguments can be run as a single short name.
/// </summary>
[Command(
    Name = ApplicationConstants.CommandName_Deploy,
    Description = "Deploys a thing to an environment")]
[CommandAlias(ApplicationConstants.CommandAlias_DeployProd,
    "environment=production", "verbose",
    Description = "Deploy to production with verbose output")]
[CommandAlias(ApplicationConstants.CommandAlias_DeployDev,
    "environment=development",
    Description = "Deploy to development")]
public class SampleCommandWithPresetAliases : Command
{
    public SampleCommandWithPresetAliases(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {

    }

    public string Environment { get; private set; } = string.Empty;
    public bool Verbose { get; private set; }
    public string Thing { get; private set; } = string.Empty;

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString("environment")
            .WithAllowedValues("production", "development", "staging")
            .AsRequired()
            .WithDescription("environment to deploy to");

        args.AddString("thing").AsNotRequired().WithDescription(
            "thing to deploy").WithDefaultValue("the-usual-thing");

        args.AddBoolean("verbose").AsNotRequired().AllowEmptyValue().WithDescription(
            "verbose output");

        return args;
    }

    protected override Task OnExecute(CancellationToken cancellationToken)
    {
        Environment = Arguments.GetStringValue("environment");
        Verbose = Arguments.GetBooleanValue("verbose");
        Thing = Arguments.GetStringValue("thing");

        var builder = new StringBuilder();

        builder.AppendLine("** SUCCESS **");
        builder.AppendLine($"environment: {Environment}");
        builder.AppendLine($"thing: {Thing}");
        builder.AppendLine($"verbose: {Verbose}");

        WriteLine(builder.ToString());

        return Task.CompletedTask;
    }
}
