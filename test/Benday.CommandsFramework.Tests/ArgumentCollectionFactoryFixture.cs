using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Benday.CommandsFramework.Tests;

[TestClass]
public class ArgumentCollectionFactoryFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _SystemUnderTest = null;
    }

    private ArgumentCollectionFactory? _SystemUnderTest;

    private ArgumentCollectionFactory SystemUnderTest
    {
        get
        {
            if (_SystemUnderTest == null)
            {
                _SystemUnderTest = new ArgumentCollectionFactory();
            }

            return _SystemUnderTest;
        }
    }

    [TestMethod]
    public void Parse()
    {
        // arrange
        var expectedCommandName = "commandname";
        var expectedKey1 = "arg1";
        var expectedVal1 = "value1";
        var expectedKey2 = "arg2";
        var expectedVal2 = "value with spaces";
        var expectedKey3 = "arg3";
        var expectedVal3 = string.Empty;

        string[] input = { expectedCommandName,
            $"/{expectedKey1}:{expectedVal1}",
            $"/{expectedKey2}:\"{expectedVal2}\"",
            $"/{expectedKey3}"
        };

        // act
        var actual = SystemUnderTest.Parse(input);

        // assert
        Assert.AreEqual<string>(expectedCommandName, actual.CommandName, "Command name was wrong");
        Assert.AreEqual<int>(3, actual.Arguments.Count, "Argument count was wrong");

        Assert.IsTrue(actual.Arguments.ContainsKey(expectedKey1), "key 1 missing");
        Assert.IsTrue(actual.Arguments.ContainsKey(expectedKey2), "key 2 missing");
        Assert.IsTrue(actual.Arguments.ContainsKey(expectedKey3), "key 3 missing");

        Assert.AreEqual<string>(expectedVal1, actual.Arguments[expectedKey1], "Key1 value was wrong");
        Assert.AreEqual<string>(expectedVal2, actual.Arguments[expectedKey2], "Key2 value was wrong");
        Assert.AreEqual<string>(expectedVal3, actual.Arguments[expectedKey3], "Key3 value was wrong");
    }

    [TestMethod]
    public void Parse_Positional_OnePositionalArg()
    {
        // arrange
        var expectedCommandName = "commandname";
        var expectedKey1 = "arg1";
        var expectedVal1 = "value1";
        var expectedKey2 = "arg2";
        var expectedVal2 = "value with spaces";
        var expectedKey3 = "arg3";
        var expectedVal3 = string.Empty;
        var expectedPositionalKey1 = "POSITION_1";
        var expectedPositionalValue1 = "positional1value";

        string[] input = { expectedCommandName,
            $"/{expectedKey1}:{expectedVal1}",
            $"/{expectedKey2}:\"{expectedVal2}\"",
            $"/{expectedKey3}",
            expectedPositionalValue1
        };

        // act
        var actual = SystemUnderTest.Parse(input, true);

        // assert
        Assert.AreEqual<string>(expectedCommandName, actual.CommandName, "Command name was wrong");
        Assert.AreEqual<int>(4, actual.Arguments.Count, "Argument count was wrong");

        Assert.IsTrue(actual.Arguments.ContainsKey(expectedKey1), "key 1 missing");
        Assert.IsTrue(actual.Arguments.ContainsKey(expectedKey2), "key 2 missing");
        Assert.IsTrue(actual.Arguments.ContainsKey(expectedKey3), "key 3 missing");
        Assert.IsTrue(actual.Arguments.ContainsKey(expectedPositionalKey1), 
            $"{expectedPositionalKey1} key is missing");

        Assert.AreEqual<string>(expectedVal1, actual.Arguments[expectedKey1], "Key1 value was wrong");
        Assert.AreEqual<string>(expectedVal2, actual.Arguments[expectedKey2], "Key2 value was wrong");
        Assert.AreEqual<string>(expectedVal3, actual.Arguments[expectedKey3], "Key3 value was wrong");
        Assert.AreEqual<string>(expectedPositionalValue1, 
            actual.Arguments[expectedPositionalKey1], $"{expectedPositionalKey1} value was wrong");
    }

    
    [TestMethod]
    public void Parse_ContainsHelpString()
    {
        // arrange
        var expectedCommandName = "commandname";
        
        var input = Utilities.GetStringArray(
           expectedCommandName,
           ArgumentFrameworkConstants.ArgumentHelpString
           );

        // act
        var actual = SystemUnderTest.Parse(input);

        // assert
        Assert.AreEqual<string>(expectedCommandName, actual.CommandName, "Command name was wrong");
        Assert.AreEqual<int>(1, actual.Arguments.Count, "Argument count was wrong");

        Assert.IsTrue(actual.Arguments.ContainsKey(ArgumentFrameworkConstants.ArgumentHelpString), 
            $"key {ArgumentFrameworkConstants.ArgumentHelpString} missing");
    }

    [TestMethod]
    public void Parse_OnlyCommandName()
    {
        // arrange
        var expectedCommandName = "commandname";

        string[] input = { expectedCommandName };

        // act
        var actual = SystemUnderTest.Parse(input);

        // assert
        Assert.AreEqual<string>(expectedCommandName, actual.CommandName, "Command name was wrong");
        Assert.AreEqual<int>(0, actual.Arguments.Count, "Argument count was wrong");
    }

    [TestMethod]
    public void Parse_MessyInputArgs_IgnorePositionalArgs()
    {
        // arrange
        var processPositionalArguments = false;

        var expectedCommandName = "commandname";
        var expectedKey1 = "arg1";
        var expectedVal1 = "value1";
        var expectedKey2 = "arg2";
        var expectedVal2 = "value with spaces";
        var expectedKey3 = "arg3";
        var expectedVal3 = string.Empty;

        string[] input = { expectedCommandName,
            $"/{expectedKey1}:{expectedVal1}",
            $"/{expectedKey2}:\"{expectedVal2}\"",
            $"/{expectedKey3}",
            "junk_arg4",
            "junk_arg5"
        };

        // act
        var actual = SystemUnderTest.Parse(input, 
            processPositionalArguments);

        // assert
        Assert.AreEqual<string>(expectedCommandName, actual.CommandName, "Command name was wrong");
        Assert.AreEqual<int>(3, actual.Arguments.Count, "Argument count was wrong");

        Assert.IsTrue(actual.Arguments.ContainsKey(expectedKey1), "key 1 missing");
        Assert.IsTrue(actual.Arguments.ContainsKey(expectedKey2), "key 2 missing");
        Assert.IsTrue(actual.Arguments.ContainsKey(expectedKey3), "key 3 missing");

        Assert.AreEqual<string>(expectedVal1, actual.Arguments[expectedKey1], "Key1 value was wrong");
        Assert.AreEqual<string>(expectedVal2, actual.Arguments[expectedKey2], "Key2 value was wrong");
        Assert.AreEqual<string>(expectedVal3, actual.Arguments[expectedKey3], "Key3 value was wrong");
    }
}