namespace Benday.CommandsFramework.Tui.Model;

/// <summary>
/// What happened when the interface ran a command.
/// </summary>
/// <remarks>
/// The command's own CommandResult, plus the two things only the interface knows: what the
/// command wrote while it ran, and how long it took. Nothing here is turned into a process
/// exit code -- the interface outlives any one command, which is the entire reason
/// CommandResult exists instead of a command assigning Environment.ExitCode as a side effect.
/// </remarks>
public sealed class TuiRunResult
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="result">What the command reported</param>
    /// <param name="output">What the command wrote</param>
    /// <param name="duration">How long the run took</param>
    /// <param name="exception">The exception that ended the run, when one did</param>
    public TuiRunResult(
        CommandResult result,
        IReadOnlyList<TuiOutputLine> output,
        TimeSpan duration,
        Exception? exception = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        Result = result;
        Output = output ?? [];
        Duration = duration;
        Exception = exception;
    }

    /// <summary>
    /// What the command reported.
    /// </summary>
    public CommandResult Result { get; }

    /// <summary>
    /// Everything the command wrote, on every channel, in write order.
    /// </summary>
    public IReadOnlyList<TuiOutputLine> Output { get; }

    /// <summary>
    /// How long the run took.
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// The exception that ended the run, when the command threw one the framework does not
    /// treat as an ordinary failure. Null otherwise.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// How the run ended.
    /// </summary>
    public CommandExecutionStatus Status => Result.Status;

    /// <summary>
    /// True when the command did what it was asked to do.
    /// </summary>
    public bool IsSuccess => Result.IsSuccess;

    /// <summary>
    /// Human readable explanation, when there is one.
    /// </summary>
    public string Message => Result.Message;

    /// <summary>
    /// Why validation failed. Empty unless it did.
    /// </summary>
    public IReadOnlyList<ValidationFailure> ValidationFailures => Result.ValidationFailures;

    public override string ToString() => Result.ToString();
}
