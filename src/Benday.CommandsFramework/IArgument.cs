namespace Benday.CommandsFramework;

/// <summary>
/// Interface describing the methods and properties for an argument
/// </summary>
public interface IArgument
{
    /// <summary>
    /// Allow or disallow empty values
    /// </summary>
    bool AllowEmptyValue { get; }

    /// <summary>
    /// Data type for the argument
    /// </summary>
    ArgumentDataType DataType { get; }

    /// <summary>
    /// Human readable description for the argument
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Does this argument have a value?
    /// </summary>
    bool HasValue { get; }

    /// <summary>
    /// Is this argument required to have a value?
    /// </summary>
    bool IsRequired { get; }

    /// <summary>
    /// Name of the argument on the command line
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Value for the argument
    /// </summary>
    string Value { get; }

    /// <summary>
    /// Validate the argument value against the argument definition information
    /// </summary>
    /// <returns>True if the value is valid</returns>
    bool Validate();

    /// <summary>
    /// Try to set a value to this argument.
    /// </summary>
    /// <param name="input">Value to set</param>
    /// <returns>True if the value could be converted to the appropriate data
    /// type and was successfully set as the argument value.</returns>
    bool TrySetValue(string input);
}
