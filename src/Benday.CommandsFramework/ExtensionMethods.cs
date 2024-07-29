using Microsoft.VisualBasic;

namespace Benday.CommandsFramework;

/// <summary>
/// Extension methods. Mostly methods for configuring command argument
/// definitions using fluent config methods.
/// </summary>
public static class ExtensionMethods
{
    /// <summary>
    /// Adds a string argument definition
    /// </summary>
    /// <param name="collection">Argument collection</param>
    /// <param name="argumentName">Argument name on the command line</param>
    /// <returns></returns>
    public static StringArgument AddString(this ArgumentCollection collection, string argumentName)
    {
        var arg = new StringArgument(argumentName, true, false);

        collection.Add(argumentName, arg);

        return arg;
    }

    /// <summary>
    /// Adds a datetime argument definition
    /// </summary>
    /// <param name="collection">Argument collection</param>
    /// <param name="argumentName">Argument name on the command line</param>
    public static DateTimeArgument AddDateTime(this ArgumentCollection collection,
        string argumentName)
    {        
        var arg = new DateTimeArgument(argumentName, true, false);

        collection.Add(argumentName, arg);

        return arg;
    }

    /// <summary>
    /// Adds a boolean argument definition
    /// </summary>
    /// <param name="collection">Argument collection</param>
    /// <param name="argumentName">Argument name on the command line</param>
    public static BooleanArgument AddBoolean(this ArgumentCollection collection, string argumentName)
    {
        var arg = new BooleanArgument(argumentName, true, false);

        collection.Add(argumentName, arg);

        return arg;
    }

    /// <summary>
    /// Adds a int32 argument definition
    /// </summary>
    /// <param name="collection">Argument collection</param>
    /// <param name="argumentName">Argument name on the command line</param>
    public static Int32Argument AddInt32(this ArgumentCollection collection, string argumentName)
    {
        var arg = new Int32Argument(argumentName, true, false);

        collection.Add(argumentName, arg);

        return arg;
    }

    /// <summary>
    /// Adds a description to an argument definition
    /// </summary>
    /// <param name="collection">Argument collection</param>
    /// <param name="description">Human readable description for the argument</param>
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

    /// <summary>
    /// Adds a user friendly name to an argument definition
    /// </summary>
    /// <param name="collection">Argument collection</param>
    /// <param name="friendlyName">Human readable name for the argument</param>
    public static Argument<T> WithFriendlyName<T>(
        this Argument<T> arg, string friendlyName)
    {
        if (arg == null)
        {
            throw new ArgumentNullException(nameof(arg));
        }
        else
        {
            arg.FriendlyName = friendlyName;
            return arg;
        }
    }

    /// <summary>
    /// Adds a alias to an argument definition
    /// </summary>
    /// <param name="collection">Argument collection</param>
    /// <param name="alias">Alternate name for the argument</param>
    public static Argument<T> WithAlias<T>(
        this Argument<T> arg, string alias)
    {
        if (arg == null)
        {
            throw new ArgumentNullException(nameof(arg));
        }
        else
        {
            arg.Alias = alias;
            return arg;
        }
    }

    /// <summary>
    /// Gets the value from an unnamed variable based on position in the arg string.
    /// </summary>
    /// <param name="collection">Argument collection</param>
    /// <param name="position">Position in the arg string</param>
    public static Argument<T> FromPositionalArgument<T>(
        this Argument<T> arg, int position)
    {
        if (arg == null)
        {
            throw new ArgumentNullException(nameof(arg));
        }
        else if (position < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(position));            
        }
        else
        {
            arg.Alias = $"POSITION_{position}";
            arg.IsPositionalSource = true;

            return arg;
        }
    }

    /// <summary>
    /// Adds a default value for the argument definition
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="arg">Argument definition to configure</param>
    /// <param name="defaultValue">Default value</param>
    /// <returns>The argument</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static Argument<T> WithDefaultValue<T>(
        this Argument<T> arg, T defaultValue)
    {
        if (arg == null)
        {
            throw new ArgumentNullException(nameof(arg));
        }
        else if (defaultValue == null)
        {
            throw new ArgumentNullException(nameof(arg));
        }
        else
        {
            arg.TrySetValue(defaultValue.ToString());

            return arg;
        }
    }

    /// <summary>
    /// Configures an argument definition to allow empty values
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="arg">Argument definition to configure</param>
    /// <param name="allowEmptyValue">Allow or disallow empty values</param>
    /// <returns>The argument</returns>
    /// <exception cref="ArgumentNullException"></exception>
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

    /// <summary>
    /// Configures an argument definition as required
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="arg">Argument definition to configure</param>
    /// <returns>The argument</returns>
    /// <exception cref="ArgumentNullException"></exception>
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

    /// <summary>
    /// Configures an argument definition as not required
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="arg">Argument definition to configure</param>
    /// <returns>The argument</returns>
    /// <exception cref="ArgumentNullException"></exception>
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

    /// <summary>
    /// Adds an argument value to the arguments in an
    /// execution info object
    /// </summary>
    /// <param name="execInfo">The execution information for the command</param>
    /// <param name="argName">Argument name</param>
    /// <param name="argValue">Argument value</param>
    /// <exception cref="ArgumentNullException"></exception>
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

    /// <summary>
    /// Remove an argument value from the command execution info
    /// </summary>
    /// <param name="execInfo">The execution information for the command</param>
    /// <param name="argName">Argument name to remove</param>
    /// <exception cref="ArgumentNullException"></exception>
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

    /// <summary>
    /// Get an argument value as DateTime
    /// </summary>
    /// <param name="args">Argument collection</param>
    /// <param name="argumentName">Argument name</param>
    /// <returns>The value as DateTime</returns>
    /// <exception cref="InvalidOperationException"></exception>
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

    /// <summary>
    /// Get an argument value as int32
    /// </summary>
    /// <param name="args">Argument collection</param>
    /// <param name="argumentName">Argument name</param>
    /// <returns>The value as int</returns>
    /// <exception cref="InvalidOperationException"></exception>
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

    /// <summary>
    /// Does this argument collection have a value for an argument
    /// </summary>
    /// <param name="args">Argument collection</param>
    /// <param name="argumentName">Argument name</param>
    /// <returns>True if there's a value</returns>
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

    /// <summary>
    /// Get an argument value as string
    /// </summary>
    /// <param name="args">Argument collection</param>
    /// <param name="argumentName">Argument name</param>
    /// <returns>The value as string</returns>
    /// <exception cref="InvalidOperationException"></exception>
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

    /// <summary>
    /// Get an argument value as boolean
    /// </summary>
    /// <param name="args">Argument collection</param>
    /// <param name="argumentName">Argument name</param>
    /// <returns>The value as boolean</returns>
    /// <exception cref="InvalidOperationException"></exception>
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

    public static CommandExecutionInfo GetCloneOfArguments(
        this CommandExecutionInfo execInfo, string commandName, bool quietMode)
    {
        if (execInfo is null || execInfo.Arguments is null)
        {
            throw new ArgumentNullException(nameof(execInfo));
        }

        var argsClone = execInfo.Arguments.ToDictionary(entry => entry.Key, entry => entry.Value);

        if (quietMode == true)
        {
            argsClone.TryAdd(CommandFrameworkConstants.CommandArgName_QuietMode, "true");
        }

        var returnValue = new CommandExecutionInfo();
        returnValue.Arguments = argsClone;
        returnValue.CommandName = commandName;

        return returnValue;
    }

    public static string GetPathToFile(
        this ArgumentCollection arguments,
        string argumentName, bool mustExist = false)
    {
        var path = arguments.GetStringValue(argumentName);

        return CommandFrameworkUtilities.GetPathToSourceFile(path, mustExist);
    }

    public static string GetPathToDirectory(
        this ArgumentCollection arguments,
        string argumentName, bool mustExist = false)
    {
        var path = arguments.GetStringValue(argumentName);

        return CommandFrameworkUtilities.GetPathToSourceDir(path, mustExist);
    }
}
