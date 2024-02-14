namespace Benday.CommandsFramework;

public class DefaultProgramOptions : ICommandProgramOptions
{

    public string ApplicationName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public DisplayUsageOptions DisplayUsageOptions { get; set; } = new();
    private string _ConfigurationFolderName = string.Empty;
    public string ConfigurationFolderName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_ConfigurationFolderName) == true)
            {
                return ApplicationName;
            }
            else
            {
                return _ConfigurationFolderName;
            }            
        }
        set => _ConfigurationFolderName = value;
    }
}
