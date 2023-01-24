namespace Benday.CommandsFramework;

public class ConsoleTextOutputProvider : ITextOutputProvider
{
    public void WriteLine(string line)
    {
        Console.WriteLine(line);
    }
    public void WriteLine()
    {
        Console.WriteLine();
    }
}
