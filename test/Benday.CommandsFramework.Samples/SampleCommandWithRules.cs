using Benday.CommandsFramework;

namespace Benday.CommandsFramework.Samples;

/// <summary>
/// Sample command demonstrating rules about combinations of arguments. Rules are declarative
/// rather than a callback in OnExecute(), so they show up in usage output and in the --json
/// schema -- which means a form can apply them as it is being filled in rather than only when
/// it is submitted.
/// </summary>
[Command(Name = ApplicationConstants.CommandName_CommandWithRules,
    Description = "Sample command demonstrating argument rules.")]
public class SampleCommandWithRules : Command
{
    public const string ArgumentName_Token = "token";
    public const string ArgumentName_WindowsAuth = "windowsauth";
    public const string ArgumentName_Mode = "mode";
    public const string ArgumentName_Level = "level";
    public const string ArgumentName_Username = "username";
    public const string ArgumentName_Password = "password";

    public SampleCommandWithRules(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString(ArgumentName_Token)
            .AsNotRequired()
            .WithDescription("Personal access token");

        args.AddBoolean(ArgumentName_WindowsAuth)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription("Use Windows authentication instead of a token");

        args.AddString(ArgumentName_Mode)
            .WithAllowedValues("simple", "advanced")
            .AsNotRequired()
            .WithDescription("How much configuration to ask for");

        args.AddInt32(ArgumentName_Level)
            .AsNotRequired()
            .WithDescription("Detail level. Only meaningful in advanced mode.");

        args.AddString(ArgumentName_Username)
            .AsNotRequired()
            .WithDescription("Username");

        args.AddString(ArgumentName_Password)
            .AsNotRequired()
            .WithDescription("Password");

        // this is the whole of what used to be hand written 'if' checks at the top of
        // OnExecute(), where nothing else could see them
        args.ExactlyOneOf(ArgumentName_Token, ArgumentName_WindowsAuth);
        args.RequiredTogether(ArgumentName_Username, ArgumentName_Password);
        args.When(ArgumentName_Mode, "advanced").Require(ArgumentName_Level);
        args.When(ArgumentName_Mode, "simple").Forbid(ArgumentName_Level);

        return args;
    }

    protected override Task OnExecute(CancellationToken cancellationToken)
    {
        WriteLine("** SUCCESS **");
        WriteLine($"{ArgumentName_Mode}: {Arguments.GetStringValue(ArgumentName_Mode)}");

        return Task.CompletedTask;
    }
}
