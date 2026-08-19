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

    /// <summary>
    /// The width of the console window, or the default when output is redirected or there is
    /// no console attached -- reading the window width in either case is meaningless at best
    /// and throws at worst.
    /// </summary>
    public int Width
    {
        get
        {
            if (Console.IsOutputRedirected == true)
            {
                return CommandFrameworkConstants.DefaultOutputWidth;
            }

            try
            {
                return Console.WindowWidth;
            }
            catch (IOException)
            {
                return CommandFrameworkConstants.DefaultOutputWidth;
            }
        }
    }
}
