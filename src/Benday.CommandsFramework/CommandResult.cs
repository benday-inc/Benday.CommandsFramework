namespace Benday.CommandsFramework;

/// <summary>
/// What happened when a command ran.
/// </summary>
/// <remarks>
/// Commands used to report failure by assigning Environment.ExitCode as a side effect --
/// Validate() set it, DisplayUsage() set it, and CommandBase had save and restore dances
/// around nested calls to contain the damage. That is workable when the process runs exactly
/// one command and then exits, and actively wrong anywhere else: in a long lived host, one
/// command's failure would decide the exit code of a process that goes on to do many other
/// things. A command returns this instead, and only the outermost console entry point turns
/// it into a process exit code.
/// </remarks>
public sealed class CommandResult
{
    private CommandResult(
        CommandExecutionStatus status,
        string message,
        IReadOnlyList<ValidationFailure> validationFailures)
    {
        Status = status;
        Message = message;
        ValidationFailures = validationFailures;
    }

    /// <summary>
    /// How the run ended.
    /// </summary>
    public CommandExecutionStatus Status { get; }

    /// <summary>
    /// Human readable explanation, when there is one. Empty on success.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Why validation failed. Empty unless Status is ValidationFailed.
    /// </summary>
    public IReadOnlyList<ValidationFailure> ValidationFailures { get; }

    /// <summary>
    /// True when the command did what it was asked to do. Displaying usage counts as
    /// success -- the user asked for the usage information and got it.
    /// </summary>
    public bool IsSuccess =>
        Status == CommandExecutionStatus.Success ||
        Status == CommandExecutionStatus.UsageDisplayed;

    /// <summary>
    /// The process exit code this result corresponds to, for a caller that has to produce
    /// one. Nothing in the framework assigns this to Environment.ExitCode on its own.
    /// </summary>
    public int ExitCode =>
        IsSuccess
            ? CommandFrameworkConstants.ExitCode_Success
            : CommandFrameworkConstants.ExitCode_Failure;

    public static CommandResult Success() =>
        new(CommandExecutionStatus.Success, string.Empty, []);

    public static CommandResult UsageDisplayed() =>
        new(CommandExecutionStatus.UsageDisplayed, string.Empty, []);

    public static CommandResult ValidationFailed(IReadOnlyList<ValidationFailure> failures)
    {
        return new CommandResult(
            CommandExecutionStatus.ValidationFailed,
            string.Join(" ", failures.Select(x => x.Message)),
            failures);
    }

    public static CommandResult Failed(string message) =>
        new(CommandExecutionStatus.Failed, message, []);

    public static CommandResult Cancelled() =>
        new(CommandExecutionStatus.Cancelled, "The command was cancelled.", []);

    public override string ToString() =>
        string.IsNullOrWhiteSpace(Message) ? Status.ToString() : $"{Status}: {Message}";
}
