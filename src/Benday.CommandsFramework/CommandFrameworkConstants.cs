namespace Benday.CommandsFramework;

public static class CommandFrameworkConstants
{
    public const string CommandName_SetConfig = "set-configuration";
    public const string CommandName_GetConfig = "get-configuration";
    public const string CommandName_RemoveConfig = "remove-configuration";
    public const string CommandArgName_ConfigName = "name";
    public const string CommandArgName_ConfigValue = "value";
    public const string CategoryName_Configuration = "Configuration";
    public const string CommandArgName_QuietMode = "quiet";
    public const int ExitCode_Success = 0;
    public const int ExitCode_Failure = 1;

    /// <summary>
    /// Maximum depth for commands calling other commands. This exists to turn an
    /// accidental A calls B calls A loop into a clear exception rather than a stack overflow.
    /// </summary>
    public const int MaxCommandNestingDepth = 16;
}
