namespace Benday.CommandsFramework;

/// <summary>
/// This represents a known exception that should end execution of the application.
/// </summary>
public class KnownException : Exception
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="message">Error message</param>
    public KnownException(string message) : base(message) { }

}