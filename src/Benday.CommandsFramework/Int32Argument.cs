namespace Benday.CommandsFramework;

/// <summary>
/// Argument that has an int value.
/// </summary>
public class Int32Argument : Argument<int>, IInt32Argument
{
    public Int32Argument(string name) :
        base(name)
    {
        
    }

    /// <summary>
    /// DataType for the argument
    /// </summary>
    public override ArgumentDataType DataType { get => ArgumentDataType.Int32; }

    /// <summary>
    /// Value as int
    /// </summary>
    public int ValueAsInt32 => Value;

    /// <summary>
    /// Returns the default value
    /// </summary>
    /// <returns>Default value for the argument</returns>
    protected override int GetDefaultValue()
    {
        return default(int);
    }

    /// <summary>
    /// Try to convert the input value to int and then set it as the value
    /// for the argument
    /// </summary>
    /// <param name="input">Input value as string</param>
    /// <returns>True if the value could be converted and set</returns>
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
