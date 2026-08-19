using System.Text;

namespace Benday.CommandsFramework;

/// <summary>
/// Implementation of ITextOutputProvider that wraps a StringBuilder.
/// This is helpful for checking output during unit testing or
/// for implementing a user interface that could call this command
/// </summary>
/// <remarks>
/// The three channels are captured separately as well as together. GetOutput() returns
/// everything in the order it was written, which is what a console shows and what most
/// tests want; GetResultOutput() returns just the payload, so a test can assert on what
/// the command produced without the chatter around it.
/// </remarks>
public class StringBuilderTextOutputProvider : ITextOutputProvider
{
    public StringBuilderTextOutputProvider()
    {
        _Instance = new StringBuilder();
        _Result = new StringBuilder();
        _Status = new StringBuilder();
        _Error = new StringBuilder();
    }

    private StringBuilder _Instance;
    private StringBuilder _Result;
    private StringBuilder _Status;
    private StringBuilder _Error;

    /// <summary>
    /// Write a line of text
    /// </summary>
    /// <param name="line"></param>
    public void WriteLine(string line)
    {
        _Instance.AppendLine(line);
        _Result.AppendLine(line);
    }

    /// <summary>
    /// Get everything that has been written, on all three channels, in the order it was
    /// written.
    /// </summary>
    /// <returns></returns>
    public string GetOutput()
    {
        return _Instance.ToString();
    }

    /// <summary>
    /// Get only what the command produced as its result -- what would have gone to stdout.
    /// </summary>
    public string GetResultOutput()
    {
        return _Result.ToString();
    }

    /// <summary>
    /// Get only the commentary about the work -- what would have gone to stderr via
    /// WriteStatus().
    /// </summary>
    public string GetStatusOutput()
    {
        return _Status.ToString();
    }

    /// <summary>
    /// Get only the error messages -- what would have gone to stderr via WriteError().
    /// </summary>
    public string GetErrorOutput()
    {
        return _Error.ToString();
    }

    /// <summary>
    /// Write a new line
    /// </summary>
    public void WriteLine()
    {
        _Instance.AppendLine();
        _Result.AppendLine();
    }

    public void Write(string message)
    {
        _Instance.Append(message);
        _Result.Append(message);
    }

    /// <summary>
    /// Write a line of commentary about the work.
    /// </summary>
    /// <param name="line">Text to write</param>
    public void WriteStatus(string line)
    {
        _Instance.AppendLine(line);
        _Status.AppendLine(line);
    }

    /// <summary>
    /// Write an error message.
    /// </summary>
    /// <param name="line">Text to write</param>
    public void WriteError(string line)
    {
        _Instance.AppendLine(line);
        _Error.AppendLine(line);
    }
}
