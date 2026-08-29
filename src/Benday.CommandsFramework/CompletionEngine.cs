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

        if (command is null)
        {
            // the registry resolved it, so this should not happen -- offering nothing is the
            // right answer if it somehow does, since a completion path must never fail
            return returnValue;
        }

        var arguments = command.GetArguments();

        var syntax = _Utility.ProgramOptions.ArgumentSyntax;

        // '--name=', '--name:' and '/name:' all mean the user is on to the value
        var delimiterIndex = IndexOfValueDelimiter(partial, syntax);

        if (delimiterIndex > 0)
        {
            var name = StripPrefix(partial[..delimiterIndex], syntax);
            var valueSoFar = partial[(delimiterIndex + 1)..];

            if (arguments.ContainsKey(name) == true)
            {
                return GetValueCandidates(
                    arguments[name], partial[..(delimiterIndex + 1)], valueSoFar);
            }

            return returnValue;
        }

        // nothing typed yet, and the token before it was an option with no value attached --
        // so what comes next is that option's value, typed the space separated way
        if (string.IsNullOrEmpty(partial) == true)
        {
            var previous = resolution.RemainingTokens.Count == 0
                ? null
                : resolution.RemainingTokens[^1];

            if (previous is not null &&
                IsOptionToken(previous, syntax) == true &&
                IndexOfValueDelimiter(previous, syntax) < 0)
            {
                var name = StripPrefix(previous, syntax);

                if (arguments.ContainsKey(name) == true &&
                    TakesAValue(arguments[name]) == true)
                {
                    return GetValueCandidates(arguments[name], string.Empty, string.Empty);
                }
            }
        }

        var alreadySupplied = GetSuppliedNames(resolution.RemainingTokens, syntax, arguments);

        foreach (var key in arguments.Keys)
        {
            var argument = arguments[key];

            if (argument.IsPositionalSource == true || alreadySupplied.Contains(key) == true)
            {
                continue;
            }

            // In the POSIX syntax the name is a complete token on its own -- the value is the
            // next word, and a second TAB completes it. Offering '--name=' instead would put
            // an '=' in the middle of the word being completed, which bash splits on by
            // default and then completes the wrong half of.
            //
            // The slash syntax has no such separate token, so it keeps its trailing colon.
            var candidate = syntax == ArgumentSyntax.Slash && TakesAValue(argument) == true
                ? $"/{argument.Name}:"
                : syntax.FormatName(argument.Name);

            if (Matches(candidate, partial) == true)
            {
                returnValue.Add(
                    CompletionCandidate.ForValue(candidate, argument.Description));
            }
        }

        foreach (var keyword in ReservedKeywords.ForCommands)
        {
            var name = keyword.GetDisplayName(syntax);

            if (Matches(name, partial) == true)
            {
                returnValue.Add(
                    CompletionCandidate.ForValue(name, keyword.Description));
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

    /// <summary>
    /// Whether an option consumes a following token. A boolean flag that allows an empty
    /// value does not.
    /// </summary>
    private static bool TakesAValue(IArgument argument)
    {
        return argument.DataType != ArgumentDataType.Boolean ||
            argument.AllowEmptyValue == false;
    }

    /// <summary>
    /// True when this token is an option rather than a value, in either syntax.
    /// </summary>
    private static bool IsOptionToken(string token, ArgumentSyntax syntax)
    {
        if (syntax.AllowsPosix() == true &&
            token.StartsWith('-') == true &&
            token.Length > 1 &&
            token != "--")
        {
            return true;
        }

        return syntax.AllowsSlash() == true && token.StartsWith('/') == true;
    }

    /// <summary>
    /// Where the value starts in an option token, or -1 when the token carries no value.
    /// </summary>
    private static int IndexOfValueDelimiter(string token, ArgumentSyntax syntax)
    {
        if (IsOptionToken(token, syntax) == false)
        {
            return -1;
        }

        return token.StartsWith('/') == true
            ? token.IndexOf(':')
            : token.IndexOfAny(['=', ':']);
    }

    /// <summary>
    /// Strips the '--', '-' or '/' from the front of an option token.
    /// </summary>
    private static string StripPrefix(string token, ArgumentSyntax syntax)
    {
        if (token.StartsWith("--") == true)
        {
            return token[2..];
        }

        if (token.StartsWith('-') == true || token.StartsWith('/') == true)
        {
            return token[1..];
        }

        return token;
    }

    /// <summary>
    /// The argument names already on the command line, so they are not offered twice.
    /// </summary>
    /// <remarks>
    /// A token consumed as the value of the option before it is not itself an option, which
    /// is why this walks the tokens rather than filtering them.
    /// </remarks>
    private static HashSet<string> GetSuppliedNames(
        IReadOnlyList<string> tokens, ArgumentSyntax syntax, ArgumentCollection arguments)
    {
        var returnValue = new HashSet<string>(ArgumentCollection.ArgumentNameComparer);

        for (var index = 0; index < tokens.Count; index++)
        {
            var token = tokens[index];

            if (IsOptionToken(token, syntax) == false)
            {
                continue;
            }

            var delimiterIndex = IndexOfValueDelimiter(token, syntax);

            var name = delimiterIndex > 0
                ? StripPrefix(token[..delimiterIndex], syntax)
                : StripPrefix(token, syntax);

            returnValue.Add(name);

            if (delimiterIndex < 0 &&
                arguments.ContainsKey(name) == true &&
                TakesAValue(arguments[name]) == true)
            {
                // the next token is this option's value, not an option of its own
                index++;
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
