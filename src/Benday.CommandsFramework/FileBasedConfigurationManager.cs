using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            System.IO.File.WriteAllText(_ConfigFilePath, "{}");
        }
    }
}
