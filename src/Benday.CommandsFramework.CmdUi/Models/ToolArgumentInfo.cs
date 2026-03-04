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
    public string Value { get; set; } = string.Empty;
    public string[] AllowedValues { get; set; } = [];
}
