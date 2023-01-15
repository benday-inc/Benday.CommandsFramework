namespace Benday.CommandsFramework;

public class ArgumentCollectionFactory
{
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
            if (string.IsNullOrWhiteSpace(arg) == false &&
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
