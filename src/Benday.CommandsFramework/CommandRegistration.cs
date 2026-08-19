using System.Reflection;

namespace Benday.CommandsFramework;

/// <summary>
/// Everything the framework knows about one command, worked out once when the registry is
/// built rather than rediscovered by each caller sweeping the assembly again.
/// </summary>
public sealed class CommandRegistration
{
    internal CommandRegistration(
        Type commandType,
        CommandAttribute attribute,
        Assembly sourceAssembly,
        bool isBuiltIn,
        IReadOnlyList<CommandAliasInfo> aliases)
    {
        CommandType = commandType;
        Attribute = attribute;
        SourceAssembly = sourceAssembly;
        IsBuiltIn = isBuiltIn;
        Aliases = aliases;

        // Path is a list rather than a string so that multi-level command names have
        // somewhere to live. Until commands can declare a group, every path is one segment.
        Path = string.IsNullOrWhiteSpace(attribute.Group)
            ? [attribute.Name]
            : [attribute.Group, attribute.Name];
    }

    /// <summary>
    /// The tokens that name this command on the command line, in order. One segment for a
    /// flat command name, two when the command declares a group.
    /// </summary>
    public IReadOnlyList<string> Path { get; }

    /// <summary>
    /// The command's own name -- the last segment of Path.
    /// </summary>
    public string Name => Attribute.Name;

    /// <summary>
    /// The group this command belongs to, or empty when it is a flat command name. This is
    /// part of how the command is typed, unlike Category which is only a display heading.
    /// </summary>
    public string Group => Attribute.Group;

    /// <summary>
    /// Display heading for grouping commands in usage output. Not part of the command name.
    /// </summary>
    public string Category => Attribute.Category;

    /// <summary>
    /// Human readable description of the command.
    /// </summary>
    public string Description => Attribute.Description;

    /// <summary>
    /// The attribute the registration was built from.
    /// </summary>
    public CommandAttribute Attribute { get; }

    /// <summary>
    /// The class that implements the command.
    /// </summary>
    public Type CommandType { get; }

    /// <summary>
    /// The assembly the command was found in.
    /// </summary>
    public Assembly SourceAssembly { get; }

    /// <summary>
    /// True for the framework's own built-in commands, such as the configuration commands.
    /// They are registered exactly like any other command; this only marks where they came
    /// from.
    /// </summary>
    public bool IsBuiltIn { get; }

    /// <summary>
    /// Every alternate name for this command -- plain renames from CommandAttribute.Aliases
    /// and presets from CommandAliasAttribute alike.
    /// </summary>
    public IReadOnlyList<CommandAliasInfo> Aliases { get; }

    /// <summary>
    /// The command's path as it is typed, for messages and usage output.
    /// </summary>
    public string PathAsString => string.Join(" ", Path);

    public override string ToString() => PathAsString;
}
