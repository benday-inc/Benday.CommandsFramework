namespace Benday.CommandsFramework;

/// <summary>
/// Exception class for describing missing arguments
/// </summary>
public class MissingArgumentException : Exception
{
    public MissingArgumentException() { }
    public MissingArgumentException(string message) : base(message) { }
    public MissingArgumentException(string message, Exception innerException) : base(message, innerException) { }

}
