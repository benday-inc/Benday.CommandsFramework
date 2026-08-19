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
    /// <summary>
    /// Version of the shape that --json writes. Version 1 was a bare array of commands with
    /// no envelope; version 2 is a CommandSchema object. A consumer can tell them apart from
    /// the root JSON token alone, which is why there is no negotiation.
    /// </summary>
    public const int CurrentSchemaVersion = 2;

    /// <summary>
    /// Width to wrap output at when the real width is not known -- output is redirected, or
    /// there is no console at all.
    /// </summary>
    public const int DefaultOutputWidth = 60;

    public const int ExitCode_Success = 0;
    public const int ExitCode_Failure = 1;

    /// <summary>
    /// Maximum depth for commands calling other commands. This exists to turn an
    /// accidental A calls B calls A loop into a clear exception rather than a stack overflow.
    /// </summary>
    public const int MaxCommandNestingDepth = 16;
}
