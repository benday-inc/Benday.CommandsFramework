namespace Benday.CommandsFramework;

public class ArgumentCollection
{
    private readonly Dictionary<string, string> _Arguments;

    public ArgumentCollection()
    {
        _Arguments = new();
    }

    public ArgumentCollection(Dictionary<string, string> fromDictionary)
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
        _Arguments.Add(key, value);
    }

    public void Remove(string key)
    {
        _Arguments.Remove(key);
    }

    public bool ContainsKey(string key)
    {
        return _Arguments.ContainsKey(key);
    }

    public int Count
    {
        get { return _Arguments.Count; }
    }
}