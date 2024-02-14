using System;
using System.Linq;

namespace Benday.CommandsFramework;
public interface ICommandConfigurationManager
{
    bool ConfigFileExists();
    string GetValue(string key);
    IDictionary<string, string> GetValues();
    bool HasValue(string key);
    void RemoveValue(string key);
    void SetValue(string key, string val);    
}
