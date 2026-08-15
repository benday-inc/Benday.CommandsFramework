namespace Benday.CommandsFramework.CmdUi.Models;

public class ToolArgumentInfo
{
    public bool AllowEmptyValue { get; set; }
    public string DataType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FriendlyName { get; set; } = string.Empty;
    public bool HasValue { get; set; }
    public bool IsRequired { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public bool HasAlias { get; set; }
    public bool IsPositionalSource { get; set; }

    /// <summary>
    /// True when this argument reads its value from the tool's stored configuration.
    /// The value can still be overridden here.
    /// </summary>
    public bool IsFromConfig { get; set; }

    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// The default value configured for this argument, if there is one.
    /// </summary>
    public string DefaultValue { get; set; } = string.Empty;

    /// <summary>
    /// True when an explicit default value was configured for this argument.
    /// </summary>
    public bool HasDefaultValue { get; set; }

    public string[] AllowedValues { get; set; } = [];
}
