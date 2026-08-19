namespace Benday.CommandsFramework;

/// <summary>
/// Implementation of ITextInputProvider that hands out lines from a queue. This is the
/// input side counterpart to StringBuilderTextOutputProvider: queue up the answers a
/// command is going to be asked for, run it, and assert on what it did.
/// </summary>
public class QueuedTextInputProvider : ITextInputProvider
{
    private readonly Queue<string?> _Lines = new();

    /// <summary>
    /// Constructor. Creates a provider with no queued input, which behaves as though the
    /// input stream is already at its end.
    /// </summary>
    public QueuedTextInputProvider()
    {
    }

    /// <summary>
    /// Constructor. Creates a provider preloaded with lines of input.
    /// </summary>
    /// <param name="lines">Lines to return from ReadLine(), in order</param>
    public QueuedTextInputProvider(params string?[] lines)
    {
        AddLines(lines);
    }

    /// <summary>
    /// How many times ReadLine() has been called. Useful for asserting that a command
    /// prompted exactly as many times as it should have.
    /// </summary>
    public int ReadCount { get; private set; }

    /// <summary>
    /// Lines that have been queued and not yet read.
    /// </summary>
    public int RemainingLineCount => _Lines.Count;

    /// <summary>
    /// Queue a line of input.
    /// </summary>
    /// <param name="line">Line to queue</param>
    /// <returns>This provider, so calls can be chained</returns>
    public QueuedTextInputProvider AddLine(string? line)
    {
        _Lines.Enqueue(line);

        return this;
    }

    /// <summary>
    /// Queue several lines of input.
    /// </summary>
    /// <param name="lines">Lines to queue, in order</param>
    /// <returns>This provider, so calls can be chained</returns>
    public QueuedTextInputProvider AddLines(params string?[] lines)
    {
        if (lines is null)
        {
            throw new ArgumentNullException(nameof(lines));
        }

        foreach (var line in lines)
        {
            _Lines.Enqueue(line);
        }

        return this;
    }

    /// <summary>
    /// Read the next queued line.
    /// </summary>
    /// <returns>The next queued line, or null when the queue is empty. Null is what
    /// Console.ReadLine() returns at end of input, so a command that keeps reading past
    /// the queued answers sees the same thing it would see from a closed stdin.</returns>
    public string? ReadLine()
    {
        ReadCount++;

        if (_Lines.Count == 0)
        {
            return null;
        }

        return _Lines.Dequeue();
    }
}
