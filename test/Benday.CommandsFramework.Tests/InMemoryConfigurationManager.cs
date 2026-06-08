namespace Benday.CommandsFramework.Tests;

public class InMemoryConfigurationManager : ICommandConfigurationManager
{
    private Dictionary<string, string> _Values = new();

    public InMemoryConfigurationManager(string applicationName)
    {
        ApplicationName = applicationName;
    }

    public string ApplicationName { get; }

    public bool ConfigFileExists()
    {
        return true;
    }
    public void SetValue(string key, string val)
    {
        _Values[key] = val;
    }    

    public IDictionary<string, string> GetValues()
    {
        return _Values;
    }

    public string GetValue(string expectedKey)
    {
        if (_Values.ContainsKey(expectedKey) == false)
        {
            return string.Empty;
        }
        else
        {
            return _Values[expectedKey];
        }
    }

    public bool HasValue(string expectedKey)
    {
        if (_Values.ContainsKey(expectedKey) == false)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public void RemoveValue(string expectedKey)
    {
        if (HasValue(expectedKey) == true)
        {
            _Values.Remove(expectedKey);
        }
    }
}
