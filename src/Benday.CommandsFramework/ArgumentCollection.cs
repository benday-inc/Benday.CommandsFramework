using System.Collections;
using System.Diagnostics;

namespace Benday.CommandsFramework;

/// <summary>
/// Collection of arguments. Two common use cases: 1) create a 
/// collection of arguments in order to describe what arguments are available
/// for a command and 2) to represent the arguments on a command with the values
/// populated from the command line.
/// </summary>
public class ArgumentCollection : IEnumerable<IArgument>
{
    private readonly Dictionary<string, IArgument> _Arguments;

    /// <summary>
    /// Constructor. Creates an empty argument collection.
    /// </summary>
    public ArgumentCollection()
    {
        _Arguments = new();
    }

    /// <summary>
    /// Constructor. Creates the argument collection and populates it with 
    /// string arguments and values from the supplied dictionary.
    /// </summary>
    /// <param name="fromDictionary">Dictionary of keys and values that will be used to populate this argument collection</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public ArgumentCollection(Dictionary<string, string> fromDictionary)
    {
        if (fromDictionary is null)
        {
            throw new ArgumentNullException(nameof(fromDictionary));
        }

        _Arguments = new();

        foreach (var key in fromDictionary.Keys)
        {
            var value = fromDictionary[key];

            if (value is null)
            {
                throw new InvalidOperationException($"Value for key '{key}' is null.");
            }
            else
            {
                Add(key, value);
            }
        }
    }

    /// <summary>
    /// Constructor. Creates the argument collection and populates it with 
    /// arguments in the supplied dictionary.
    /// </summary>
    /// <param name="fromDictionary">Dictionary of keys and values that will be used to populate this argument collection</param>
    /// <exception cref="InvalidOperationException"></exception>
    public ArgumentCollection(Dictionary<string, IArgument> fromDictionary)
    {
        _Arguments = new();

        foreach (var key in fromDictionary.Keys)
        {
            var value = fromDictionary[key];

            if (value is null)
            {
                throw new InvalidOperationException($"Value for key '{key}' is null.");
            }
            else
            {
                _Arguments.Add(key, value);
            }
        }
    }

    /// <summary>
    /// Add a string argument and value
    /// </summary>
    /// <param name="key">Name of the argument</param>
    /// <param name="value">Value for the argument</param>
    public void Add(string key, string value)
    {
        var arg = new StringArgument(key) { Value = value };

        _Arguments.Add(key, arg);
    }

    /// <summary>
    /// Add an argument to the collection
    /// </summary>
    /// <param name="key">Name of the argument</param>
    /// <param name="value">The populated argument to add</param>
    public void Add(string key, IArgument value)
    {
        if (_Arguments.ContainsKey(key) == true)
        {
            _Arguments.Remove(key);
        }

        _Arguments.Add(key, value);
    }

    /// <summary>
    /// Remove an argument
    /// </summary>
    /// <param name="key">Name of the argument to remove</param>
    public void Remove(string key)
    {
        _Arguments.Remove(key);
    }

    /// <summary>
    /// Collection of argument keys/names managed
    /// </summary>
    public Dictionary<string, IArgument>.KeyCollection Keys
    {
        get => _Arguments.Keys;
    }

    /// <summary>
    /// Does this collection have an argument with this name/key?
    /// </summary>
    /// <param name="key">Name of the argument</param>
    /// <returns>True if it exists in this collection</returns>
    public bool ContainsKey(string key)
    {
        return _Arguments.ContainsKey(key);
    }

    /// <summary>
    /// For an argument collection that already has arguments defined, 
    /// populate those argument definitions with values. This is typically
    /// the arguments from the command line.
    /// </summary>
    /// <param name="fromArguments">Key value pairs to set in to this collection</param>
    /// <exception cref="ArgumentNullException"></exception>
    public void SetValues(Dictionary<string, string> fromArguments)
    {
        if (fromArguments is null)
        {
            throw new ArgumentNullException(nameof(fromArguments));
        }

        if (fromArguments.Count == 0)
        {
            return;
        }
        else
        {
            var aliasedArgs = _Arguments.Values.
                Where(a => a.HasAlias == true).
                ToDictionary(a => a.Alias, a => a);

            foreach (var key in fromArguments.Keys)
            {
                if (_Arguments.ContainsKey(key) == true)
                {
                    var targetArg = _Arguments[key];

                    targetArg.TrySetValue(fromArguments[key]);
                }
                else if (aliasedArgs.ContainsKey(key) == true)
                {
                    var targetArg = aliasedArgs[key];

                    targetArg.TrySetValue(fromArguments[key]);
                }
            }
        }
    }

    /// <summary>
    /// Enumerator
    /// </summary>
    /// <returns>Enumerator for the arguments in this collection</returns>
    public IEnumerator<IArgument> GetEnumerator()
    {
        return _Arguments.Values.GetEnumerator();
    }

    /// <summary>
    /// Enumerator
    /// </summary>
    /// <returns>Enumerator for the arguments in this collection</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Indexer. Get the argument by argument name
    /// </summary>
    /// <param name="key">Name of the argument</param>
    /// <returns>Returns the argument if it exists</returns>
    public IArgument this[string key]
    {
        get { return _Arguments[key]; }
    }

    /// <summary>
    /// Number of arguments in this collection
    /// </summary>
    public int Count
    {
        get { return _Arguments.Count; }
    }
}
