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
}
