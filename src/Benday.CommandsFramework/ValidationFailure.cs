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
    RuleViolated,

    /// <summary>
    /// A required value that can come from stored configuration was neither supplied on the
    /// command line nor found in the configuration.
    /// </summary>
    MissingConfiguration,

    /// <summary>
    /// A value that can be found by searching was not supplied, and the search did not turn
    /// up exactly one match.
    /// </summary>
    DiscoveryFailed
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

    /// <summary>
    /// A required argument that reads from stored configuration has no value from either
    /// place.
    /// </summary>
    /// <remarks>
    /// This exists so the message can say what to do about it. Without it, a missing
    /// configuration value shows up either as a generic "not valid or missing" or -- worse,
    /// and this is what actually happened in practice -- as an exception thrown from a lazy
    /// configuration getter part way through the command, after validation had already
    /// passed and the command had started doing work.
    /// </remarks>
    /// <param name="argument">The argument</param>
    /// <param name="setConfigurationCommandName">Name of the command that sets configuration
    /// values, so the message can name it</param>
    public static ValidationFailure ForMissingConfiguration(
        IArgument argument, string setConfigurationCommandName)
    {
        ArgumentNullException.ThrowIfNull(argument, nameof(argument));

        return new ValidationFailure(
            ValidationFailureKind.MissingConfiguration,
            $"{argument.Name} is required. Supply it with /{argument.Name}:value, or store it " +
            $"once with: {setConfigurationCommandName} " +
            $"/{CommandFrameworkConstants.CommandArgName_ConfigName}:{argument.Name} " +
            $"/{CommandFrameworkConstants.CommandArgName_ConfigValue}:value",
            [argument.Name],
            argument);
    }

    /// <summary>
    /// A value that can be found by searching was not supplied, and the search did not find
    /// exactly one match.
    /// </summary>
    /// <remarks>
    /// Finding nothing and finding several are different situations and get different
    /// messages -- "I could not find one" and "I found four, pick one" call for different
    /// things from the user, and telling them apart is most of what this feature is for.
    /// </remarks>
    /// <param name="argument">The argument</param>
    /// <param name="message">What the search found, or did not</param>
    public static ValidationFailure ForDiscovery(IArgument argument, string message)
    {
        ArgumentNullException.ThrowIfNull(argument, nameof(argument));

        return new ValidationFailure(
            ValidationFailureKind.DiscoveryFailed,
            message,
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
