namespace Benday.CommandsFramework;

public interface IArgument
{
    bool AllowEmptyValue { get; }
    ArgumentDataType DataType { get; }
    string Description { get; }
    bool HasValue { get; }
    bool IsRequired { get;  }
    string Name { get; set; }
    string Value { get; }

    bool Validate();
}
