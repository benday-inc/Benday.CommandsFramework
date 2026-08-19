using System.Reflection;

namespace Benday.CommandsFramework;

/// <summary>
/// Works out what could come next on a partially typed command line.
/// </summary>
/// <remarks>
/// Dynamic completion rather than a generated static script: the tool itself is the only thing
/// that knows its own commands, and a static script goes stale the moment the tool is updated.
/// It is affordable because this path is deliberately cheap -- completing a command name reads
/// the registry and instantiates nothing, and only once a command is resolved does it create
/// that one command to ask for its arguments. Asking for the whole schema instead would
/// instantiate every command in the tool on every keystroke.
/// </remarks>
public sealed class CompletionEngine
{
    private readonly CommandRegistry _Registry;
    private readonly CommandAttributeUtility _Utility;
    private readonly Assembly _Assembly;

    public CompletionEngine(
        CommandAttributeUtility utility, CommandRegistry registry, Assembly containingAssembly)
    {
        _Utility = utility ?? throw new ArgumentNullException(nameof(utility));
        _Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _Assembly = containingAssembly ?? throw new ArgumentNullException(nameof(containingAssembly));
    }

    /// <summary>
    /// Works out the candidates for a partially typed command line.
    /// </summary>
    /// <param name="commandLine">Everything typed so far, including the tool name. A trailing
    /// space means the user has finished the last word and is starting a new one.</param>
    /// <returns>Candidates and directives</returns>
    public List<CompletionCandidate> GetCandidates(string commandLine)
    {
        var tokens = Tokenize(commandLine ?? string.Empty);

        // the tool name itself is not something to complete
        if (tokens.Count > 0)
        {
            tokens.RemoveAt(0);
        }

        var endsWithSpace =
            string.IsNullOrEmpty(commandLine) == true || commandLine.EndsWith(' ') == true;

        var partial = endsWithSpace == true || tokens.Count == 0
            ? string.Empty
            : tokens[^1];

        // the word being typed is not yet a complete token
        var completed = endsWithSpace == true || tokens.Count == 0
            ? tokens
            : tokens.Take(tokens.Count - 1).ToList();

        var resolution = _Registry.Resolve(completed);

        return resolution is null
            ? GetCommandNameCandidates(partial)
            : GetArgumentCandidates(resolution, partial);
    }

    /// <summary>
    /// Splits a command line into tokens, respecting double quotes.
    /// </summary>
    public static List<string> Tokenize(string commandLine)
    {
        var tokens = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        foreach (var c in commandLine)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (char.IsWhiteSpace(c) == true && inQuotes == false)
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
        {
            tokens.Add(current.ToString());
        }

        return tokens;
    }

    private List<CompletionCandidate> GetCommandNameCandidates(string partial)
    {
        var returnValue = new List<CompletionCandidate>();

        // groups first, so 'wid<TAB>' offers the group rather than nothing
        var groups = _Registry.Registrations
            .Where(x => string.IsNullOrWhiteSpace(x.Group) == false)
            .Select(x => x.Group)
            .Distinct(ArgumentCollection.ArgumentNameComparer)
            .Where(x => Matches(x, partial))
            .Order(ArgumentCollection.ArgumentNameComparer);

        foreach (var group in groups)
        {
            var count = _Registry.Registrations.Count(
                x => ArgumentCollection.ArgumentNameComparer.Equals(x.Group, group));

            returnValue.Add(CompletionCandidate.ForValue(group, $"{count} commands"));
        }

        foreach (var registration in _Registry.Registrations.OrderBy(x => x.PathAsString))
        {
            if (Matches(registration.PathAsString, partial) == true)
            {
                returnValue.Add(CompletionCandidate.ForValue(
                    registration.PathAsString, registration.Description));
            }

            foreach (var alias in registration.Aliases)
            {
                if (Matches(alias.Alias, partial) == true)
                {
                    returnValue.Add(CompletionCandidate.ForValue(
                        alias.Alias, $"alias for {registration.PathAsString}"));
                }
            }
        }

        foreach (var keyword in ReservedKeywords.ForPrograms)
        {
            if (Matches(keyword.Name, partial) == true)
            {
                returnValue.Add(
                    CompletionCandidate.ForValue(keyword.Name, keyword.Description));
            }
        }

        return returnValue;
    }

    private List<CompletionCandidate> GetArgumentCandidates(
        CommandResolution resolution, string partial)
    {
        var returnValue = new List<CompletionCandidate>();

        // only now is a command created, and only this one
        using var command = _Utility.GetCommand(
            [.. resolution.Registration.Path, ArgumentFrameworkConstants.ArgumentHelpString],
            _Assembly);

        var arguments = command.GetArguments();

        // '/name:' means the user is on to the value
        var colonIndex = partial.IndexOf(':');

        if (partial.StartsWith('/') == true && colonIndex > 0)
        {
            var name = partial[1..colonIndex];
            var valueSoFar = partial[(colonIndex + 1)..];

            if (arguments.ContainsKey(name) == true)
            {
                return GetValueCandidates(arguments[name], partial[..(colonIndex + 1)], valueSoFar);
            }

            return returnValue;
        }

        var alreadySupplied = resolution.RemainingTokens
            .Where(x => x.StartsWith('/') == true)
            .Select(x => x.Contains(':') ? x[1..x.IndexOf(':')] : x[1..])
            .ToHashSet(ArgumentCollection.ArgumentNameComparer);

        foreach (var key in arguments.Keys)
        {
            var argument = arguments[key];

            if (argument.IsPositionalSource == true || alreadySupplied.Contains(key) == true)
            {
                continue;
            }

            var candidate = argument.AllowEmptyValue == true &&
                argument.DataType == ArgumentDataType.Boolean
                    ? $"/{argument.Name}"
                    : $"/{argument.Name}:";

            if (Matches(candidate, partial) == true)
            {
                returnValue.Add(
                    CompletionCandidate.ForValue(candidate, argument.Description));
            }
        }

        foreach (var keyword in ReservedKeywords.ForCommands)
        {
            if (Matches(keyword.Name, partial) == true)
            {
                returnValue.Add(
                    CompletionCandidate.ForValue(keyword.Name, keyword.Description));
            }
        }

        return returnValue;
    }

    private static List<CompletionCandidate> GetValueCandidates(
        IArgument argument, string prefix, string valueSoFar)
    {
        var returnValue = new List<CompletionCandidate>();

        // paths go back to the shell, which already knows how to complete them and how to
        // quote what it finds
        if (argument.PathType == ArgumentPathType.File)
        {
            returnValue.Add(CompletionCandidate.ForFiles(
                string.IsNullOrWhiteSpace(argument.DiscoveryPattern)
                    ? "*"
                    : argument.DiscoveryPattern));

            return returnValue;
        }

        if (argument.PathType == ArgumentPathType.Directory)
        {
            returnValue.Add(CompletionCandidate.ForDirectories());

            return returnValue;
        }

        foreach (var allowed in argument.AllowedValues)
        {
            if (allowed.StartsWith(valueSoFar, StringComparison.OrdinalIgnoreCase) == true)
            {
                returnValue.Add(CompletionCandidate.ForValue($"{prefix}{allowed}"));
            }
        }

        if (argument.DataType == ArgumentDataType.Boolean && argument.AllowedValues.Length == 0)
        {
            foreach (var value in new[] { "true", "false" })
            {
                if (value.StartsWith(valueSoFar, StringComparison.OrdinalIgnoreCase) == true)
                {
                    returnValue.Add(CompletionCandidate.ForValue($"{prefix}{value}"));
                }
            }
        }

        return returnValue;
    }

    private static bool Matches(string candidate, string partial)
    {
        return string.IsNullOrEmpty(partial) == true ||
            candidate.StartsWith(partial, StringComparison.OrdinalIgnoreCase) == true;
    }
}
