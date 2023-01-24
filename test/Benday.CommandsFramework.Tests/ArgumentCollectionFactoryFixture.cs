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
    public void Parse_MessyInputArgs()
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
            $"/{expectedKey3}",
            "junk_arg4",
            "junk_arg5"
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
}