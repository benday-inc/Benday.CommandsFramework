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
}