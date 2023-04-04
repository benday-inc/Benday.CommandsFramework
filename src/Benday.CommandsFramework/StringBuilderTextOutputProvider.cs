using System.Text;

namespace Benday.CommandsFramework;

/// <summary>
/// Implementation of ITextOutputProvider that wraps a StringBuilder.
/// This is helpful for checking output during unit testing or
/// for implementing a user interface that could call this command
/// </summary>
public class StringBuilderTextOutputProvider : ITextOutputProvider
{
    public StringBuilderTextOutputProvider()
    {
        _Instance = new StringBuilder();
    }

    private StringBuilder _Instance;

    /// <summary>
    /// Write a line of text
    /// </summary>
    /// <param name="line"></param>
    public void WriteLine(string line)
    {
        _Instance.AppendLine(line);
    }

    /// <summary>
    /// Get the output from the string builder
    /// </summary>
    /// <returns></returns>
    public string GetOutput()
    {
        return _Instance.ToString();
    }

    /// <summary>
    /// Write a new line
    /// </summary>
    public void WriteLine()
    {
        _Instance.AppendLine();
    }
}