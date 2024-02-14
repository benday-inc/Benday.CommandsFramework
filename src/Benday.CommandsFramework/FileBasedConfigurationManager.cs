using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Benday.CommandsFramework;
public class FileBasedConfigurationManager
{
    private string _ConfigFilePath;
    private string _ConfigDirPath;

    public FileBasedConfigurationManager(string applicationName)
    {
        ApplicationName = applicationName;
        _ConfigDirPath = GetConfigurationDirectoryPath(applicationName);
        _ConfigFilePath = GetConfigurationFilePath(applicationName);
    }

    public string ApplicationName { get; }

    public static string GetConfigurationFilePath(string applicationName)
    {
        if (string.IsNullOrWhiteSpace(applicationName) == true)
        {
            throw new ArgumentException("Application name cannot be empty.", nameof(applicationName));
        }

        // get path to user profile dir
        var filename = System.IO.Path.Combine(GetConfigurationDirectoryPath(applicationName), "config.json");
        return filename;
    }

    public static string GetConfigurationDirectoryPath(string applicationName)
    {
        if (string.IsNullOrWhiteSpace(applicationName) == true)
        {
            throw new ArgumentException("Application name cannot be empty.", nameof(applicationName));
        }

        // get path to user profile dir
        var userProfileDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var directory = System.IO.Path.Combine(userProfileDir, applicationName);

        return directory;
    }

    public bool ConfigFileExists()
    {
        return File.Exists(_ConfigFilePath);
    }
    public void SetValue(string key, string val)
    {
        EnsureConfigFileExists();

        // if key exists, update it
        if (ConfigurationData.Values.ContainsKey(key) == true)
        {
            ConfigurationData.Values[key] = val;
        }
        else
        {
            // if key does not exist, add it
            ConfigurationData.Values.Add(key, val);
        }

        SaveConfigurationData();
    }

    private ConfigurationData? _ConfigurationData;
    private ConfigurationData ConfigurationData
    {
        get
        {
            if (_ConfigurationData == null)
            {
                _ConfigurationData = LoadConfigurationData();

                return _ConfigurationData;
            }
            else
            {
                return _ConfigurationData;
            }            
        }
    }
    private void SaveConfigurationData()
    {
        EnsureConfigFileExists();

        var options = new JsonSerializerOptions
        {            
            WriteIndented = true
        };

        var json = System.Text.Json.JsonSerializer.Serialize(ConfigurationData, options);

        System.IO.File.WriteAllText(_ConfigFilePath, json);
    }

    private ConfigurationData LoadConfigurationData()
    {
        EnsureConfigFileExists();

        var json = System.IO.File.ReadAllText(_ConfigFilePath);

        if (string.IsNullOrWhiteSpace(json) == true)
        {
            return new ConfigurationData();
        }
        else
        {
            var temp = System.Text.Json.JsonSerializer.Deserialize<ConfigurationData>(json);

            if (temp == null)
            {
                temp = new ConfigurationData();
            }

            return temp;
        }        
    }

    private void EnsureConfigFileExists()
    {
        // if directory does not exist, create it
        if (System.IO.Directory.Exists(_ConfigDirPath) == false)
        {
            System.IO.Directory.CreateDirectory(_ConfigDirPath);
        }

        // if file does not exist, create it
        if (System.IO.File.Exists(_ConfigFilePath) == false)
        {
            System.IO.File.WriteAllText(_ConfigFilePath, string.Empty);
        }
    }

    public string GetValue(string expectedKey)
    {
        if (ConfigurationData.Values.ContainsKey(expectedKey) == false)
        {
            return string.Empty;
        }
        else
        {
            return ConfigurationData.Values[expectedKey];
        }        
    }

    public bool HasValue(string expectedKey)
    {
        if (ConfigurationData.Values.ContainsKey(expectedKey) == false)
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
            ConfigurationData.Values.Remove(expectedKey);
            SaveConfigurationData();
        }
    }
}
