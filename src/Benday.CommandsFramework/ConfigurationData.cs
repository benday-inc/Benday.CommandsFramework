using System;
using System.Linq;

namespace Benday.CommandsFramework;

public class ConfigurationData
{
    public Dictionary<string, string> Values { get; set; } = new();
}
