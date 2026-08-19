using System.Text.Json.Serialization;

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
    [JsonConverter(typeof(JsonStringEnumConverter))]
    ArgumentDataType DataType { get; }

    /// <summary>
    /// Whether this argument's value is a path and, when it is, what kind of thing the
    /// path points at. Defaults to None, which means the value is not a path.
    /// </summary>
    /// <remarks>
    /// This is a default interface member so that adding it does not break anything that
    /// already implements IArgument. Without it a file argument and a string argument
    /// serialize to byte-identical JSON while validating differently on the same input,
    /// which leaves cmdui and shell completion with no way to tell them apart.
    /// </remarks>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    ArgumentPathType PathType { get => ArgumentPathType.None; }

    /// <summary>
    /// For a file or directory argument, whether the path has to already exist in order
    /// for the value to be valid. Always false when PathType is None.
    /// </summary>
    /// <remarks>
    /// Default interface member, for the same reason as PathType.
    /// </remarks>
    bool MustExist { get => false; }

    /// <summary>
    /// Human readable description for the argument
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Human readable name label for the argument
    /// </summary>
    string FriendlyName { get; }

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
    /// The alternate name of the argument when used on the command line
    /// </summary>
    string Alias { get; set; }

    /// <summary>
    /// Returns true if a command name alias is set
    /// </summary>
    bool HasAlias { get; }
    
    /// <summary>
    /// Should this value come from an unnamed argument on the command line?
    /// If yes, the Alias value will be the POSITION_x.
    /// </summary>
    public bool IsPositionalSource { get; set; }

    /// <summary>
    /// Should this value come from configuration instead of command line?
    /// The value can still be overridden via command line.
    /// </summary>
    public bool IsFromConfig { get; set; }

    /// <summary>
    /// Value for the argument
    /// </summary>
    string Value { get; }

    /// <summary>
    /// The explicit default value for this argument as configured by WithDefaultValue().
    /// This is the string form of the configured default and is never changed by values
    /// supplied on the command line or from configuration. Empty string when no explicit
    /// default has been configured.
    /// </summary>
    string DefaultValue { get; }

    /// <summary>
    /// Returns true when an explicit default value has been configured via
    /// WithDefaultValue(). This is false for the implicit type default
    /// (empty string, false, 0, DateTime.MinValue) that every argument starts with.
    /// </summary>
    bool HasDefaultValue { get; }

    /// <summary>
    /// List of valid values for this argument. Empty array means any value is accepted.
    /// When non-empty, the argument value must match one of these values (case-insensitive).
    /// </summary>
    string[] AllowedValues { get; }

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
