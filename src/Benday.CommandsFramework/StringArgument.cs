namespace Benday.CommandsFramework;

public class StringArgument : Argument<string>
{
    public StringArgument(string name, bool isRequired = true, bool allowEmptyValue = true) :
        base(name, name, isRequired, allowEmptyValue)
    {

    }

    public StringArgument(string name, string value, bool isRequired = true, bool allowEmptyValue = true) :
        base(name, value, name, isRequired, allowEmptyValue)
    {

    }

    public StringArgument(string name, string value, string description, bool isRequired, bool allowEmptyValue) :
        base(name, value, description, isRequired, allowEmptyValue)
    {

    }

    public StringArgument(string name, bool noValue, string description, bool isRequired, bool allowEmptyValue) : 
        base(name, description, isRequired, allowEmptyValue)
    {
    }

    public override ArgumentDataType DataType { get => ArgumentDataType.String; }
    protected override string GetDefaultValue()
    {
        return string.Empty;
    }
}
