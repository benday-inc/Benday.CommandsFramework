using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

/// <summary>
/// Tests that a missing stored configuration value is a validation concern with an actionable
/// message, rather than something a command discovers part way through its own work.
/// </summary>
public class ConfigurationRequirementFixture
{
    private static (DefaultProgramOptions Options, StringBuilderTextOutputProvider Output)
        GetOptions(InMemoryConfigurationManager configuration)
    {
        var output = new StringBuilderTextOutputProvider();

        var options = new DefaultProgramOptions
        {
            ApplicationName = "Test Sample Application",
            ConfigurationFolderName = "TestSampleApplication-Deleteable",
            OutputProvider = output,
            UsesConfiguration = true
        };

        return (options, output);
    }

    [Fact]
    public async Task MissingConfigurationValue_SaysHowToStoreIt()
    {
        // arrange
        var output = new StringBuilderTextOutputProvider();

        var executionInfo = new ArgumentCollectionFactory().Parse(
            Utilities.GetStringArray("api-call"));

        executionInfo.Configuration =
            new InMemoryConfigurationManager("CommandsFrameworkTests-Deletable");

        using var command = new SampleCommandWithConfigArgs(executionInfo, output);

        // act
        var result = await command.ExecuteAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(CommandExecutionStatus.ValidationFailed, result.Status);

        Assert.Contains(
            result.ValidationFailures,
            x => x.Kind == ValidationFailureKind.MissingConfiguration);

        Assert.Contains("set-configuration /name:api-key", output.GetOutput());
    }

    [Fact]
    public async Task ArgumentSuppliedOnTheCommandLine_DoesNotNeedConfiguration()
    {
        // arrange
        var output = new StringBuilderTextOutputProvider();

        var executionInfo = new ArgumentCollectionFactory().Parse(
            Utilities.GetStringArray(
                "api-call", "/api-key:abc", "/base-url:https://example.com"));

        executionInfo.Configuration =
            new InMemoryConfigurationManager("CommandsFrameworkTests-Deletable");

        using var command = new SampleCommandWithConfigArgs(executionInfo, output);

        // act
        var result = await command.ExecuteAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ArgumentThatIsNotFromConfig_KeepsTheOrdinaryMessage()
    {
        // arrange -- the actionable message would be nonsense for an argument that cannot
        // come from configuration in the first place
        var output = new StringBuilderTextOutputProvider();

        var executionInfo = new ArgumentCollectionFactory().Parse(
            Utilities.GetStringArray(ApplicationConstants.CommandName_Greeting));

        using var command = new SampleGreetingCommand(executionInfo, output);

        // act
        var result = await command.ExecuteAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(CommandExecutionStatus.ValidationFailed, result.Status);

        Assert.All(
            result.ValidationFailures,
            x => Assert.Equal(ValidationFailureKind.InvalidArgument, x.Kind));

        Assert.Contains("name is not valid or missing", output.GetOutput());
        Assert.DoesNotContain("set-configuration", output.GetOutput());
    }

    [Fact]
    public async Task CheckConfiguration_ReportsWhatTheToolNeedsAndWhoNeedsIt()
    {
        // arrange
        var (options, output) = GetOptions(
            new InMemoryConfigurationManager("CommandsFrameworkTests-Deletable"));

        var executionInfo = new ArgumentCollectionFactory().Parse(
            Utilities.GetStringArray(CommandFrameworkConstants.CommandName_CheckConfig));

        executionInfo.Options = options;
        executionInfo.Configuration =
            new InMemoryConfigurationManager("CommandsFrameworkTests-Deletable");

        options.CommandRegistry = CommandRegistry.Build(options, typeof(SampleCommand1).Assembly);

        using var command = new CheckConfigurationCommand(executionInfo, output);

        // act
        await command.ExecuteAsync(TestContext.Current.CancellationToken);

        // assert -- the same declaration that makes validation actionable says what a working
        // setup looks like
        Assert.Contains(command.Requirements, x => x.Name == "api-key" && x.IsRequired == true);
        Assert.Contains(command.Requirements, x => x.Name == "base-url");

        var apiKey = command.Requirements.Single(x => x.Name == "api-key");

        Assert.Contains("api-call", apiKey.CommandNames);
        Assert.False(apiKey.IsSet);
        Assert.False(command.IsComplete);

        Assert.Contains("NOT SET", output.GetOutput());
    }

    [Fact]
    public async Task CheckConfiguration_SaysSoWhenEverythingIsSet()
    {
        // arrange
        var configuration = new InMemoryConfigurationManager("CommandsFrameworkTests-Deletable");

        configuration.SetValue("api-key", "abc");
        configuration.SetValue("base-url", "https://example.com");

        var (options, output) = GetOptions(configuration);

        var executionInfo = new ArgumentCollectionFactory().Parse(
            Utilities.GetStringArray(
                CommandFrameworkConstants.CommandName_CheckConfig,
                $"/{CheckConfigurationCommand.ArgumentName_MissingOnly}"));

        executionInfo.Options = options;
        executionInfo.Configuration = configuration;

        options.CommandRegistry = CommandRegistry.Build(options, typeof(SampleCommand1).Assembly);

        using var command = new CheckConfigurationCommand(executionInfo, output);

        // act
        await command.ExecuteAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.True(command.IsComplete);
        Assert.Contains("Every configuration value this tool reads is set.", output.GetOutput());
    }
}
