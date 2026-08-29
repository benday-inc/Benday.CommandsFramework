namespace Benday.CommandsFramework.Tui.Model;

/// <summary>
/// Where a command reads input from while it runs inside the interface.
/// </summary>
/// <remarks>
/// This is what makes an interactive command work inside the interface at all. Because
/// CommandBase.Prompt() and PromptForYesNo() go through the input provider, a command that
/// asks questions runs here with no knowledge that it is inside an interface -- had it called
/// Console.ReadLine() itself it would be fighting the renderer for the terminal.
///
/// The question itself arrives through the output provider, written without a line ending,
/// which is why this holds one: taking that partial line is what turns the command's question
/// into the label on the prompt rather than half a line stranded above it.
/// </remarks>
public sealed class TuiTextInputProvider : ITextInputProvider
{
    private readonly TuiTextOutputProvider? _Output;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="output">The output provider the question was written to, when there is
    /// one</param>
    public TuiTextInputProvider(TuiTextOutputProvider? output = null)
    {
        _Output = output;
    }

    /// <summary>
    /// What actually asks. Set by the layer that has a terminal to ask on; null means there
    /// is nothing to ask with.
    /// </summary>
    /// <remarks>
    /// A delegate rather than a rendering call, so every decision here stays testable: a test
    /// hands it a function that answers, exactly the way QueuedTextInputProvider does for the
    /// console.
    /// </remarks>
    public Func<string, string?>? Reader { get; set; }

    /// <summary>
    /// How many times a command has asked for input.
    /// </summary>
    public int ReadCount { get; private set; }

    /// <summary>
    /// The last question that was asked, which is whatever the command wrote before reading.
    /// </summary>
    public string LastPrompt { get; private set; } = string.Empty;

    /// <summary>
    /// Reads a line of input.
    /// </summary>
    /// <remarks>
    /// Returns null when there is no way to ask, which is what Console.ReadLine() returns at
    /// the end of input. A command that prompts handles that already, because that is what it
    /// sees when its input is a file that has run out.
    /// </remarks>
    /// <returns>The answer, or null</returns>
    public string? ReadLine()
    {
        ReadCount++;

        LastPrompt = _Output?.TakePendingText() ?? string.Empty;

        return Reader is null ? null : Reader(LastPrompt);
    }
}
