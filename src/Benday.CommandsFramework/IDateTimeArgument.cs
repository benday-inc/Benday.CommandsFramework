namespace Benday.CommandsFramework;

/// <summary>
/// Interface for accessing a DateTime argument value
/// </summary>
public interface IDateTimeArgument
{
    /// <summary>
    /// Argument value typed as DateTime
    /// </summary>
    DateTime ValueAsDateTime { get; }
}
