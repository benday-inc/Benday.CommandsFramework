﻿using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

[TestClass]
public class DefaultProgramFixture
{
    [TestMethod]
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

        Assert.IsTrue(output.Contains("defaultvaluescommand"), "Should contain 'defaultvaluescommand'");

        foreach (var commandName in commandNames)
        {
            Assert.IsFalse(output.Contains(commandName), $"Should not contain '{commandName}'");
        }
    }

    [TestMethod]
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

        var commandNames = new string[]
        {
            CommandFrameworkConstants.CommandName_GetConfig,
            CommandFrameworkConstants.CommandName_SetConfig,
            CommandFrameworkConstants.CommandName_RemoveConfig
        };

        Assert.IsTrue(output.Contains("defaultvaluescommand"), "Should contain 'defaultvaluescommand'");

        foreach (var commandName in commandNames)
        {
            Assert.IsTrue(output.Contains(commandName), $"Did not contain '{commandName}'");
        }
    }
}
