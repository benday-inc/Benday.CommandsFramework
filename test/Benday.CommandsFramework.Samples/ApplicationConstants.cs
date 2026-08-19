namespace Benday.CommandsFramework.Samples;

public static class ApplicationConstants
{
    public const string CommandName_Command1 = "command1";
    public const string CommandName_Command2 = "command2";
    public const string CommandName_Command3 = "command3";
    public const string CommandName_CommandWithDefaultValues = "defaultvaluescommand";
    public const string CommandName_CommandWithFriendlyNameValues = "friendlyvaluescommand";
    public const string CommandName_CommandWithPositionalSources = "positionals";
    public const string CommandName_CommandWithAliases = "aliases";
    public const string CommandName_CommandThatUsesConfig = "useconfig";
    public const string CommandName_CommandWithNoArgs = "noargs";
    public const string CommandName_CommandWithAllowedValues = "allowedvaluescommand";
    public const string CommandName_CommandWithFileAndDirectoryArgs = "filesanddirs";
    public const string CommandName_Interactive = "interactive";
    public const string CommandName_ConstructorInjection = "injected-greeting";
    public const string CommandGroup_Widget = "widget";
    public const string CommandName_WidgetList = "list";
    public const string CommandName_WidgetShow = "show";
    public const string CommandAlias_ShowWidget = "showwidget";
    public const string CommandName_CommandWithDefaultsAndRequiredArg = "defaultsandrequired";
    public const string CommandName_CommandWithCommandNameAliases = "command-with-a-long-name";
    public const string CommandName_Greeting = "greeting";
    public const string CommandName_CallsOtherCommands = "greet-everybody";
    public const string CommandName_SelfCalling = "selfcalling";
    public const string CommandName_AsyncGreeting = "async-greeting";
    public const string CommandName_AsyncCallsOtherCommands = "async-greet-everybody";
    public const string CommandName_CallsOtherCommandsWithBadArgs = "greet-badly";
    public const string CommandName_CallsATypeWithNoAttribute = "not-a-command-caller";
    public const string CommandName_Deploy = "deploy";
    public const string CommandAlias_DeployProd = "deploy-prod";
    public const string CommandAlias_DeployDev = "deploy-dev";
}