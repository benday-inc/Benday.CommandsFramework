namespace Benday.CommandsFramework;

/// <summary>
/// The result of matching command line tokens against the registry: which command was named,
/// what is left over for the argument parser, and any argument values the name itself
/// supplied.
/// </summary>
public sealed class CommandResolution
{
    internal CommandResolution(
        CommandRegistration registration,
        IReadOnlyList<string> remainingTokens,
        IReadOnlyDictionary<string, string> presetArguments,
        string matchedAs)
    {
        Registration = registration;
        RemainingTokens = remainingTokens;
        PresetArguments = presetArguments;
        MatchedAs = matchedAs;
    }

    /// <summary>
    /// The command that was named.
    /// </summary>
    public CommandRegistration Registration { get; }

    /// <summary>
    /// The tokens after the command name. These are what the argument parser sees.
    /// </summary>
    public IReadOnlyList<string> RemainingTokens { get; }

    /// <summary>
    /// Argument values supplied by the alias that was used, if one was. These are applied as
    /// though they had been typed, so anything actually typed on the command line wins.
    /// </summary>
    public IReadOnlyDictionary<string, string> PresetArguments { get; }

    /// <summary>
    /// What was actually typed to reach this command -- the real name, or the alias that was
    /// used. Kept because resolution used to overwrite the typed name in place, which
    /// destroyed the only record of what the user asked for.
    /// </summary>
    public string MatchedAs { get; }

    /// <summary>
    /// True when the command was reached through an alias rather than by its real name.
    /// </summary>
    public bool WasMatchedByAlias =>
        string.Equals(MatchedAs, Registration.PathAsString, StringComparison.OrdinalIgnoreCase) == false;
}
