namespace Benday.CommandsFramework;

/// <summary>
/// Interface for handling text output from commands
/// </summary>
public interface ITextOutputProvider
{
    /// <summary>
    /// Write a line of text to output
    /// </summary>
    /// <param name="line">Text to write</param>
    void WriteLine(string line);

    /// <summary>
    /// Write a new line to the output
    /// </summary>
    void WriteLine();
}
