namespace Benday.CommandsFramework;

/// <summary>
/// Sentinel argument used to represent an argument key that was supplied on the command
/// line but is not defined by the command. Instances are created during validation and
/// surfaced in the validation summary as "Unknown argument: {name}".
/// </summary>
internal class UnknownArgument : IArgument
{
    public UnknownArgument(string name)
    {
        Name = name;
    }

    public string Name { get; set; }
    public string Alias { get; set; } = string.Empty;
    public bool AllowEmptyValue => false;
    public ArgumentDataType DataType => ArgumentDataType.String;
    public string Description => Name;
    public string FriendlyName => Name;
    public bool HasValue => false;
    public bool HasAlias => false;
    public bool IsRequired => false;
    public bool IsPositionalSource { get; set; }
    public bool IsFromConfig { get; set; }
    public string Value => string.Empty;
    public string[] AllowedValues => [];
    public bool Validate() => false;
    public bool TrySetValue(string input) => false;
}
