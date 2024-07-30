namespace Benday.CommandsFramework;

/// <summary>
/// Argument implementation for working with DateTime values
/// </summary>
public class DateTimeArgument : Argument<DateTime>, IDateTimeArgument
{
    /// <summary>
    /// Constructor. Creates an argument definition without a value. This is
    /// typically used in a command for describing the available arguments.
    /// </summary>
    /// <param name="name">Name of the argument on the command line</param>
    /// <param name="isRequired">Is this argument required</param>
    /// <param name="allowEmptyValue">If true, then you can use this argument as a flag without 
    /// having to explicitly supply a value. This is helpful for scenarios like /debug or /verbose
    /// where it's easier than specifying fully populated arguments like /debug:true or /verbose:true.
    /// </param>
    public DateTimeArgument(string name) :
        base(name)
    {

    }

    /// <summary>
    /// Argument data type
    /// </summary>
    public override ArgumentDataType DataType { get => ArgumentDataType.DateTime; }

    /// <summary>
    /// Value as DateTime
    /// </summary>
    public DateTime ValueAsDateTime => Value;

    /// <summary>
    /// Default value for this argument.
    /// </summary>
    /// <returns>DateTime.MinValue</returns>
    protected override DateTime GetDefaultValue()
    {
        return DateTime.MinValue;
    }


    /// <summary>
    /// Try to set a string value to this DateTime argument
    /// </summary>
    /// <param name="input">Value as string</param>
    /// <returns>True if the value was successfully converted and set into the value</returns>
    public override bool TrySetValue(string input)
    {
        if (input == null)
        {
            return false;
        }
        else
        {
            if (DateTime.TryParse(input, out var temp) == false)
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
