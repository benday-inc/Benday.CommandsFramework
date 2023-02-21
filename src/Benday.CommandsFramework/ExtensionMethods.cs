using Microsoft.VisualBasic;

namespace Benday.CommandsFramework;

public static class ExtensionMethods
{
    public static StringArgument AddString(this ArgumentCollection collection, string argumentName)
    {
        var arg = new StringArgument(argumentName, true, false);

        collection.Add(argumentName, arg);

        return arg;
    }    

    public static DateTimeArgument AddDateTime(this ArgumentCollection collection,
        string argumentName)
    {        
        var arg = new DateTimeArgument(argumentName, true, false);

        collection.Add(argumentName, arg);

        return arg;
    }

    public static BooleanArgument AddBoolean(this ArgumentCollection collection, string argumentName)
    {
        var arg = new BooleanArgument(argumentName, true, false);

        collection.Add(argumentName, arg);

        return arg;
    }

    public static Int32Argument AddInt32(this ArgumentCollection collection, string argumentName)
    {
        var arg = new Int32Argument(argumentName, true, false);

        collection.Add(argumentName, arg);

        return arg;
    }

    public static Argument<T> WithDescription<T>(
        this Argument<T> arg, string description)
    {
        if (arg == null)
        {
            throw new ArgumentNullException(nameof(arg));
        }
        else
        {
            arg.Description = description;
            return arg;
        }
    }

    public static Argument<T> AllowEmptyValue<T>(
        this Argument<T> arg, bool allowEmptyValue = true)
    {
        if (arg == null)
        {
            throw new ArgumentNullException(nameof(arg));
        }
        else
        {
            arg.AllowEmptyValue = allowEmptyValue;
            return arg;
        }
    }

    public static Argument<T> AsRequired<T>(
        this Argument<T> arg)
    {
        if (arg == null)
        {
            throw new ArgumentNullException(nameof(arg));
        }
        else
        {
            arg.IsRequired = true;

            return arg;
        }
    }

    public static Argument<T> AsNotRequired<T>(
        this Argument<T> arg)
    {
        if (arg == null)
        {
            throw new ArgumentNullException(nameof(arg));
        }
        else
        {
            arg.IsRequired = false;

            return arg;
        }
    }

    public static void AddArgumentValue(
        this CommandExecutionInfo execInfo, string argName, string argValue)
    {
        if (execInfo is null || execInfo.Arguments is null)
        {
            throw new ArgumentNullException(nameof(execInfo));
        }

        if (execInfo.Arguments.ContainsKey(argName) == true)
        {
            execInfo.Arguments.Remove(argName);
        }

        execInfo.Arguments.Add(argName, argValue);
    }

    public static void RemoveArgumentValue(
        this CommandExecutionInfo execInfo, string argName)
    {
        if (execInfo is null || execInfo.Arguments is null)
        {
            throw new ArgumentNullException(nameof(execInfo));
        }

        if (execInfo.Arguments.ContainsKey(argName) == true)
        {
            execInfo.Arguments.Remove(argName);
        }
    }

    public static DateTime GetDateTimeValue(
        this ArgumentCollection args, string argumentName)
    {
        if (args.ContainsKey(argumentName) == false)
        {
            return DateTime.MinValue;
        }
        else
        {
            var argAsDateTime = args[argumentName] as DateTimeArgument;

            if (argAsDateTime == null)
            {
                throw new InvalidOperationException($"Cannot get as datetime arg '{argumentName}'.");
            }
            else
            {
                if (argAsDateTime.HasValue == false)
                {
                    return DateTime.MinValue;
                }
                else
                {
                    return argAsDateTime.ValueAsDateTime;
                }
            }
        }
    }

    public static int GetInt32Value(
    this ArgumentCollection args, string argumentName)
    {
        if (args.ContainsKey(argumentName) == false)
        {
            return 0;
        }
        else
        {
            var argAsInt32 = args[argumentName] as Int32Argument;

            if (argAsInt32 == null)
            {
                throw new InvalidOperationException($"Cannot get as int arg '{argumentName}'.");
            }
            else
            {
                if (argAsInt32.HasValue == false)
                {
                    return 0;
                }
                else
                {
                    return argAsInt32.ValueAsInt32;
                }
            }
        }
    }

    public static bool HasValue(
        this ArgumentCollection args, string argumentName)
    {
        if (args.ContainsKey(argumentName) == false)
        {
            return false;
        }
        else
        {
            return args[argumentName].HasValue;
        }
    }

    public static string GetStringValue(
        this ArgumentCollection args, string argumentName)
    {
        if (args.ContainsKey(argumentName) == false)
        {
            return string.Empty;
        }
        else
        {
            return args[argumentName].Value;
        }
    }

    public static bool GetBooleanValue(
        this ArgumentCollection args, string argumentName)
    {
        if (args.ContainsKey(argumentName) == false)
        {
            return false;
        }
        else
        {
            var argAsBool = args[argumentName] as BooleanArgument;

            if (argAsBool == null)
            {
                throw new InvalidOperationException($"Cannot call ArgumentBooleanValue() on non-boolean arg '{argumentName}'.");
            }
            else
            {
                if (argAsBool.HasValue == false)
                {
                    return false;
                }
                else
                {
                    return argAsBool.ValueAsBoolean;
                }
            }
        }
    }
}