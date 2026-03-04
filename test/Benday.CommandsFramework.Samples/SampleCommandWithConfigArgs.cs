namespace Benday.CommandsFramework.Samples;

[Command(Name = "api-call",
    Description = "Sample command demonstrating FromConfig() arguments",
    Category = "Samples")]
public class SampleCommandWithConfigArgs : SynchronousCommand
{
    public SampleCommandWithConfigArgs(CommandExecutionInfo info, ITextOutputProvider outputProvider)
        : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        // Regular command line argument
        args.AddString("endpoint-path")
            .AsRequired()
            .WithDescription("API endpoint path to call (e.g., /users)");

        // Config-backed arguments
        args.AddString("api-key")
            .FromConfig()
            .AsRequired()
            .WithDescription("Your API key");

        args.AddString("base-url")
            .FromConfig()
            .AsNotRequired()
            .WithDefaultValue("https://api.example.com")
            .WithDescription("API base URL");

        args.AddBoolean("verbose")
            .AsNotRequired()
            .WithDescription("Enable verbose output");

        return args;
    }

    protected override void OnExecute()
    {
        var endpointPath = Arguments.GetStringValue("endpoint-path");
        var apiKey = Arguments.GetStringValue("api-key");
        var baseUrl = Arguments.GetStringValue("base-url");
        var verbose = Arguments.GetBooleanValue("verbose");

        WriteLine($"API Call Configuration:");
        WriteLine($"  Base URL: {baseUrl}");
        WriteLine($"  Endpoint: {endpointPath}");
        WriteLine($"  API Key:  {(verbose ? apiKey : "****" + apiKey.Substring(Math.Max(0, apiKey.Length - 4)))}");
        WriteLine($"  Full URL: {baseUrl}{endpointPath}");
        WriteLine();
        WriteLine("(This is a demo - no actual API call made)");
    }
}
