using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

public class DefaultProgramFixture
{
    [Fact]
    public async Task GetUsages_UsesConfigurationFile_False()
    {
        // arrange
        var options = new DefaultProgramOptions();
        options.ApplicationName = "My App";
        options.Version = "1.0.0";
        options.Website = "https://www.benday.com";
        options.UsesConfiguration = false;

        var outputProvider = new StringBuilderTextOutputProvider();

        options.OutputProvider = outputProvider;

        var sut =
            new DefaultProgram(options, typeof(SampleAsyncCommand).Assembly);

        await // act

        sut.RunAsync(new string[] { }, TestContext.Current.CancellationToken);

        // assert
        var output = outputProvider.GetOutput();

        var commandNames = new string[]
        {
            CommandFrameworkConstants.CommandName_GetConfig,
            CommandFrameworkConstants.CommandName_SetConfig,
            CommandFrameworkConstants.CommandName_RemoveConfig
        };

        Assert.Contains("defaultvaluescommand", output);

        foreach (var commandName in commandNames)
        {
            Assert.DoesNotContain(commandName, output);
        }
    }

    [Fact]
    public async Task GetUsages_UsesConfigurationFile_True()
    {
        // arrange
        var options = new DefaultProgramOptions();
        options.ApplicationName = "My App";
        options.Version = "1.0.0";
        options.Website = "https://www.benday.com";
        options.UsesConfiguration = true;

        var outputProvider = new StringBuilderTextOutputProvider();

        options.OutputProvider = outputProvider;

        var sut =
            new DefaultProgram(options, typeof(SampleAsyncCommand).Assembly);

        await // act

        sut.RunAsync(new string[] { }, TestContext.Current.CancellationToken);

        // assert
        var output = outputProvider.GetOutput();

        Console.WriteLine(output);

        var commandNames = new string[]
        {
            CommandFrameworkConstants.CommandName_GetConfig,
            CommandFrameworkConstants.CommandName_SetConfig,
            CommandFrameworkConstants.CommandName_RemoveConfig
        };

        Assert.Contains("defaultvaluescommand", output);

        foreach (var commandName in commandNames)
        {
            Assert.Contains(commandName, output);
        }
    }

    [Fact]
    public async Task GetHelpStringForDefaultCommmand_UsesConfigurationFile_True()
    {
        // arrange
        var options = new DefaultProgramOptions();
        options.ApplicationName = "My App";
        options.Version = "1.0.0";
        options.Website = "https://www.benday.com";
        options.UsesConfiguration = true;

        var outputProvider = new StringBuilderTextOutputProvider();

        options.OutputProvider = outputProvider;

        var sut =
            new DefaultProgram(options, typeof(SampleAsyncCommand).Assembly);

        await // act

        sut.RunAsync(new string[] {
            CommandFrameworkConstants.CommandName_GetConfig,
            ArgumentFrameworkConstants.ArgumentHelpString }, TestContext.Current.CancellationToken);

        // assert
        var output = outputProvider.GetOutput();

        Console.WriteLine(output);

        Assert.DoesNotContain("Invalid command name", output);
    }

    [Fact]
    public async Task GetHelpStringForDefaultCommmand_UsesConfigurationFile_False()
    {
        // arrange
        var options = new DefaultProgramOptions();
        options.ApplicationName = "My App";
        options.Version = "1.0.0";
        options.Website = "https://www.benday.com";
        options.UsesConfiguration = false;

        var outputProvider = new StringBuilderTextOutputProvider();

        options.OutputProvider = outputProvider;

        var sut =
            new DefaultProgram(options, typeof(SampleAsyncCommand).Assembly);

        await // act

        sut.RunAsync(new string[] {
            CommandFrameworkConstants.CommandName_GetConfig,
            ArgumentFrameworkConstants.ArgumentHelpString }, TestContext.Current.CancellationToken);

        // assert
        var output = outputProvider.GetOutput();
        
        Console.WriteLine(output);

        Assert.Contains("Invalid command name", output);
    }

    [Fact]
    public async Task GetJsonForDefaultProgram()
    {
        // arrange
        var options = new DefaultProgramOptions();
        options.ApplicationName = "My App";
        options.Version = "1.0.0";
        options.Website = "https://www.benday.com";
        options.UsesConfiguration = false;

        var outputProvider = new StringBuilderTextOutputProvider();

        options.OutputProvider = outputProvider;

        var sut =
            new DefaultProgram(options, typeof(SampleAsyncCommand).Assembly);

        await // act

        sut.RunAsync(new string[] {
            ArgumentFrameworkConstants.ArgumentJson}, TestContext.Current.CancellationToken);

        // assert
        var output = outputProvider.GetOutput();

        Console.WriteLine(output);

        Assert.False(string.IsNullOrWhiteSpace(output));

        Assert.Contains("\"FriendlyName\": \"Thing Date\"", output);
    }
}
