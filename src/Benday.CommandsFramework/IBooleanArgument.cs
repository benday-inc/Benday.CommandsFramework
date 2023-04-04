namespace Benday.CommandsFramework;

/// <summary>
/// Interface for accessing a boolean argument value
/// </summary>
public interface IBooleanArgument
{
    /// <summary>
    /// Argument value typed as bool
    /// </summary>
    bool ValueAsBoolean { get; }
}
