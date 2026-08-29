namespace Benday.CommandsFramework;

[Command(Name = CommandFrameworkConstants.CommandName_RemoveConfig, 
        Description = "Remove a configuration value",
    Category = CommandFrameworkConstants.CategoryName_Configuration)]
public class RemoveConfigurationValueCommand : Command
{
    public RemoveConfigurationValueCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString(CommandFrameworkConstants.CommandArgName_ConfigName).AsRequired()
            .WithDescription("Name of the configuration name to display");

        return args;
    }

    protected override Task OnExecute(CancellationToken cancellationToken)
    {
        var key = Arguments.GetStringValue(CommandFrameworkConstants.CommandArgName_ConfigName);

        RemoveValue(key);

        return Task.CompletedTask;
    }

    private void RemoveValue(string key)
    {
        if (ExecutionInfo.Configuration.HasValue(key) == false)
        {
            // do nothing            
        }
        else
        {
            ExecutionInfo.Configuration.RemoveValue(key);
        }

        WriteLine($"Configuration value removed for '{key}'.");
    }
}