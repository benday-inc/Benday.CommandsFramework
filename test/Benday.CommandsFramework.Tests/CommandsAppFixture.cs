using Benday.CommandsFramework.Samples;

using Microsoft.Extensions.Configuration;

namespace Benday.CommandsFramework.Tests;

public class CommandsAppFixture
{
    [Fact]
    public void ConfigureConfiguration_AddsInMemoryValues()
    {
        // arrange
        var args = new[] { "commandname1", "/arg1:Hello", "/isawesome:true", "/count:42", "/dateofthingy:01/01/2025" };

        IConfiguration? capturedConfig = null;

        // act
        CommandsApp
            .Create<SampleCommand1>(args)
            .WithAppInfo("Test App", "https://www.example.com")
            .ConfigureConfiguration(config =>
            {
                config.AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("TestSection:TestKey", "TestValue"),
                    new KeyValuePair<string, string?>("TestSection:AnotherKey", "AnotherValue")
                });
            })
            .ConfigureServices((services, config) =>
            {
                capturedConfig = config;
            })
            .Run();

        // assert
        Assert.NotNull(capturedConfig);
        Assert.Equal("TestValue", capturedConfig["TestSection:TestKey"]);
        Assert.Equal("AnotherValue", capturedConfig["TestSection:AnotherKey"]);
    }

    [Fact]
    public void ConfigureConfiguration_WorksWithoutPriorWithAppSettings()
    {
        // arrange
        var args = new[] { "commandname1", "/arg1:Hello", "/isawesome:true", "/count:42", "/dateofthingy:01/01/2025" };

        IConfiguration? capturedConfig = null;

        // act - calling ConfigureConfiguration without WithAppSettings first
        CommandsApp
            .Create<SampleCommand1>(args)
            .WithAppInfo("Test App", "https://www.example.com")
            .ConfigureConfiguration(config =>
            {
                config.AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("MyKey", "MyValue")
                });
            })
            .ConfigureServices((services, config) =>
            {
                capturedConfig = config;
            })
            .Run();

        // assert
        Assert.NotNull(capturedConfig);
        Assert.Equal("MyValue", capturedConfig["MyKey"]);
    }

    [Fact]
    public void ConfigureConfiguration_CanBeCalledMultipleTimes()
    {
        // arrange
        var args = new[] { "commandname1", "/arg1:Hello", "/isawesome:true", "/count:42", "/dateofthingy:01/01/2025" };

        IConfiguration? capturedConfig = null;

        // act
        CommandsApp
            .Create<SampleCommand1>(args)
            .WithAppInfo("Test App", "https://www.example.com")
            .ConfigureConfiguration(config =>
            {
                config.AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("First:Key", "FirstValue")
                });
            })
            .ConfigureConfiguration(config =>
            {
                config.AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("Second:Key", "SecondValue")
                });
            })
            .ConfigureServices((services, config) =>
            {
                capturedConfig = config;
            })
            .Run();

        // assert
        Assert.NotNull(capturedConfig);
        Assert.Equal("FirstValue", capturedConfig["First:Key"]);
        Assert.Equal("SecondValue", capturedConfig["Second:Key"]);
    }
}
