namespace Benday.CommandsFramework;

/// <summary>
/// Add this attribute to a class to indicate that the class contains a command that
/// is runnable through the commands framework. This attribute provides information 
/// about the command name, whether it uses async execution, and optionally the human
/// readable description.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class CommandAttribute : Attribute
{
    /// <summary>
    /// Name of command. This is the command argument (arg[0]) from the command line.
    /// This value is used to locate the command to be instantiated and run.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// True if this command should be run in async mode
    /// </summary>
    public bool IsAsync { get; set; } = false;

    /// <summary>
    /// Human readable description of the command.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Category for the command. This is used to group commands together in the help output.
    /// This is a display heading only -- it is not part of how the command is typed.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Group this command belongs to, which becomes the first segment of the command name:
    /// a command named 'list' in group 'workitem' is run as 'mytool workitem list'.
    /// </summary>
    /// <remarks>
    /// This is deliberately separate from Category. Category holds display strings like
    /// "Work Items" and "Project Administration", so using it as a prefix would produce
    /// command names nobody would type. Grouping is a rename, not a prefix.
    /// </remarks>
    public string Group { get; set; } = string.Empty;

    /// <summary>
    /// Alternate names that can be used on the command line in place of Name. This is
    /// useful for providing a short form of a long command name, for example 'mc' as an
    /// alias for 'my-super-long-command-name'.
    /// Aliases are resolved to the real command name before the command runs, so the rest
    /// of the framework only ever sees Name. Real command names always take precedence
    /// over aliases.
    /// </summary>
    public string[] Aliases { get; set; } = [];
}
