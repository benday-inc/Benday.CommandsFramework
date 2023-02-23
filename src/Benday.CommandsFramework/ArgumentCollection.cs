using System.Collections;
using System.Diagnostics;

namespace Benday.CommandsFramework;

public class ArgumentCollection : IEnumerable<IArgument>
{
    private readonly Dictionary<string, IArgument> _Arguments;

    public ArgumentCollection()
    {
        _Arguments = new();
    }

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

    public void Add(string key, string value)
    {
        _Arguments.Add(key, new StringArgument(key, value, key, true, false));
    }

    public void Add(string key, IArgument value)
    {
        if (_Arguments.ContainsKey(key) == true)
        {
            _Arguments.Remove(key);
        }

        _Arguments.Add(key, value);
    }

    public void Remove(string key)
    {
        _Arguments.Remove(key);
    }

    public Dictionary<string, IArgument>.KeyCollection Keys
    {
        get => _Arguments.Keys;
    }

    public bool ContainsKey(string key)
    {
        return _Arguments.ContainsKey(key);
    }

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
            foreach (var key in fromArguments.Keys)
            {
                if (_Arguments.ContainsKey(key) == true)
                {
                    var targetArg = _Arguments[key];

                    targetArg.TrySetValue(fromArguments[key]);
                }
            }
        }
    }

    public IEnumerator<IArgument> GetEnumerator()
    {
        return _Arguments.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IArgument this[string key]
    {
        get { return _Arguments[key]; }
    }

    public int Count
    {
        get { return _Arguments.Count; }
    }
}
