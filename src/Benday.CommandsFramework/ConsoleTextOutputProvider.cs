namespace Benday.CommandsFramework;

/// <summary>
/// Implementation of ITextOutputProvider that outputs messages to the system console.
/// </summary>
public class ConsoleTextOutputProvider : ITextOutputProvider
{
    /// <summary>
    /// Write a message to the console
    /// </summary>
    /// <param name="line"></param>
    public void WriteLine(string line)
    {
        Console.WriteLine(line);
    }

    /// <summary>
    /// Write a new line to the console
    /// </summary>
    public void WriteLine()
    {
        Console.WriteLine();
    }
    public void Write(string message)
    {
        Console.Write(message);
    }

    /// <summary>
    /// Write a line of commentary to stderr, so that it survives -- and stays out of -- a
    /// redirect of the command's result.
    /// </summary>
    /// <param name="line">Text to write</param>
    public void WriteStatus(string line)
    {
        Console.Error.WriteLine(line);
    }

    /// <summary>
    /// Write an error message to stderr.
    /// </summary>
    /// <param name="line">Text to write</param>
    public void WriteError(string line)
    {
        Console.Error.WriteLine(line);
    }
}
