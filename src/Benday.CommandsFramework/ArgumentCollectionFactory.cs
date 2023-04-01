namespace Benday.CommandsFramework;

/// <summary>
/// Factory for creating CommandExecutionInfo instances from command line arguments.
/// This class *should* be named CommandExecutionInfoFactory and will be renamed in a future release.
/// </summary>
public class ArgumentCollectionFactory
{
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

        var returnValue = new CommandExecutionInfo();

        returnValue.CommandName = input[0];

        if (input.Length > 1)
        {
            returnValue.Arguments = GetArgsAsDictionary(input[1..]);
        }

        return returnValue;
    }

    private Dictionary<string, string> GetArgsAsDictionary(string[] args)
    {
        var returnValue = new Dictionary<string, string>();

        foreach (var arg in args)
        {
            if (arg == ArgumentFrameworkConstants.ArgumentHelpString)
            {
                AddToDictionaryAsIs(arg, returnValue);
            }
            else if (string.IsNullOrWhiteSpace(arg) == false &&
                arg.StartsWith("/") == true &&
                arg.Contains(':') == true)
            {
                CleanArgAndAddToDictionary(arg, returnValue);
            }
            else if (string.IsNullOrWhiteSpace(arg) == false &&
                arg.StartsWith("/") == true &&
                arg.Contains(':') == false)
            {
                CleanArgWithoutColonAndAddToDictionary(arg, returnValue);
            }
        }

        return returnValue;
    }

    private static void CleanArgAndAddToDictionary(string arg, Dictionary<string, string> args)
    {
        var argWithoutSlash = arg[1..];

        var locationOfColon = argWithoutSlash.IndexOf(":");

        var argName = argWithoutSlash[..locationOfColon];

        var argValue = argWithoutSlash[(locationOfColon + 1)..].Trim();

        argValue = RemoveLeadingQuote(argValue);
        argValue = RemoveTrailingQuote(argValue);

        if (args.ContainsKey(argName) == false)
        {
            args.Add(argName, argValue);
        }
    }

    private static void AddToDictionaryAsIs(string arg, Dictionary<string, string> args)
    {
        if (args.ContainsKey(arg) == false)
        {
            args.Add(arg, string.Empty);
        }
    }

    private static void CleanArgWithoutColonAndAddToDictionary(string arg, Dictionary<string, string> args)
    {
        var argWithoutSlash = arg[1..].ToLower();

        if (args.ContainsKey(argWithoutSlash) == false)
        {
            args.Add(argWithoutSlash, string.Empty);
        }
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
