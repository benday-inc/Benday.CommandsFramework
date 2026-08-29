using Benday.CommandsFramework;
using Benday.CommandsFramework.Tui.Model;

namespace Benday.CommandsFramework.Tui.Tests;

/// <summary>
/// Tests how a form draws and edits one argument. The widget mapping is a decision, so it
/// lives in the model and is tested here rather than being eyeballed in a terminal.
/// </summary>
public class TuiFieldFixture
{
    private static TuiField GetField(ArgumentCollection args, string name)
    {
        return new TuiField(args[name]);
    }

    [Fact]
    public void AStringArgumentIsATextField()
    {
        // arrange
        var args = new ArgumentCollection();

        args.AddString("thing");

        // assert
        Assert.Equal(TuiFieldWidget.Text, TuiField.GetWidget(args["thing"]));
    }

    [Fact]
    public void ABooleanArgumentIsAToggle()
    {
        // arrange
        var args = new ArgumentCollection();

        args.AddBoolean("thing");

        // assert
        Assert.Equal(TuiFieldWidget.Toggle, TuiField.GetWidget(args["thing"]));
    }

    [Fact]
    public void AnInt32ArgumentIsANumberField()
    {
        // arrange
        var args = new ArgumentCollection();

        args.AddInt32("thing");

        // assert
        Assert.Equal(TuiFieldWidget.Number, TuiField.GetWidget(args["thing"]));
    }

    [Fact]
    public void ADateTimeArgumentIsADateField()
    {
        // arrange
        var args = new ArgumentCollection();

        args.AddDateTime("thing");

        // assert
        Assert.Equal(TuiFieldWidget.Date, TuiField.GetWidget(args["thing"]));
    }

    [Fact]
    public void AFileArgumentIsAPathFieldEvenThoughItsDataTypeIsString()
    {
        // arrange -- what tells a path from a string is PathType, not DataType. A file
        // argument derives from StringArgument and reports String for its data type.
        var args = new ArgumentCollection();

        args.AddFile("thing");

        // assert
        Assert.Equal(ArgumentDataType.String, args["thing"].DataType);
        Assert.Equal(TuiFieldWidget.FilePath, TuiField.GetWidget(args["thing"]));
    }

    [Fact]
    public void ADirectoryArgumentIsADirectoryField()
    {
        // arrange
        var args = new ArgumentCollection();

        args.AddDirectory("thing");

        // assert
        Assert.Equal(TuiFieldWidget.DirectoryPath, TuiField.GetWidget(args["thing"]));
    }

    [Fact]
    public void AllowedValuesWinOverTheDataType()
    {
        // arrange
        var args = new ArgumentCollection();

        args.AddString("thing").WithAllowedValues("a", "b");

        // assert
        Assert.Equal(TuiFieldWidget.Selection, TuiField.GetWidget(args["thing"]));
    }

    [Fact]
    public void AllowedValuesWinOverBeingAPathToo()
    {
        // arrange -- a file argument derives from StringArgument and so can carry a list of
        // allowed values. A list of choices is more specific than "this is a path".
        var args = new ArgumentCollection();

        args.AddFile("thing").WithAllowedValues("one.json", "two.json");

        // assert
        Assert.Equal(ArgumentPathType.File, args["thing"].PathType);
        Assert.Equal(TuiFieldWidget.Selection, TuiField.GetWidget(args["thing"]));
    }

    [Fact]
    public void TheLabelIsTheFriendlyNameWhenThereIsOne()
    {
        // arrange -- FriendlyName is not used by console usage output at all. It travels in
        // the schema and becomes a form label, which is what this is.
        var args = new ArgumentCollection();

        args.AddString("thing").WithFriendlyName("The Thing");

        // assert
        Assert.Equal("The Thing", GetField(args, "thing").Label);
    }

    [Fact]
    public void TheLabelFallsBackToTheArgumentName()
    {
        // arrange
        var args = new ArgumentCollection();

        args.AddString("thing");

        // assert -- the framework defaults FriendlyName to the name, so either way the label
        // is the name
        Assert.Equal("thing", GetField(args, "thing").Label);
    }

    [Fact]
    public void ARequiredArgumentThatAllowsAnEmptyValueIsNotMarkedRequired()
    {
        // arrange -- being present at all satisfies it, which is how a boolean flag works, so
        // marking it required would be a lie
        var args = new ArgumentCollection();

        args.AddBoolean("verbose").AsRequired().AllowEmptyValue();
        args.AddString("name").AsRequired();

        // assert
        Assert.False(GetField(args, "verbose").IsRequired);
        Assert.True(GetField(args, "name").IsRequired);
    }

    [Fact]
    public void AConfiguredDefaultIsCarriedOnTheField()
    {
        // arrange
        var args = new ArgumentCollection();

        args.AddString("thing").WithDefaultValue("blue");
        args.AddString("other");

        // assert -- the implicit type default does not count
        var withDefault = GetField(args, "thing");

        Assert.True(withDefault.HasDefaultValue);
        Assert.Equal("blue", withDefault.DefaultValue);
        Assert.False(GetField(args, "other").HasDefaultValue);
    }

    [Fact]
    public void AnArgumentThatReadsFromConfigurationSaysSo()
    {
        // arrange
        var args = new ArgumentCollection();

        args.AddString("token").FromConfig();

        // assert
        Assert.True(GetField(args, "token").IsFromConfig);
    }

    [Fact]
    public void ADiscoverableArgumentSaysWhatBlankWillLookFor()
    {
        // arrange
        var args = new ArgumentCollection();

        args.AddFile("input").DiscoverSingleMatch("*.json");

        // act
        var field = GetField(args, "input");

        // assert
        Assert.True(field.IsDiscoverable);
        Assert.Contains("*.json", field.DiscoveryHint);
        Assert.Contains("the current directory", field.DiscoveryHint);
    }

    [Fact]
    public void APositionalArgumentKnowsWhichSlotItReads()
    {
        // arrange -- FromPositionalArgument(n) records the position in the alias, which is
        // the only place it lives
        var args = new ArgumentCollection();

        args.AddString("first").FromPositionalArgument(1);
        args.AddString("second").FromPositionalArgument(2);
        args.AddString("named");

        // assert
        Assert.Equal(1, GetField(args, "first").Position);
        Assert.Equal(2, GetField(args, "second").Position);
        Assert.False(GetField(args, "named").IsPositional);
        Assert.Equal(0, GetField(args, "named").Position);
    }

    [Fact]
    public void AValueTheArgumentRefusesLeavesThePreviousOneAlone()
    {
        // arrange -- this is what makes validating a field as it is typed safe
        var args = new ArgumentCollection();

        args.AddInt32("count");

        var field = GetField(args, "count");

        Assert.True(field.TrySetValue("7"));

        // act
        var accepted = field.TrySetValue("not a number");

        // assert
        Assert.False(accepted);
        Assert.Equal("7", field.Value);
    }

    [Theory]
    [InlineData("nope", "whole number")]
    [InlineData("", "required")]
    public void GetProblemWithValue_SaysWhatIsWrongWithANumber(string value, string expected)
    {
        // arrange
        var args = new ArgumentCollection();

        args.AddInt32("count").AsRequired();

        // assert
        Assert.Contains(expected, GetField(args, "count").GetProblemWithValue(value));
    }

    [Fact]
    public void GetProblemWithValue_RejectsAValueThatIsNotAllowed()
    {
        // arrange
        var args = new ArgumentCollection();

        args.AddString("environment").WithAllowedValues("dev", "prod");

        // act
        var problem = GetField(args, "environment").GetProblemWithValue("staging");

        // assert
        Assert.Contains("dev, prod", problem);
    }

    [Fact]
    public void GetProblemWithValue_AcceptsAValueThatIsAllowed()
    {
        // arrange
        var args = new ArgumentCollection();

        args.AddString("environment").WithAllowedValues("dev", "prod");

        // assert
        Assert.Empty(GetField(args, "environment").GetProblemWithValue("prod"));
    }

    [Fact]
    public void GetProblemWithValue_RejectsAMissingFileWhenTheArgumentSaysItHasToExist()
    {
        // arrange
        var args = new ArgumentCollection();

        args.AddFile("input").MustExist();

        // act
        var problem = GetField(args, "input")
            .GetProblemWithValue(Path.Combine(Path.GetTempPath(), "definitely-not-here.txt"));

        // assert
        Assert.Contains("has to be a file that exists", problem);
    }

    [Fact]
    public void GetProblemWithValue_LeavesAMissingFileAloneWhenExistenceIsOptional()
    {
        // arrange
        var args = new ArgumentCollection();

        args.AddFile("output");

        // assert
        Assert.Empty(
            GetField(args, "output")
                .GetProblemWithValue(Path.Combine(Path.GetTempPath(), "not-here-yet.txt")));
    }

    [Fact]
    public void AnEmptyOptionalValueIsNotAProblem()
    {
        // arrange
        var args = new ArgumentCollection();

        args.AddString("thing").AsNotRequired();

        // assert
        Assert.Empty(GetField(args, "thing").GetProblemWithValue(string.Empty));
    }
}
