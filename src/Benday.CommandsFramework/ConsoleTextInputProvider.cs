namespace Benday.CommandsFramework;

/// <summary>
/// Implementation of ITextInputProvider that reads from the system console.
/// </summary>
public class ConsoleTextInputProvider : ITextInputProvider
{
    /// <summary>
    /// Read a line from the console.
    /// </summary>
    /// <returns>The line that was read, or null at end of input.</returns>
    public string? ReadLine()
    {
        return Console.ReadLine();
    }
}
