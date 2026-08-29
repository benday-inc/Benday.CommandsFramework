namespace Benday.CommandsFramework;

/// <summary>
/// Typed builder for the argument values one command supplies when it runs another.
/// </summary>
/// <remarks>
/// Running a command from a command used to mean populating a raw
/// Dictionary&lt;string, string&gt;, so every caller wrote its own value formatting -- and got
/// it subtly wrong for dates and booleans, because the parser expects the same formats the
/// command line uses.
/// </remarks>
public sealed class CommandArgumentValues
{
    private readonly Dictionary<string, string> _Values =
        new(ArgumentCollection.ArgumentNameComparer);

    /// <summary>
    /// The values as the framework consumes them.
    /// </summary>
    public Dictionary<string, string> Values => _Values;

    /// <summary>
    /// Number of values supplied.
    /// </summary>
    public int Count => _Values.Count;

    /// <summary>
    /// Set a string value.
    /// </summary>
    public CommandArgumentValues Set(string name, string value)
    {
        _Values[name] = value ?? string.Empty;

        return this;
    }

    /// <summary>
    /// Set an integer value.
    /// </summary>
    public CommandArgumentValues Set(string name, int value)
    {
        _Values[name] = value.ToString();

        return this;
    }

    /// <summary>
    /// Set a boolean value.
    /// </summary>
    public CommandArgumentValues Set(string name, bool value)
    {
        _Values[name] = value ? "true" : "false";

        return this;
    }

    /// <summary>
    /// Set a date value, formatted the way the command line parser reads dates.
    /// </summary>
    public CommandArgumentValues Set(string name, DateTime value)
    {
        _Values[name] = value.ToString("O");

        return this;
    }

    /// <summary>
    /// Set a flag style argument -- the equivalent of typing '/name' with no value.
    /// </summary>
    public CommandArgumentValues SetFlag(string name)
    {
        _Values[name] = string.Empty;

        return this;
    }

    /// <summary>
    /// True when a value has been supplied for this name.
    /// </summary>
    public bool Contains(string name) => _Values.ContainsKey(name);
}
