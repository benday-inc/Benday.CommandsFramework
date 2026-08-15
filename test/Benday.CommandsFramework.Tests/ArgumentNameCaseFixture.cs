namespace Benday.CommandsFramework.Tests;

/// <summary>
/// Argument names are matched without regard to case. Before this was fixed, flag style
/// '/name' arguments were lowercased during parsing while the argument lookup was
/// case-sensitive, so a flag argument whose definition contained uppercase letters could
/// never be set from the command line and silently landed in UnrecognizedKeys.
/// </summary>
public class ArgumentNameCaseFixture
{
    private static ArgumentCollection GetArgumentDefinitions()
    {
        var args = new ArgumentCollection();

        args.AddBoolean("isThingy").AsNotRequired().AllowEmptyValue();
        args.AddString("environment").AsNotRequired();
        args.AddString("longName").AsNotRequired().WithAlias("ln");

        return args;
    }

    private static Dictionary<string, string> Parse(params string[] commandLineArgs)
    {
        return new ArgumentCollectionFactory().Parse(commandLineArgs).Arguments;
    }

    [Theory]
    [InlineData("/isThingy")]
    [InlineData("/isthingy")]
    [InlineData("/ISTHINGY")]
    [InlineData("/IsThingy")]
    public void FlagStyleArgumentMatchesRegardlessOfCase(string suppliedArgument)
    {
        // arrange
        var args = GetArgumentDefinitions();

        // act
        args.SetValues(Parse("somecommand", suppliedArgument));

        // assert
        Assert.True(args["isThingy"].HasValue);
        Assert.Equal("True", args["isThingy"].Value);
        Assert.Empty(args.UnrecognizedKeys);
    }

    [Theory]
    [InlineData("/environment:staging")]
    [InlineData("/ENVIRONMENT:staging")]
    [InlineData("/Environment:staging")]
    public void NamedArgumentMatchesRegardlessOfCase(string suppliedArgument)
    {
        // arrange
        var args = GetArgumentDefinitions();

        // act
        args.SetValues(Parse("somecommand", suppliedArgument));

        // assert
        Assert.True(args["environment"].HasValue);
        Assert.Equal("staging", args["environment"].Value);
        Assert.Empty(args.UnrecognizedKeys);
    }

    [Theory]
    [InlineData("/ln:value")]
    [InlineData("/LN:value")]
    [InlineData("/Ln:value")]
    public void ArgumentAliasMatchesRegardlessOfCase(string suppliedArgument)
    {
        // arrange
        var args = GetArgumentDefinitions();

        // act
        args.SetValues(Parse("somecommand", suppliedArgument));

        // assert
        Assert.True(args["longName"].HasValue);
        Assert.Equal("value", args["longName"].Value);
        Assert.Empty(args.UnrecognizedKeys);
    }

    [Fact]
    public void ArgumentValueKeepsItsCase()
    {
        // arrange
        var args = GetArgumentDefinitions();

        // act
        // only the argument *name* is case-insensitive. The value is untouched.
        args.SetValues(Parse("somecommand", "/ENVIRONMENT:StAgInG"));

        // assert
        Assert.Equal("StAgInG", args["environment"].Value);
    }

    [Fact]
    public void ParsedArgumentNameKeepsTheCaseThatWasTyped()
    {
        // act
        var parsed = Parse("somecommand", "/isThingy", "/ENVIRONMENT:staging");

        // assert
        // the name is no longer lowercased during parsing, so an unrecognized argument
        // can be reported back to the user the way they actually typed it
        Assert.Contains("isThingy", parsed.Keys);
        Assert.Contains("ENVIRONMENT", parsed.Keys);
    }

    [Fact]
    public void UnrecognizedArgumentIsStillDetected()
    {
        // arrange
        var args = GetArgumentDefinitions();

        // act
        args.SetValues(Parse("somecommand", "/NoSuchArgument:whatever"));

        // assert
        var unrecognized = Assert.Single(args.UnrecognizedKeys);
        Assert.Equal("NoSuchArgument", unrecognized);
    }

    [Fact]
    public void SameArgumentSuppliedTwiceInDifferentCaseIsTreatedAsOneArgument()
    {
        // act
        var parsed = Parse("somecommand", "/environment:first", "/ENVIRONMENT:second");

        // assert
        // the parser keeps the first occurrence of an argument, and differing case no
        // longer sneaks a second copy of the same argument into the dictionary
        Assert.Single(parsed);
        Assert.Equal("first", parsed["environment"]);
    }

    [Fact]
    public void ReservedFrameworkArgumentsMatchRegardlessOfCase()
    {
        // arrange
        var args = GetArgumentDefinitions();

        // act
        args.SetValues(Parse("somecommand", "/QUIET"));

        // assert
        // 'quiet' is reserved, so it must not be reported as an unrecognized argument
        Assert.Empty(args.UnrecognizedKeys);
    }

    [Fact]
    public void CollectionLookupIsCaseInsensitive()
    {
        // arrange
        var args = GetArgumentDefinitions();

        // act & assert
        Assert.True(args.ContainsKey("ISTHINGY"));
        Assert.True(args.ContainsKey("isthingy"));
        Assert.NotNull(args["IsThingy"]);
    }
}
