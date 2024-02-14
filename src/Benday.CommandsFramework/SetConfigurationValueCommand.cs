namespace Benday.CommandsFramework;

[Command(Name = CommandFrameworkConstants.CommandName_SetConfig, IsAsync = false, Description = "Set a configuration value",
    Category = CommandFrameworkConstants.CategoryName_Configuration)]
public class SetConfigurationValueCommand : SynchronousCommand
{
    public SetConfigurationValueCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString(CommandFrameworkConstants.CommandArgName_ConfigName).AsRequired().WithDescription("Name of the configuration value to set");
        args.AddString(CommandFrameworkConstants.CommandArgName_ConfigValue).AsRequired().WithDescription("Value of the configuration");

        return args;
    }

    protected override void OnExecute()
    {
        var key = Arguments.GetStringValue(CommandFrameworkConstants.CommandArgName_ConfigName);
        var value = Arguments.GetStringValue(CommandFrameworkConstants.CommandArgName_ConfigValue);

        ExecutionInfo.Configuration.SetValue(key, value);

        WriteLine("Configuration value set.");
    }
}
