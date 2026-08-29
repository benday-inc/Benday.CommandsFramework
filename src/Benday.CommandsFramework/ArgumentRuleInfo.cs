namespace Benday.CommandsFramework;

/// <summary>
/// One argument rule, as it travels in the --json schema.
/// </summary>
/// <remarks>
/// A flat shape on purpose: a consumer switches on RuleType and reads the names, without
/// needing the framework's rule classes.
/// </remarks>
public class ArgumentRuleInfo
{
    /// <summary>
    /// Which kind of rule this is: ExactlyOneOf, AtLeastOneOf, MutuallyExclusive,
    /// RequiredTogether or When.
    /// </summary>
    public string RuleType { get; internal set; } = string.Empty;

    /// <summary>
    /// Human readable statement of what the rule requires.
    /// </summary>
    public string Description { get; internal set; } = string.Empty;

    /// <summary>
    /// Every argument the rule is about.
    /// </summary>
    public string[] ArgumentNames { get; internal set; } = [];

    /// <summary>
    /// For a When rule, the argument whose value decides whether it applies.
    /// </summary>
    public string WhenArgumentName { get; internal set; } = string.Empty;

    /// <summary>
    /// For a When rule, the value that makes it apply. Empty means "whenever the argument is
    /// supplied at all".
    /// </summary>
    public string WhenValue { get; internal set; } = string.Empty;

    /// <summary>
    /// For a When rule, the arguments that become required.
    /// </summary>
    public string[] RequiredNames { get; internal set; } = [];

    /// <summary>
    /// For a When rule, the arguments that cannot be used.
    /// </summary>
    public string[] ForbiddenNames { get; internal set; } = [];

    /// <summary>
    /// Builds the schema shape from a rule.
    /// </summary>
    /// <param name="rule">The rule</param>
    /// <returns>The schema shape</returns>
    public static ArgumentRuleInfo FromRule(ArgumentRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule, nameof(rule));

        var returnValue = new ArgumentRuleInfo
        {
            RuleType = rule.RuleType,
            Description = rule.Describe(),
            ArgumentNames = [.. rule.ArgumentNames]
        };

        if (rule is ConditionalRule conditional)
        {
            returnValue.WhenArgumentName = conditional.WhenArgumentName;
            returnValue.WhenValue = conditional.WhenValue ?? string.Empty;
            returnValue.RequiredNames = [.. conditional.RequiredNames];
            returnValue.ForbiddenNames = [.. conditional.ForbiddenNames];
        }

        return returnValue;
    }
}
