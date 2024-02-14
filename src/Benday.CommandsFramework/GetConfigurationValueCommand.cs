namespace Benday.CommandsFramework;

[Command(Name = CommandFrameworkConstants.CommandName_GetConfig, IsAsync = false, 
    Description = "Display all configuration values or a specific configuration value",
    Category = CommandFrameworkConstants.CategoryName_Configuration)]
public class GetConfigurationValueCommand : SynchronousCommand
{
    public GetConfigurationValueCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString(CommandFrameworkConstants.CommandArgName_ConfigName).AsNotRequired()
            .WithDescription("Name of the configuration name to display");

        return args;
    }

    protected override void OnExecute()
    {
        var showOneValueOnly = Arguments.HasValue(CommandFrameworkConstants.CommandArgName_ConfigName);

        if (showOneValueOnly == false)
        {
            DisplayAllValues();
        }
        else
        {
            var key = Arguments.GetStringValue(CommandFrameworkConstants.CommandArgName_ConfigName);

            DisplayOneValue(key);
        }
    }

    private void DisplayAllValues()
    {
        var values = ExecutionInfo.Configuration.GetValues();

        foreach (var key in values.Keys)
        {
            WriteLine($"{key}: {values[key]}");
        }
    }

    private void DisplayOneValue(string key)
    {
        if (ExecutionInfo.Configuration.HasValue(key) == false)
        {
            WriteLine($"No value found for '{key}'.");
        }
        else
        {
            var value = ExecutionInfo.Configuration.GetValue(key);

            WriteLine($"{key}: {value}");
        }
    }
}
