namespace Benday.CommandsFramework;

/// <summary>
/// A rule about the combination of argument values, rather than about any one of them.
/// </summary>
/// <remarks>
/// Declarative rather than a callback on purpose. A callback can only answer "is this valid",
/// and only once every value is in; a rule can be described, shipped in the --json schema, and
/// evaluated by a form as it is being filled in.
/// </remarks>
public abstract class ArgumentRule
{
    protected ArgumentRule(IReadOnlyList<string> argumentNames)
    {
        ArgumentNames = argumentNames ?? [];
    }

    /// <summary>
    /// The arguments this rule is about.
    /// </summary>
    public IReadOnlyList<string> ArgumentNames { get; }

    /// <summary>
    /// Short name for the kind of rule, for the schema. Consumers switch on this.
    /// </summary>
    public abstract string RuleType { get; }

    /// <summary>
    /// Human readable statement of what the rule requires, used in usage output and in the
    /// message when the rule is broken.
    /// </summary>
    public abstract string Describe();

    /// <summary>
    /// Checks the rule.
    /// </summary>
    /// <param name="arguments">The command's arguments, with values set</param>
    /// <returns>Null when the rule holds, otherwise why it does not</returns>
    public abstract string? Check(ArgumentCollection arguments);

    /// <summary>
    /// True when the argument has a usable value.
    /// </summary>
    protected static bool HasValue(ArgumentCollection arguments, string name)
    {
        if (arguments.ContainsKey(name) == false)
        {
            return false;
        }

        var argument = arguments[name];

        if (argument.HasValue == false)
        {
            return false;
        }

        // a boolean flag that is present but false has not really been supplied as far as
        // "which of these did you choose" is concerned
        if (argument.DataType == ArgumentDataType.Boolean &&
            argument is IBooleanArgument booleanArgument)
        {
            return booleanArgument.ValueAsBoolean;
        }

        return string.IsNullOrWhiteSpace(argument.Value) == false;
    }

    /// <summary>
    /// The names that have a value, in the order the rule declares them.
    /// </summary>
    protected IReadOnlyList<string> GetSuppliedNames(ArgumentCollection arguments)
    {
        return [.. ArgumentNames.Where(x => HasValue(arguments, x))];
    }

    protected static string Quote(IEnumerable<string> names)
    {
        return string.Join(", ", names.Select(x => $"'{x}'"));
    }
}

/// <summary>
/// Exactly one of these arguments has to be supplied.
/// </summary>
public sealed class ExactlyOneOfRule : ArgumentRule
{
    public ExactlyOneOfRule(IReadOnlyList<string> argumentNames) : base(argumentNames)
    {
    }

    public override string RuleType => "ExactlyOneOf";

    public override string Describe() => $"Supply exactly one of {Quote(ArgumentNames)}.";

    public override string? Check(ArgumentCollection arguments)
    {
        var supplied = GetSuppliedNames(arguments);

        if (supplied.Count == 1)
        {
            return null;
        }

        // zero and several are different mistakes and deserve different messages
        if (supplied.Count == 0)
        {
            return $"One of {Quote(ArgumentNames)} is required.";
        }

        return $"Only one of {Quote(ArgumentNames)} can be supplied, but {Quote(supplied)} were.";
    }
}

/// <summary>
/// At least one of these arguments has to be supplied.
/// </summary>
public sealed class AtLeastOneOfRule : ArgumentRule
{
    public AtLeastOneOfRule(IReadOnlyList<string> argumentNames) : base(argumentNames)
    {
    }

    public override string RuleType => "AtLeastOneOf";

    public override string Describe() => $"Supply at least one of {Quote(ArgumentNames)}.";

    public override string? Check(ArgumentCollection arguments)
    {
        return GetSuppliedNames(arguments).Count > 0
            ? null
            : $"At least one of {Quote(ArgumentNames)} is required.";
    }
}

/// <summary>
/// These arguments cannot be used together, though none of them is required.
/// </summary>
public sealed class MutuallyExclusiveRule : ArgumentRule
{
    public MutuallyExclusiveRule(IReadOnlyList<string> argumentNames) : base(argumentNames)
    {
    }

    public override string RuleType => "MutuallyExclusive";

    public override string Describe() => $"{Quote(ArgumentNames)} cannot be used together.";

    public override string? Check(ArgumentCollection arguments)
    {
        var supplied = GetSuppliedNames(arguments);

        return supplied.Count <= 1
            ? null
            : $"{Quote(supplied)} cannot be used together.";
    }
}

/// <summary>
/// Either all of these arguments are supplied or none of them is.
/// </summary>
public sealed class RequiredTogetherRule : ArgumentRule
{
    public RequiredTogetherRule(IReadOnlyList<string> argumentNames) : base(argumentNames)
    {
    }

    public override string RuleType => "RequiredTogether";

    public override string Describe() =>
        $"{Quote(ArgumentNames)} have to be supplied together.";

    public override string? Check(ArgumentCollection arguments)
    {
        var supplied = GetSuppliedNames(arguments);

        if (supplied.Count == 0 || supplied.Count == ArgumentNames.Count)
        {
            return null;
        }

        var missing = ArgumentNames.Where(x => supplied.Contains(x) == false);

        return $"{Quote(supplied)} requires {Quote(missing)}.";
    }
}

/// <summary>
/// A rule that only applies when another argument has a particular value.
/// </summary>
public sealed class ConditionalRule : ArgumentRule
{
    public ConditionalRule(
        string whenArgumentName,
        string? whenValue,
        IReadOnlyList<string> requiredNames,
        IReadOnlyList<string> forbiddenNames)
        : base([whenArgumentName, .. requiredNames, .. forbiddenNames])
    {
        WhenArgumentName = whenArgumentName;
        WhenValue = whenValue;
        RequiredNames = requiredNames;
        ForbiddenNames = forbiddenNames;
    }

    /// <summary>
    /// The argument whose value decides whether the rule applies.
    /// </summary>
    public string WhenArgumentName { get; }

    /// <summary>
    /// The value that makes the rule apply. Null means "whenever this argument is supplied at
    /// all", whatever its value.
    /// </summary>
    public string? WhenValue { get; }

    /// <summary>
    /// Arguments that are required when the condition holds.
    /// </summary>
    public IReadOnlyList<string> RequiredNames { get; }

    /// <summary>
    /// Arguments that cannot be used when the condition holds.
    /// </summary>
    public IReadOnlyList<string> ForbiddenNames { get; }

    public override string RuleType => "When";

    public override string Describe()
    {
        var condition = WhenValue is null
            ? $"When '{WhenArgumentName}' is supplied"
            : $"When '{WhenArgumentName}' is '{WhenValue}'";

        var parts = new List<string>();

        if (RequiredNames.Count > 0)
        {
            parts.Add($"{Quote(RequiredNames)} {(RequiredNames.Count == 1 ? "is" : "are")} required");
        }

        if (ForbiddenNames.Count > 0)
        {
            parts.Add($"{Quote(ForbiddenNames)} cannot be used");
        }

        return $"{condition}, {string.Join(" and ", parts)}.";
    }

    public override string? Check(ArgumentCollection arguments)
    {
        if (Applies(arguments) == false)
        {
            return null;
        }

        var missing = RequiredNames.Where(x => HasValue(arguments, x) == false).ToList();

        if (missing.Count > 0)
        {
            return $"{ConditionText()}, so {Quote(missing)} " +
                $"{(missing.Count == 1 ? "is" : "are")} required.";
        }

        var forbidden = ForbiddenNames.Where(x => HasValue(arguments, x)).ToList();

        if (forbidden.Count > 0)
        {
            return $"{ConditionText()}, so {Quote(forbidden)} cannot be used.";
        }

        return null;
    }

    private bool Applies(ArgumentCollection arguments)
    {
        if (HasValue(arguments, WhenArgumentName) == false)
        {
            return false;
        }

        if (WhenValue is null)
        {
            return true;
        }

        return string.Equals(
            arguments[WhenArgumentName].Value, WhenValue, StringComparison.OrdinalIgnoreCase);
    }

    private string ConditionText()
    {
        return WhenValue is null
            ? $"'{WhenArgumentName}' was supplied"
            : $"'{WhenArgumentName}' is '{WhenValue}'";
    }
}
