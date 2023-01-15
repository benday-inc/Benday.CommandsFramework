namespace Benday.CommandsFramework;

public class Int32Argument : Argument<int>, IInt32Argument
{
    public Int32Argument(string name, bool isRequired = true, bool allowEmptyValue = true) :
        base(name, name, isRequired, allowEmptyValue)
    {

    }

    public Int32Argument(string name, int value, bool isRequired = true, bool allowEmptyValue = true) :
        base(name, value, name, isRequired, allowEmptyValue)
    {

    }

    public Int32Argument(string name, int value, string description, bool isRequired, bool allowEmptyValue) :
        base(name, value, description, isRequired, allowEmptyValue)
    {

    }

    public override ArgumentDataType DataType { get => ArgumentDataType.Int32; }

    public int ValueAsInt32 => Value;

    protected override int GetDefaultValue()
    {
        return default(int);
    }

    public override bool TrySetValue(string input)
    {
        if (input == null)
        {
            return false;
        }
        else
        {
            if (int.TryParse(input, out var temp) == false)
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
