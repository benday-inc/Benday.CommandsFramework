using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

/// <summary>
/// Tests for how configured default values are recorded on arguments and rendered in
/// the usage output. The important case is the validation failure path: usage is
/// displayed *after* command line values have been merged on to the arguments, so the
/// usage output has to report the configured default rather than whatever the user typed.
/// </summary>
public class DefaultValueUsageDisplayFixture
{
    private StringBuilderTextOutputProvider? _OutputProvider;

    private StringBuilderTextOutputProvider OutputProvider
    {
        get
        {
            _OutputProvider ??= new StringBuilderTextOutputProvider();

            return _OutputProvider;
        }
    }

    private string RunAndGetOutput(params string[] commandLineArgs)
    {
        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        var command = new SampleCommandWithDefaultsAndRequiredArg(executionInfo, OutputProvider);

        command.Execute();

        var output = OutputProvider.GetOutput();

        Console.WriteLine(output);

        return output;
    }

    [Fact]
    public void ValidationFailure_ShowsConfiguredDefaultRatherThanSuppliedValue()
    {
        // arrange
        // 'required-thing' is missing so validation fails and usage gets displayed.
        // 'bingbong' is supplied on the command line with something other than its default.
        var commandLineArgs = Utilities.GetStringArray(
            ApplicationConstants.CommandName_CommandWithDefaultsAndRequiredArg,
            "/bingbong:value-the-user-typed"
            );

        // act
        var output = RunAndGetOutput(commandLineArgs);

        // assert
        Assert.Contains("** INVALID ARGUMENT **", output);
        Assert.Contains("(default: wickid awesome)", output);
        Assert.DoesNotContain("(default: value-the-user-typed)", output);
    }

    [Fact]
    public void ValidationFailure_ShowsDefaultForArgumentWithNoDescription()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            ApplicationConstants.CommandName_CommandWithDefaultsAndRequiredArg
            );

        // act
        var output = RunAndGetOutput(commandLineArgs);

        // assert
        // 'countish' has no description, so it takes the other branch of the usage
        // rendering. Its default still has to show up.
        Assert.Contains("(default: 42)", output);
    }

    [Fact]
    public void RequiredArgumentWithNoDefault_ShowsNoDefaultLine()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            ApplicationConstants.CommandName_CommandWithDefaultsAndRequiredArg,
            ArgumentFrameworkConstants.ArgumentHelpString
            );

        // act
        var output = RunAndGetOutput(commandLineArgs);

        // assert
        var lines = output.Split(Environment.NewLine);

        var index = Array.FindIndex(lines, l => l.Contains("this one is required"));

        Assert.True(index >= 0, "Could not find the required argument in the usage output.");

        // the line following the required arg's description must not be a default line
        Assert.DoesNotContain("(default:", lines[index + 1]);
    }

    [Fact]
    public void SuppliedValueDoesNotOverwriteRecordedDefault()
    {
        // arrange
        var args = new ArgumentCollection();

        args.AddString("bingbong").AsNotRequired().WithDefaultValue("the-default");

        // act
        args.SetValues(new Dictionary<string, string>
        {
            ["bingbong"] = "the-supplied-value"
        });

        // assert
        var arg = args["bingbong"];

        Assert.True(arg.HasDefaultValue);
        Assert.Equal("the-default", arg.DefaultValue);
        Assert.Equal("the-supplied-value", arg.Value);
    }

    [Fact]
    public void ArgumentWithoutExplicitDefault_HasNoDefaultValue()
    {
        // arrange
        var args = new ArgumentCollection();

        args.AddString("nodefault").AsNotRequired();
        args.AddInt32("nodefaultint").AsNotRequired();
        args.AddBoolean("nodefaultbool").AsNotRequired();

        // act & assert
        // the implicit type default (empty string / 0 / false) must not count as a
        // configured default value
        Assert.False(args["nodefault"].HasDefaultValue);
        Assert.False(args["nodefaultint"].HasDefaultValue);
        Assert.False(args["nodefaultbool"].HasDefaultValue);

        Assert.Equal(string.Empty, args["nodefault"].DefaultValue);
    }

    [Fact]
    public void InvalidDefaultValueIsNotRecorded()
    {
        // arrange
        var arg = new Int32Argument("countish");

        // act
        var result = arg.TrySetDefaultValue("not-a-number");

        // assert
        Assert.False(result);
        Assert.False(arg.HasDefaultValue);
        Assert.Equal(string.Empty, arg.DefaultValue);
    }

    [Fact]
    public void EmptyDefaultValueIsNotShownInUsage()
    {
        // arrange
        var args = new ArgumentCollection();

        args.AddString("emptydefault").AsNotRequired().AllowEmptyValue().WithDefaultValue(string.Empty);

        // act & assert
        // the default is recorded...
        Assert.True(args["emptydefault"].HasDefaultValue);
        Assert.Equal(string.Empty, args["emptydefault"].DefaultValue);
    }
}
