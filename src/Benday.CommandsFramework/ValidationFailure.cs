namespace Benday.CommandsFramework;

/// <summary>
/// What kind of validation failure this is.
/// </summary>
public enum ValidationFailureKind
{
    /// <summary>
    /// A required argument has no value, or the value it has is not valid for its type or
    /// allowed values.
    /// </summary>
    InvalidArgument,

    /// <summary>
    /// A key was supplied on the command line that the command does not define. Only
    /// reported when StrictArgumentValidation is on.
    /// </summary>
    UnknownArgument,

    /// <summary>
    /// A rule about the combination of arguments was broken.
    /// </summary>
    RuleViolated
}

/// <summary>
/// One reason a command's arguments are not valid.
/// </summary>
/// <remarks>
/// Validation used to return a list of IArgument, which meant every failure had to be
/// expressed as an argument -- and the proof that this was too narrow was UnknownArgument, a
/// fake IArgument invented to stand for a failure that is not about an argument the command
/// defines at all. A rule about the combination of arguments has no single argument to blame
/// either.
/// </remarks>
public sealed class ValidationFailure
{
    private ValidationFailure(
        ValidationFailureKind kind,
        string message,
        IReadOnlyList<string> argumentNames,
        IArgument? argument)
    {
        Kind = kind;
        Message = message;
        ArgumentNames = argumentNames;
        Argument = argument;
    }

    /// <summary>
    /// What kind of failure this is.
    /// </summary>
    public ValidationFailureKind Kind { get; }

    /// <summary>
    /// Human readable explanation.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// The arguments this failure is about. One name for an argument level failure, several
    /// for a rule.
    /// </summary>
    public IReadOnlyList<string> ArgumentNames { get; }

    /// <summary>
    /// The argument that failed, when the failure is about one the command defines.
    /// Null for an unknown argument or a broken rule.
    /// </summary>
    public IArgument? Argument { get; }

    public static ValidationFailure ForArgument(IArgument argument)
    {
        ArgumentNullException.ThrowIfNull(argument, nameof(argument));

        return new ValidationFailure(
            ValidationFailureKind.InvalidArgument,
            $"{argument.Name} is not valid or missing",
            [argument.Name],
            argument);
    }

    public static ValidationFailure ForUnknownArgument(string name)
    {
        return new ValidationFailure(
            ValidationFailureKind.UnknownArgument,
            $"Unknown argument: {name}",
            [name],
            null);
    }

    public static ValidationFailure ForRule(ArgumentRule rule, string message)
    {
        ArgumentNullException.ThrowIfNull(rule, nameof(rule));

        return new ValidationFailure(
            ValidationFailureKind.RuleViolated,
            message,
            rule.ArgumentNames,
            null);
    }

    public override string ToString() => Message;
}
