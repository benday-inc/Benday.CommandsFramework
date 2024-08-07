﻿namespace Benday.CommandsFramework;

/// <summary>
/// Argument implementation for working with boolean values
/// </summary>
public class BooleanArgument : Argument<bool>, IBooleanArgument
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
    public BooleanArgument(string name) :
        base(name)
    {

    }

    /// <summary>
    /// Argument data type
    /// </summary>
    public override ArgumentDataType DataType { get => ArgumentDataType.Boolean; }

    /// <summary>
    /// Value as boolean
    /// </summary>
    public bool ValueAsBoolean => Value;

    /// <summary>
    /// Default value for the argument. If this argument is configured to allow empty values,
    /// the default value will be false. Otherwise, the default value is true. 
    /// </summary>
    /// <returns></returns>
    protected override bool GetDefaultValue()
    {
        return false;
    }


    /// <summary>
    /// Try to set a string value to this boolean argument
    /// </summary>
    /// <param name="input">Value as string</param>
    /// <returns>True if the value was successfully converted to bool and set into the value</returns>
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
