namespace Benday.CommandsFramework;

public class BooleanArgument : Argument<bool>
{
    public BooleanArgument(string name, bool isRequired = true, bool allowEmptyValue = true) :
        base(name, name, isRequired, allowEmptyValue)
    {

    }

    public BooleanArgument(string name, bool value, bool isRequired = true, bool allowEmptyValue = true) :
        base(name, value, name, isRequired, allowEmptyValue)
    {

    }

    public BooleanArgument(string name, bool value, string description, bool isRequired, bool allowEmptyValue) :
        base(name, value, description, isRequired, allowEmptyValue)
    {

    }

    public override ArgumentDataType DataType { get => ArgumentDataType.Boolean; }
    protected override bool GetDefaultValue()
    {
        return true;
    }
}
