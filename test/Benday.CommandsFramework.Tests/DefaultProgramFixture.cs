using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

public class DefaultProgramFixture
{
    [Fact]
    public void GetUsages_UsesConfigurationFile_False()
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

        // act

        sut.Run(new string[] { });

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
    public void GetUsages_UsesConfigurationFile_True()
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

        // act

        sut.Run(new string[] { });

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
    public void GetHelpStringForDefaultCommmand_UsesConfigurationFile_True()
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

        // act

        sut.Run(new string[] {
            CommandFrameworkConstants.CommandName_GetConfig,
            ArgumentFrameworkConstants.ArgumentHelpString });

        // assert
        var output = outputProvider.GetOutput();

        Console.WriteLine(output);

        Assert.DoesNotContain("Invalid command name", output);
    }

    [Fact]
    public void GetHelpStringForDefaultCommmand_UsesConfigurationFile_False()
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

        // act

        sut.Run(new string[] {
            CommandFrameworkConstants.CommandName_GetConfig,
            ArgumentFrameworkConstants.ArgumentHelpString });

        // assert
        var output = outputProvider.GetOutput();
        
        Console.WriteLine(output);

        Assert.Contains("Invalid command name", output);
    }

    [Fact]
    public void GetJsonForDefaultProgram()
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

        // act

        sut.Run(new string[] {
            ArgumentFrameworkConstants.ArgumentJson});

        // assert
        var output = outputProvider.GetOutput();

        Console.WriteLine(output);

        Assert.False(string.IsNullOrWhiteSpace(output));

        Assert.Contains("\"FriendlyName\": \"Thing Date\"", output);
    }
}
