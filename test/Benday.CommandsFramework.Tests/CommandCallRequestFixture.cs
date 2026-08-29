using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

/// <summary>
/// Tests the split between what was asked for and the context it runs in. Alias resolution
/// used to overwrite the command name in place, which destroyed the only record of what the
/// user actually typed.
/// </summary>
public class CommandCallRequestFixture
{
    private static CommandAttributeUtility SystemUnderTest =>
        new(new DefaultProgramOptions
        {
            ApplicationName = "Test Sample Application",
            ConfigurationFolderName = "TestSampleApplication-Deleteable",
            OutputProvider = new StringBuilderTextOutputProvider(),
            UsesConfiguration = false
        });

    [Fact]
    public void Request_KeepsWhatWasTypedWhenAnAliasWasUsed()
    {
        // act
        var command = SystemUnderTest.GetCommand(
            Utilities.GetStringArray("mc", "/message:hello"),
            typeof(SampleCommand1).Assembly);

        // assert
        Assert.NotNull(command);

        var request = command.ExecutionInfo.Request;

        Assert.Equal(
            ApplicationConstants.CommandName_CommandWithCommandNameAliases, request.CommandName);
        Assert.Equal("mc", request.RequestedName);
        Assert.True(request.WasMatchedByAlias);
    }

    [Fact]
    public void Request_RequestedNameIsTheRealNameWhenNoAliasWasUsed()
    {
        // act
        var command = SystemUnderTest.GetCommand(
            Utilities.GetStringArray(ApplicationConstants.CommandName_Command1),
            typeof(SampleCommand1).Assembly);

        // assert
        Assert.NotNull(command);

        var request = command.ExecutionInfo.Request;

        Assert.Equal(request.CommandName, request.RequestedName);
        Assert.False(request.WasMatchedByAlias);
    }

    [Fact]
    public void Request_ArgumentNamesAreMatchedWithoutRegardToCase()
    {
        // arrange
        var request = new CommandCallRequest(
            "somecommand",
            new Dictionary<string, string> { { "Thing", "value" } });

        // assert
        Assert.True(request.Arguments.ContainsKey("thing"));
        Assert.True(request.Arguments.ContainsKey("THING"));
    }

    [Fact]
    public void Request_CopiesTheArgumentsItIsGiven()
    {
        // arrange -- a request is what was asked for; it should not change underneath a
        // caller who reuses the dictionary
        var source = new Dictionary<string, string> { { "thing", "value" } };

        var request = new CommandCallRequest("somecommand", source);

        // act
        source["thing"] = "changed";
        source["other"] = "added";

        // assert
        Assert.Equal("value", request.Arguments["thing"]);
        Assert.False(request.Arguments.ContainsKey("other"));
    }

    [Fact]
    public void ExecutionInfo_ReadsThroughToTheRequest()
    {
        // arrange
        var info = new CommandExecutionInfo
        {
            Request = new CommandCallRequest(
                "somecommand", new Dictionary<string, string> { { "thing", "value" } })
        };

        // assert -- the old flat properties still read, so command code does not have to
        // change all at once
        Assert.Equal("somecommand", info.CommandName);
        Assert.Equal("value", info.Arguments["thing"]);
    }

    [Fact]
    public void ArgumentValues_FormatEachTypeTheWayTheParserReadsIt()
    {
        // arrange
        var values = new CommandArgumentValues();

        // act
        values
            .Set("name", "Ben")
            .Set("count", 42)
            .Set("enabled", true)
            .Set("disabled", false)
            .Set("asof", new DateTime(2026, 8, 19, 13, 45, 0, DateTimeKind.Utc))
            .SetFlag("verbose");

        // assert
        Assert.Equal("Ben", values.Values["name"]);
        Assert.Equal("42", values.Values["count"]);
        Assert.Equal("true", values.Values["enabled"]);
        Assert.Equal("false", values.Values["disabled"]);
        Assert.StartsWith("2026-08-19T13:45:00", values.Values["asof"]);

        // a flag is the equivalent of typing '/verbose' with no value
        Assert.Equal(string.Empty, values.Values["verbose"]);
        Assert.True(values.Contains("verbose"));
        Assert.Equal(6, values.Count);
    }

    [Fact]
    public void ArgumentValues_AreMatchedWithoutRegardToCase()
    {
        // arrange
        var values = new CommandArgumentValues();

        // act
        values.Set("Thing", "first").Set("thing", "second");

        // assert
        Assert.Equal(1, values.Count);
        Assert.Equal("second", values.Values["THING"]);
    }

    [Fact]
    public async Task NestedCall_UsesTypedValues()
    {
        // arrange
        var output = new StringBuilderTextOutputProvider();

        var executionInfo = new ArgumentCollectionFactory().Parse(
            Utilities.GetStringArray(
                ApplicationConstants.CommandName_CallsOtherCommands, "/names:Alice,Bob"));

        using var command = new SampleCommandThatCallsOtherCommands(executionInfo, output);

        // act
        await command.ExecuteAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(2, command.Greetings.Count);
        Assert.Contains(command.Greetings, x => x.Contains("Alice"));
        Assert.Contains(command.Greetings, x => x.Contains("Bob"));
    }
}
