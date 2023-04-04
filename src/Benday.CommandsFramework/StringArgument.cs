namespace Benday.CommandsFramework;

/// <summary>
/// Argument implementation for working with string values
/// </summary>
public class StringArgument : Argument<string>
{
    /// <summary>
    /// Constructor. Creates an argument definition without a value. This is typically used
    /// in a command for describing the available arguments.
    /// </summary>
    /// <param name="name">Name of the argument on the command line</param>
    /// <param name="isRequired">Is this argument required</param>
    /// <param name="allowEmptyValue">If true, then you can use this argument as a flag without 
    /// having to explicitly supply a value. This is helpful for scenarios like /debug or /verbose
    /// where it's easier than specifying fully populated arguments like /debug:true or /verbose:true.</param>
    public StringArgument(string name, bool isRequired = true, bool allowEmptyValue = true) :
        base(name, name, isRequired, allowEmptyValue)
    {

    }

    /// <summary>
    /// Constructor. Creates an argument definition along with a value. 
    /// </summary>
    /// <param name="name">Name of the argument on the command line</param>
    /// <param name="value">Value for the argument</param>
    /// <param name="isRequired">Is this argument required</param>
    /// <param name="allowEmptyValue">If true, then you can use this argument as a flag without 
    /// having to explicitly supply a value. This is helpful for scenarios like /debug or /verbose
    /// where it's easier than specifying fully populated arguments like /debug:true or /verbose:true.</param>
    public StringArgument(string name, string value, bool isRequired = true, bool allowEmptyValue = true) :
        base(name, value, name, isRequired, allowEmptyValue)
    {

    }

    /// <summary>
    /// Constructor. Creates an argument definition along with a value and description. 
    /// </summary>
    /// <param name="name">Name of the argument on the command line</param>
    /// <param name="value">Value for the argument</param>
    /// <param name="description">Human friendly description of the argument</param>
    /// <param name="isRequired">Is this argument required</param>
    /// <param name="allowEmptyValue">If true, then you can use this argument as a flag without 
    /// having to explicitly supply a value. This is helpful for scenarios like /debug or /verbose
    /// where it's easier than specifying fully populated arguments like /debug:true or /verbose:true.</param>
    public StringArgument(string name, string value, string description, bool isRequired, bool allowEmptyValue) :
        base(name, value, description, isRequired, allowEmptyValue)
    {

    }

    /// <summary>
    /// Add a string argument definition with a description
    /// </summary>
    /// <param name="name">Argument name</param>
    /// <param name="noValue">Not used. Only here to distinguish this overload.</param>
    /// <param name="description">Human-friendly description of the argument</param>
    /// <param name="isRequired">Is this a required argument?</param>
    /// <param name="allowEmptyValue">Allow empty values</param>
    public StringArgument(string name, bool noValue, string description, bool isRequired, bool allowEmptyValue) :
        base(name, description, isRequired, allowEmptyValue)
    {
    }

    /// <summary>
    /// Data type for the argument
    /// </summary>
    public override ArgumentDataType DataType { get => ArgumentDataType.String; }

    /// <summary>
    /// Get the default value for the argument
    /// </summary>
    /// <returns>Empty string</returns>
    protected override string GetDefaultValue()
    {
        return string.Empty;
    }

    /// <summary>
    /// Validate the argument value against the argument definition
    /// </summary>
    /// <returns>True if the argument is valid</returns>
    public override bool Validate()
    {
        if (IsRequired == false)
        {
            return true;
        }
        else
        {
            var isNullOrWhitespace = string.IsNullOrWhiteSpace(Value as string);

            if (isNullOrWhitespace == true && AllowEmptyValue == true)
            {
                return true;
            }
            else if (isNullOrWhitespace == true && AllowEmptyValue == false)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    /// <summary>
    /// Try to set a string value to this string argument
    /// </summary>
    /// <param name="input">Value as string</param>
    /// <returns>True if the value was successfully converted to string
    /// and set into the value</returns>
    public override bool TrySetValue(string input)
    {
        if (input != null)
        {
            Value = input;
            return true;
        }
        else
        { return false; }
    }
}
