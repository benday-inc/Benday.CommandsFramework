namespace Benday.CommandsFramework;

/// <summary>
/// Factory for creating CommandExecutionInfo instances from command line arguments.
/// This class *should* be named CommandExecutionInfoFactory and will be renamed in a future release.
/// </summary>
public class ArgumentCollectionFactory
{
    /// <summary>
    /// Which argument syntax to parse. Defaults to accepting both the POSIX form and the
    /// deprecated slash form.
    /// </summary>
    public ArgumentSyntax Syntax { get; set; } = ArgumentSyntax.Both;

    /// <summary>
    /// The argument definitions for the command being parsed, when they are known.
    /// </summary>
    /// <remarks>
    /// This is what makes the space separated form -- <c>--name value</c> -- possible.
    /// Nothing in <c>--name value</c> says whether <c>value</c> belongs to <c>--name</c> or is
    /// a positional argument that follows a boolean flag; only the definition of <c>name</c>
    /// says that. The other forms (<c>--name=value</c>, <c>--name:value</c>,
    /// <c>/name:value</c>) carry their own delimiter and parse without this.
    ///
    /// When it is null -- a caller parsing a fragment before any command is resolved -- an
    /// option consumes the next token whenever that token does not itself look like an option.
    /// </remarks>
    public ArgumentCollection? Definitions { get; set; }

    /// <summary>
    /// Tokens in this parse that used the deprecated slash syntax, in the order they were
    /// typed. The caller decides whether to warn; the parser only records.
    /// </summary>
    public List<string> DeprecatedSlashTokens { get; } = new();

    /// <summary>
    /// Parse raw command line args and return a populated CommandExecutionInfo object.
    /// </summary>
    /// <param name="input">Array of strings. This is typically the raw args from the
    /// command line.</param>
    /// <returns>CommandExecutionInfo for this requested command invocation</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public CommandExecutionInfo Parse(string[] input)
    {
        if (input == null) throw new ArgumentNullException("input");
        if (input.Length == 0) throw new ArgumentOutOfRangeException("input");

        var arguments = input.Length > 1
            ? GetArgsAsDictionary(input[1..], true)
            : null;

        return new CommandExecutionInfo
        {
            Request = new CommandCallRequest(input[0], arguments)
        };
    }

    private int _PositionalArgCount = 0;

    public static int GetSlashCount(string input)
    {
        int count = 0;

        foreach (char c in input)
        {
            if (c == '/')
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// The POSIX end of options marker. Everything after it is a value, even when it starts
    /// with a dash.
    /// </summary>
    private const string EndOfOptionsMarker = "--";

    /// <summary>
    /// Parses argument tokens -- everything after the command name -- into key/value pairs.
    /// </summary>
    /// <remarks>
    /// Public because a multi-level command name is more than one token, so the caller has
    /// to say where the name stops and the arguments start. Positional argument counting is
    /// stateful, so use a fresh factory per parse.
    /// </remarks>
    /// <param name="args">Argument tokens, not including the command name</param>
    /// <param name="processPositionalArguments">Whether bare values become positional args</param>
    /// <returns>The argument values, keyed without regard to case</returns>
    public Dictionary<string, string> GetArgsAsDictionary(string[] args,
        bool processPositionalArguments)
    {
        // case-insensitive so that '--Name a --name b' is recognized as the same argument
        // supplied twice rather than as two different arguments
        var returnValue = new Dictionary<string, string>(ArgumentCollection.ArgumentNameComparer);

        var endOfOptions = false;

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];

            if (string.IsNullOrWhiteSpace(arg) == true)
            {
                continue;
            }

            // everything after '--' is a value, which is how a value that starts with a dash
            // gets typed at all
            if (endOfOptions == true)
            {
                AddPositionalArg(arg, returnValue, processPositionalArguments);

                continue;
            }

            if (arg == EndOfOptionsMarker && Syntax.AllowsPosix() == true)
            {
                endOfOptions = true;

                continue;
            }

            // '--help' keeps its literal key, dashes and all, because that is the key every
            // command checks for. Parsing it as an ordinary POSIX option would file it under
            // 'help' and nothing would find it.
            if (arg == ArgumentFrameworkConstants.ArgumentHelpString)
            {
                AddToDictionaryAsIs(arg, returnValue);

                continue;
            }

            if (Syntax.AllowsPosix() == true && IsPosixOption(arg) == true)
            {
                index = AddPosixOption(arg, args, index, returnValue);

                continue;
            }

            if (Syntax.AllowsSlash() == true && IsSlashOption(arg, processPositionalArguments) == true)
            {
                DeprecatedSlashTokens.Add(arg);

                AddSlashOption(arg, returnValue);

                continue;
            }

            AddPositionalArg(arg, returnValue, processPositionalArguments);
        }

        return returnValue;
    }

    /// <summary>
    /// True when this token is a POSIX option rather than a value.
    /// </summary>
    /// <remarks>
    /// A negative number is a value, not an option -- '--count -5' has to work, and there is
    /// no option named '5'.
    /// </remarks>
    private static bool IsPosixOption(string arg)
    {
        if (arg.StartsWith(EndOfOptionsMarker) == true)
        {
            return arg.Length > EndOfOptionsMarker.Length;
        }

        if (arg.StartsWith('-') == false || arg.Length < 2)
        {
            return false;
        }

        return IsNumeric(arg[1..]) == false;
    }

    private static bool IsNumeric(string value)
    {
        return double.TryParse(
            value, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out _);
    }

    /// <summary>
    /// True when this token is a slash argument rather than a Unix path.
    /// </summary>
    /// <remarks>
    /// A '/'-prefixed token with more than one slash and no colon is a Unix path, which is why
    /// this heuristic exists at all. It only applies when positional arguments are being
    /// collected -- when they are not, there is nothing for a path to be, so the token is a
    /// switch.
    /// </remarks>
    private static bool IsSlashOption(string arg, bool processPositionalArguments)
    {
        if (arg.StartsWith('/') == false)
        {
            return false;
        }

        if (arg.Contains(':') == true)
        {
            return true;
        }

        return GetSlashCount(arg) <= 1 || processPositionalArguments == false;
    }

    /// <summary>
    /// Adds a POSIX option and returns the index of the last token it consumed.
    /// </summary>
    private int AddPosixOption(
        string arg, string[] args, int index, Dictionary<string, string> returnValue)
    {
        // one dash or two: clustering ('-abc' meaning three flags) is not supported, so the
        // remainder of a single dash token is simply the name. That makes '-c' a short option
        // and leaves '-config' working as a lenient spelling of '--config'.
        var withoutPrefix = arg.StartsWith(EndOfOptionsMarker) == true
            ? arg[2..]
            : arg[1..];

        var delimiterIndex = withoutPrefix.IndexOfAny(['=', ':']);

        if (delimiterIndex > 0)
        {
            var name = withoutPrefix[..delimiterIndex];
            var value = withoutPrefix[(delimiterIndex + 1)..].Trim();

            AddToDictionary(name, Unquote(value), returnValue);

            return index;
        }

        var argName = withoutPrefix;

        // no delimiter, so the value is either the next token or nothing at all. Only the
        // argument definition can say which -- a boolean flag never takes one.
        if (TakesAValue(argName) == true &&
            index + 1 < args.Length &&
            IsValueToken(args[index + 1]) == true)
        {
            AddToDictionary(argName, Unquote(args[index + 1].Trim()), returnValue);

            return index + 1;
        }

        AddToDictionary(argName, string.Empty, returnValue);

        return index;
    }

    /// <summary>
    /// Whether an option consumes the token that follows it.
    /// </summary>
    /// <remarks>
    /// A boolean argument that allows an empty value is a flag: '--verbose' is the whole of
    /// it, and the token after it belongs to something else. Everything else takes a value.
    /// An argument this command does not define takes one too, which keeps the behavior the
    /// same as when no definitions were available at all.
    /// </remarks>
    private bool TakesAValue(string argName)
    {
        var argument = FindDefinition(argName);

        if (argument is null)
        {
            return true;
        }

        return argument.DataType != ArgumentDataType.Boolean ||
            argument.AllowEmptyValue == false;
    }

    /// <summary>
    /// Finds an argument definition by name or by alias, the same two ways
    /// ArgumentCollection.SetValues() looks one up.
    /// </summary>
    private IArgument? FindDefinition(string argName)
    {
        if (Definitions is null)
        {
            return null;
        }

        if (Definitions.ContainsKey(argName) == true)
        {
            return Definitions[argName];
        }

        foreach (var argument in Definitions)
        {
            if (argument.HasAlias == true &&
                ArgumentCollection.ArgumentNameComparer.Equals(argument.Alias, argName) == true)
            {
                return argument;
            }
        }

        return null;
    }

    /// <summary>
    /// True when this token could be the value of the option before it.
    /// </summary>
    private bool IsValueToken(string arg)
    {
        if (string.IsNullOrWhiteSpace(arg) == true)
        {
            return true;
        }

        if (arg == EndOfOptionsMarker)
        {
            return false;
        }

        if (Syntax.AllowsPosix() == true && IsPosixOption(arg) == true)
        {
            return false;
        }

        // a slash argument is a switch, but a Unix path is a perfectly good value, and
        // '--file /etc/hosts' has to work
        if (Syntax.AllowsSlash() == true &&
            arg.StartsWith('/') == true &&
            arg.Contains(':') == true &&
            GetSlashCount(arg) <= 1)
        {
            return false;
        }

        return true;
    }

    private static void AddSlashOption(string arg, Dictionary<string, string> args)
    {
        var argWithoutSlash = arg[1..];

        var locationOfColon = argWithoutSlash.IndexOf(":");

        if (locationOfColon < 0)
        {
            // the name is kept exactly as it was typed. Matching against the argument
            // definitions is case-insensitive, so lowercasing here is unnecessary, and doing it
            // used to make a flag argument whose definition had uppercase letters impossible to
            // set from the command line.
            AddToDictionary(argWithoutSlash, string.Empty, args);

            return;
        }

        var argName = argWithoutSlash[..locationOfColon];

        var argValue = argWithoutSlash[(locationOfColon + 1)..].Trim();

        AddToDictionary(argName, Unquote(argValue), args);
    }

    private static void AddToDictionary(
        string name, string value, Dictionary<string, string> args)
    {
        if (string.IsNullOrEmpty(name) == true)
        {
            return;
        }

        if (args.ContainsKey(name) == false)
        {
            args.Add(name, value);
        }
    }

    private static void AddToDictionaryAsIs(string arg, Dictionary<string, string> args)
    {
        if (args.ContainsKey(arg) == false)
        {
            args.Add(arg, string.Empty);
        }
    }

    private void AddPositionalArg(
        string arg, Dictionary<string, string> args, bool processPositionalArguments)
    {
        if (processPositionalArguments == false)
        {
            return;
        }

        var positionalArgNumber = ++_PositionalArgCount;

        var key = $"POSITION_{positionalArgNumber}";

        if (args.ContainsKey(key) == false)
        {
            args.Add(key, arg);
        }
    }

    private static string Unquote(string argValue)
    {
        return RemoveTrailingQuote(RemoveLeadingQuote(argValue));
    }

    private static string RemoveLeadingQuote(string argValue)
    {
        if (argValue.StartsWith("\"") == true)
        {
            return argValue[1..];
        }
        else
        {
            return argValue;
        }
    }

    private static string RemoveTrailingQuote(string argValue)
    {
        if (argValue.EndsWith("\"") == true)
        {
            return argValue[0..^1];
        }
        else
        {
            return argValue;
        }
    }
}
