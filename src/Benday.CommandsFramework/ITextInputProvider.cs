namespace Benday.CommandsFramework;

/// <summary>
/// Interface for reading text input for commands. This is the counterpart to
/// ITextOutputProvider -- a command that prompts for input reads it through here rather
/// than calling Console.ReadLine() directly, which is what makes an interactive command
/// testable and what lets a command run somewhere other than a console.
/// </summary>
public interface ITextInputProvider
{
    /// <summary>
    /// Read a line of input.
    /// </summary>
    /// <returns>The line that was read, or null when there is no more input.</returns>
    string? ReadLine();
}
