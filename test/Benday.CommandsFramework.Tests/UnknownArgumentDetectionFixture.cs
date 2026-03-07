using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

public class UnknownArgumentDetectionFixture
{
    private StringBuilderTextOutputProvider? _OutputProvider;

    private StringBuilderTextOutputProvider OutputProvider
    {
        get
        {
            if (_OutputProvider == null)
            {
                _OutputProvider = new StringBuilderTextOutputProvider();
            }

            return _OutputProvider;
        }
    }

    // ---- ArgumentCollection.SetValues() unit tests ----

    [Fact]
    public void SetValues_UnknownKey_IsAddedToUnrecognizedKeys()
    {
        // arrange
        var collection = new ArgumentCollection();
        collection.AddString("name").AsRequired();

        var supplied = new Dictionary<string, string>
        {
            ["name"] = "Bob",
            ["verbose"] = "true"
        };

        // act
        collection.SetValues(supplied);

        // assert
        Assert.Contains("verbose", collection.UnrecognizedKeys);
    }

    [Fact]
    public void SetValues_AllKnownKeys_UnrecognizedKeysIsEmpty()
    {
        // arrange
        var collection = new ArgumentCollection();
        collection.AddString("name").AsRequired();

        var supplied = new Dictionary<string, string>
        {
            ["name"] = "Bob"
        };

        // act
        collection.SetValues(supplied);

        // assert
        Assert.Empty(collection.UnrecognizedKeys);
    }

    [Fact]
    public void SetValues_AliasedKey_IsNotUnrecognized()
    {
        // arrange
        var collection = new ArgumentCollection();
        collection.AddString("name").AsRequired().WithAlias("n");

        var supplied = new Dictionary<string, string>
        {
            ["n"] = "Bob"
        };

        // act
        collection.SetValues(supplied);

        // assert
        Assert.Empty(collection.UnrecognizedKeys);
    }

    [Fact]
    public void SetValues_FrameworkReservedKey_Quiet_IsNotUnrecognized()
    {
        // arrange
        var collection = new ArgumentCollection();
        collection.AddString("name").AsRequired();

        var supplied = new Dictionary<string, string>
        {
            ["name"] = "Bob",
            [CommandFrameworkConstants.CommandArgName_QuietMode] = "true"
        };

        // act
        collection.SetValues(supplied);

        // assert
        Assert.Empty(collection.UnrecognizedKeys);
    }

    [Fact]
    public void SetValues_MultipleUnknownKeys_AllAddedToUnrecognizedKeys()
    {
        // arrange
        var collection = new ArgumentCollection();
        collection.AddString("name").AsRequired();

        var supplied = new Dictionary<string, string>
        {
            ["name"] = "Bob",
            ["verbose"] = "true",
            ["debug"] = "true"
        };

        // act
        collection.SetValues(supplied);

        // assert
        Assert.Contains("verbose", collection.UnrecognizedKeys);
        Assert.Contains("debug", collection.UnrecognizedKeys);
        Assert.Equal(2, collection.UnrecognizedKeys.Count);
    }

    // ---- Command-level integration tests ----

    [Fact]
    public void Command_UnknownArg_ShowsUnknownArgumentError()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            "command1",
            "/arg1:Hello",
            "/isawesome:true",
            "/count:4321",
            "/dateofthingy:12/25/2022",
            "/verbose",
            "/typo:oops"
        );

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);
        executionInfo.Options.StrictArgumentValidation = true;
        var command = new SampleCommand1(executionInfo, OutputProvider);

        // act
        command.Execute();

        // assert
        var output = OutputProvider.GetOutput();
        Assert.DoesNotContain("** SUCCESS **", output);
        Assert.Contains("** INVALID ARGUMENT **", output);
        Assert.Contains("Unknown argument: typo", output);
    }

    [Fact]
    public void Command_NoUnknownArgs_Succeeds()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            "command1",
            "/arg1:Hello",
            "/isawesome:true",
            "/count:4321",
            "/dateofthingy:12/25/2022",
            "/verbose"
        );

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);
        var command = new SampleCommand1(executionInfo, OutputProvider);

        // act
        command.Execute();

        // assert
        var output = OutputProvider.GetOutput();
        Assert.Contains("** SUCCESS **", output);
        Assert.DoesNotContain("Unknown argument", output);
    }

    [Fact]
    public void Command_MultipleUnknownArgs_ShowsAllInOutput()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            "command1",
            "/arg1:Hello",
            "/isawesome:true",
            "/count:4321",
            "/dateofthingy:12/25/2022",
            "/foo:bar",
            "/baz:qux"
        );

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);
        executionInfo.Options.StrictArgumentValidation = true;
        var command = new SampleCommand1(executionInfo, OutputProvider);

        // act
        command.Execute();

        // assert
        var output = OutputProvider.GetOutput();
        Assert.DoesNotContain("** SUCCESS **", output);
        Assert.Contains("** INVALID ARGUMENTS **", output);
        Assert.Contains("Unknown argument: foo", output);
        Assert.Contains("Unknown argument: baz", output);
    }
}
