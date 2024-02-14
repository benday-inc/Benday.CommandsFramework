using System;
using System.Linq;

namespace Benday.CommandsFramework;
public interface ICommandConfigurationManager
{
    bool ConfigFileExists();
    string GetValue(string expectedKey);
    bool HasValue(string expectedKey);
    void RemoveValue(string expectedKey);
    void SetValue(string key, string val);    
}
