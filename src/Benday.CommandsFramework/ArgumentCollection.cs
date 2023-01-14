namespace Benday.CommandsFramework;

public class ArgumentCollection
{
    private Dictionary<string, object> _Arguments;

    public ArgumentCollection()
    {
        _Arguments = new Dictionary<string, object>();
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