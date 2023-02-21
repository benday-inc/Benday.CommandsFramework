namespace Benday.CommandsFramework;

public class BooleanArgument : Argument<bool>, IBooleanArgument
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

    public bool ValueAsBoolean => Value;

    protected override bool GetDefaultValue()
    {
        if (AllowEmptyValue)
        {
            // if the value isn't set, always return false
            return false;
        }
        else
        {
            return true;
        }
    }

    public override bool TrySetValue(string input)
    {
        if (input == null)
        {
            return false;
        }
        else
        {
            if (AllowEmptyValue && string.IsNullOrEmpty(input))
            {
                Value = true;
                return true;
            }
            else
            {
                if (bool.TryParse(input, out var temp) == false)
                {
                    return false;
                }
                else
                {
                    Value = temp;
                    return true;
                }
            }            
        }        
    }
}
