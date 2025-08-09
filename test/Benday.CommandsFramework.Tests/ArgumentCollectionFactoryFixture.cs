namespace Benday.CommandsFramework.Tests;

public class ArgumentCollectionFactoryFixture
{
        public ArgumentCollectionFactoryFixture()
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

    [Fact]
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
        Assert.Equal(expectedCommandName, actual.CommandName);
        Assert.Equal(3, actual.Arguments.Count);

        Assert.True(actual.Arguments.ContainsKey(expectedKey1));
        Assert.True(actual.Arguments.ContainsKey(expectedKey2));
        Assert.True(actual.Arguments.ContainsKey(expectedKey3));

        Assert.Equal(expectedVal1, actual.Arguments[expectedKey1]);
        Assert.Equal(expectedVal2, actual.Arguments[expectedKey2]);
        Assert.Equal(expectedVal3, actual.Arguments[expectedKey3]);
    }

    [Fact]
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
        var actual = SystemUnderTest.Parse(input);

        // assert
        Assert.Equal(expectedCommandName, actual.CommandName);
        Assert.Equal(4, actual.Arguments.Count);

        Assert.True(actual.Arguments.ContainsKey(expectedKey1));
        Assert.True(actual.Arguments.ContainsKey(expectedKey2));
        Assert.True(actual.Arguments.ContainsKey(expectedKey3));
        Assert.True(actual.Arguments.ContainsKey(expectedPositionalKey1));

        Assert.Equal(expectedVal1, actual.Arguments[expectedKey1]);
        Assert.Equal(expectedVal2, actual.Arguments[expectedKey2]);
        Assert.Equal(expectedVal3, actual.Arguments[expectedKey3]);
        Assert.Equal(expectedPositionalValue1, actual.Arguments[expectedPositionalKey1]);
    }

    [Fact]
    public void Parse_Positional_TwoPositionalArg()
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
        var expectedPositionalKey2 = "POSITION_2";
        var expectedPositionalValue2 = "positional2value";

        string[] input = { expectedCommandName,
            $"/{expectedKey1}:{expectedVal1}",
            $"/{expectedKey2}:\"{expectedVal2}\"",
            $"/{expectedKey3}",
            expectedPositionalValue1, 
            expectedPositionalValue2
        };

        // act
        var actual = SystemUnderTest.Parse(input);

        // assert
        Assert.Equal(expectedCommandName, actual.CommandName);
        Assert.Equal(5, actual.Arguments.Count);

        Assert.True(actual.Arguments.ContainsKey(expectedKey1));
        Assert.True(actual.Arguments.ContainsKey(expectedKey2));
        Assert.True(actual.Arguments.ContainsKey(expectedKey3));
        Assert.True(actual.Arguments.ContainsKey(expectedPositionalKey1));

        Assert.Equal(expectedVal1, actual.Arguments[expectedKey1]);
        Assert.Equal(expectedVal2, actual.Arguments[expectedKey2]);
        Assert.Equal(expectedVal3, actual.Arguments[expectedKey3]);
        Assert.Equal(expectedPositionalValue1, actual.Arguments[expectedPositionalKey1]);
        Assert.Equal(expectedPositionalValue2, actual.Arguments[expectedPositionalKey2]);
    }

    [Fact]
    public void Parse_Positional_OnePositionalArg_UnixFilePath()
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
        var expectedPositionalValue1 = "/users/thingy/filename.txt";

        string[] input = { expectedCommandName,
            $"/{expectedKey1}:{expectedVal1}",
            $"/{expectedKey2}:\"{expectedVal2}\"",
            $"/{expectedKey3}",
            expectedPositionalValue1
        };

        // act
        var actual = SystemUnderTest.Parse(input);

        // assert
        Assert.Equal(expectedCommandName, actual.CommandName);
        Assert.Equal(4, actual.Arguments.Count);

        Assert.True(actual.Arguments.ContainsKey(expectedKey1));
        Assert.True(actual.Arguments.ContainsKey(expectedKey2));
        Assert.True(actual.Arguments.ContainsKey(expectedKey3));
        Assert.True(actual.Arguments.ContainsKey(expectedPositionalKey1));

        Assert.Equal(expectedVal1, actual.Arguments[expectedKey1]);
        Assert.Equal(expectedVal2, actual.Arguments[expectedKey2]);
        Assert.Equal(expectedVal3, actual.Arguments[expectedKey3]);
        Assert.Equal(expectedPositionalValue1, actual.Arguments[expectedPositionalKey1]);
    }



    [Fact]
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
        Assert.Equal(expectedCommandName, actual.CommandName);
        Assert.Single(actual.Arguments);

        Assert.True(actual.Arguments.ContainsKey(ArgumentFrameworkConstants.ArgumentHelpString));
    }

    [Fact]
    public void Parse_OnlyCommandName()
    {
        // arrange
        var expectedCommandName = "commandname";

        string[] input = { expectedCommandName };

        // act
        var actual = SystemUnderTest.Parse(input);

        // assert
        Assert.Equal(expectedCommandName, actual.CommandName);
        Assert.Empty(actual.Arguments);
    }

    [Fact]
    public void Parse_MessyInputArgs_IgnorePositionalArgs()
    {
        // arrange
        var expectedCommandName = "commandname";
        var expectedKey1 = "arg1";
        var expectedVal1 = "value1";
        var expectedKey2 = "arg2";
        var expectedVal2 = "value with spaces";
        var expectedKey3 = "arg3";
        var expectedVal3 = string.Empty;

        var expectedKey4 = "POSITION_1";
        var expectedVal4 = "junk_arg4";
        var expectedKey5 = "POSITION_2";
        var expectedVal5 = "junk_arg5";


        string[] input = { expectedCommandName,
            $"/{expectedKey1}:{expectedVal1}",
            $"/{expectedKey2}:\"{expectedVal2}\"",
            $"/{expectedKey3}",
            expectedVal4,
            expectedVal5
        };

        // act
        var actual = SystemUnderTest.Parse(input);

        // assert
        Assert.Equal(expectedCommandName, actual.CommandName);
        Assert.Equal(5, actual.Arguments.Count);

        Assert.True(actual.Arguments.ContainsKey(expectedKey1));
        Assert.True(actual.Arguments.ContainsKey(expectedKey2));
        Assert.True(actual.Arguments.ContainsKey(expectedKey3));
        Assert.True(actual.Arguments.ContainsKey(expectedKey4));
        Assert.True(actual.Arguments.ContainsKey(expectedKey5));

        Assert.Equal(expectedVal1, actual.Arguments[expectedKey1]);
        Assert.Equal(expectedVal2, actual.Arguments[expectedKey2]);
        Assert.Equal(expectedVal3, actual.Arguments[expectedKey3]);
        Assert.Equal(expectedVal4, actual.Arguments[expectedKey4]);
        Assert.Equal(expectedVal5, actual.Arguments[expectedKey5]);
    }

    [Theory]
    [InlineData("/asdf/asdf.txt", 2)]
    [InlineData("/asdf:asdf.txt", 1)]
    [InlineData("asdf", 0)]
    public void GetSlashCount(
        string inputString, int expected)
    {
        // arrange

        // act
        var actual = ArgumentCollectionFactory.GetSlashCount(inputString);

        // assert
        Assert.Equal(expected, actual);
    }
}