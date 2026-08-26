using Benday.CommandsFramework;
using Benday.CommandsFramework.Tui.Model;

namespace Benday.CommandsFramework.Tui.Tests;

/// <summary>
/// Tests the command line preview. This is the feature that most has to be right: the
/// interface teaching the command line only works if what it shows is what the tool accepts.
/// </summary>
public class TuiCommandLineFixture
{
    private static List<TuiField> GetFields(ArgumentCollection args)
    {
        return args.Select(x => new TuiField(x)).ToList();
    }

    [Fact]
    public void OnlyTheValuesThatWereSuppliedAppear()
    {
        // arrange
        var args = new ArgumentCollection();

        args.AddString("name").AsNotRequired();
        args.AddString("other").AsNotRequired();

        var fields = GetFields(args);

        fields[0].TrySetValue("thing");

        // act
        var text = TuiCommandLine.GetDisplayText(
            "mytool", "greeting", fields, ArgumentSyntax.Posix);

        // assert
        Assert.Equal("mytool greeting --name thing", text);
    }

    [Fact]
    public void ThePosixFormIsWrittenTheWayAPersonTypesIt()
    {
        // arrange
        var args = new ArgumentCollection();

        args.AddString("environment").AsNotRequired();

        var fields = GetFields(args);

        fields[0].TrySetValue("production");

        // act
        var text = TuiCommandLine.GetDisplayText(
            "mytool", "deploy", fields, ArgumentSyntax.Posix);

        // assert -- a space, not an equals sign. The single token form exists so a value
        // survives being one element of an argument array, which is a different job.
        Assert.Equal("mytool deploy --environment production", text);
    }

    [Fact]
    public void ASlashToolGetsASlashPreview()
    {
        // arrange -- the tool declares the syntax, and a preview it cannot parse is worse
        // than no preview at all
        var args = new ArgumentCollection();

        args.AddString("environment").AsNotRequired();

        var fields = GetFields(args);

        fields[0].TrySetValue("production");

        // act
        var text = TuiCommandLine.GetDisplayText(
            "mytool", "deploy", fields, ArgumentSyntax.Slash);

        // assert
        Assert.Equal("mytool deploy /environment:production", text);
    }

    [Fact]
    public void ABooleanThatAllowsAnEmptyValueIsWrittenAsABareFlag()
    {
        // arrange
        var args = new ArgumentCollection();

        args.AddBoolean("verbose").AsNotRequired().AllowEmptyValue();

        var fields = GetFields(args);

        fields[0].TrySetValue(bool.TrueString);

        // act
        var text = TuiCommandLine.GetDisplayText(
            "mytool", "run", fields, ArgumentSyntax.Posix);

        // assert
        Assert.Equal("mytool run --verbose", text);
    }

    [Fact]
    public void ABooleanThatIsOffIsLeftOutEntirely()
    {
        // arrange -- a flag that is off is the absence of the flag, not a value
        var args = new ArgumentCollection();

        args.AddBoolean("verbose").AsNotRequired().AllowEmptyValue();

        var fields = GetFields(args);

        fields[0].TrySetValue(bool.FalseString);

        // act
        var text = TuiCommandLine.GetDisplayText(
            "mytool", "run", fields, ArgumentSyntax.Posix);

        // assert
        Assert.Equal("mytool run", text);
    }

    [Fact]
    public void ABooleanThatDoesNotAllowAnEmptyValueIsWrittenWithItsValue()
    {
        // arrange
        var args = new ArgumentCollection();

        args.AddBoolean("enabled").AsNotRequired();

        var fields = GetFields(args);

        fields[0].TrySetValue(bool.TrueString);

        // act
        var text = TuiCommandLine.GetDisplayText(
            "mytool", "run", fields, ArgumentSyntax.Posix);

        // assert
        Assert.Equal("mytool run --enabled True", text);
    }

    [Fact]
    public void PositionalValuesAreWrittenWithoutTheirNamesAndInOrder()
    {
        // arrange
        var args = new ArgumentCollection();

        args.AddString("second").AsNotRequired().FromPositionalArgument(2);
        args.AddString("first").AsNotRequired().FromPositionalArgument(1);
        args.AddString("named").AsNotRequired();

        var fields = GetFields(args);

        foreach (var field in fields)
        {
            field.TrySetValue(field.Name);
        }

        // act
        var text = TuiCommandLine.GetDisplayText(
            "mytool", "run", fields, ArgumentSyntax.Posix);

        // assert -- declaration order is not position order, and position order is what a
        // positional value is read by
        Assert.Equal("mytool run first second --named named", text);
    }

    [Fact]
    public void AValueThatIsOnlyTheArgumentsOwnDefaultIsLeftOut()
    {
        // arrange -- leaving it out produces the same run, so writing it is noise
        var args = new ArgumentCollection();

        args.AddString("colour").AsNotRequired().WithDefaultValue("blue");

        var fields = GetFields(args);

        // act
        var text = TuiCommandLine.GetDisplayText(
            "mytool", "run", fields, ArgumentSyntax.Posix);

        // assert
        Assert.Equal("mytool run", text);
    }

    [Fact]
    public void AValueThatDiffersFromTheDefaultIsWritten()
    {
        // arrange
        var args = new ArgumentCollection();

        args.AddString("colour").AsNotRequired().WithDefaultValue("blue");

        var fields = GetFields(args);

        fields[0].TrySetValue("red");

        // act
        var text = TuiCommandLine.GetDisplayText(
            "mytool", "run", fields, ArgumentSyntax.Posix);

        // assert
        Assert.Equal("mytool run --colour red", text);
    }

    [Fact]
    public void AValueWithSpacesIsQuoted()
    {
        // arrange
        var args = new ArgumentCollection();

        args.AddString("name").AsNotRequired();

        var fields = GetFields(args);

        fields[0].TrySetValue("Ada Lovelace");

        // act
        var text = TuiCommandLine.GetDisplayText(
            "mytool", "greeting", fields, ArgumentSyntax.Posix);

        // assert
        Assert.Equal("mytool greeting --name \"Ada Lovelace\"", text);
    }

    [Fact]
    public void TheTokensUseTheSingleTokenFormSoAValueSurvivesBeingOneArrayElement()
    {
        // arrange
        var args = new ArgumentCollection();

        args.AddString("name").AsNotRequired();

        var fields = GetFields(args);

        fields[0].TrySetValue("Ada Lovelace");

        // act
        var tokens = TuiCommandLine.GetTokens("greeting", fields, ArgumentSyntax.Posix);

        // assert -- one token, unquoted, because the array element is the boundary
        Assert.Equal(["greeting", "--name=Ada Lovelace"], tokens);
    }

    [Fact]
    public void AMultiLevelCommandNameBecomesSeparateTokens()
    {
        // arrange
        var args = new ArgumentCollection();

        // act
        var tokens = TuiCommandLine.GetTokens("widget list", GetFields(args), ArgumentSyntax.Posix);

        // assert -- the registry decides where the name stops, and it reads tokens
        Assert.Equal(["widget", "list"], tokens);
    }
}
