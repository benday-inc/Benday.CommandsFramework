using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

public class StringArgumentAllowedValuesFixture
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

    // ---- StringArgument.Validate() unit tests ----

    [Fact]
    public void Validate_Required_AllowedValues_ValidValue_ReturnsTrue()
    {
        // arrange
        var arg = new ArgumentCollection().AddString("environment")
            .WithAllowedValues("dev", "staging", "prod")
            .AsRequired();
        arg.Value = "staging";

        // act
        var actual = arg.Validate();

        // assert
        Assert.True(actual);
    }

    [Fact]
    public void Validate_Required_AllowedValues_InvalidValue_ReturnsFalse()
    {
        // arrange
        var arg = new ArgumentCollection().AddString("environment")
            .WithAllowedValues("dev", "staging", "prod")
            .AsRequired();
        arg.Value = "production";

        // act
        var actual = arg.Validate();

        // assert
        Assert.False(actual);
    }

    [Fact]
    public void Validate_Required_AllowedValues_CaseInsensitive_ReturnsTrue()
    {
        // arrange
        var arg = new ArgumentCollection().AddString("environment")
            .WithAllowedValues("dev", "staging", "prod")
            .AsRequired();
        arg.Value = "PROD";

        // act
        var actual = arg.Validate();

        // assert
        Assert.True(actual);
    }

    [Fact]
    public void Validate_Required_NoAllowedValues_AnyValue_ReturnsTrue()
    {
        // arrange
        var arg = new ArgumentCollection().AddString("name")
            .AsRequired();
        arg.Value = "anything goes";

        // act
        var actual = arg.Validate();

        // assert
        Assert.True(actual);
    }

    [Fact]
    public void Validate_NotRequired_AllowedValues_ValidValue_ReturnsTrue()
    {
        // arrange
        var arg = new ArgumentCollection().AddString("mode")
            .WithAllowedValues("fast", "slow")
            .AsNotRequired();
        arg.Value = "fast";

        // act
        var actual = arg.Validate();

        // assert
        Assert.True(actual);
    }

    [Fact]
    public void Validate_NotRequired_AllowedValues_InvalidValue_ReturnsFalse()
    {
        // arrange
        var arg = new ArgumentCollection().AddString("mode")
            .WithAllowedValues("fast", "slow")
            .AsNotRequired();
        arg.Value = "turbo";

        // act
        var actual = arg.Validate();

        // assert
        Assert.False(actual);
    }

    [Fact]
    public void Validate_NotRequired_AllowedValues_NoValueSupplied_ReturnsTrue()
    {
        // arrange
        var arg = new ArgumentCollection().AddString("mode")
            .WithAllowedValues("fast", "slow")
            .AsNotRequired();

        // act
        var actual = arg.Validate();

        // assert
        Assert.False(arg.HasValue);
        Assert.True(actual);
    }

    // ---- AllowedValues exposed via IArgument for JSON schema ----

    [Fact]
    public void AllowedValues_AreAccessibleViaIArgument()
    {
        // arrange
        var arg = new ArgumentCollection().AddString("environment")
            .WithAllowedValues("dev", "staging", "prod");

        IArgument iarg = arg;

        // act & assert
        Assert.Equal(new[] { "dev", "staging", "prod" }, iarg.AllowedValues);
    }

    // ---- Command-level integration tests ----

    [Fact]
    public void Command_AllowedValues_ValidValue_Succeeds()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            ApplicationConstants.CommandName_CommandWithAllowedValues,
            "/environment:prod"
        );

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);
        var command = new SampleCommandWithAllowedValues(executionInfo, OutputProvider);

        // act
        command.Execute();

        // assert
        var output = OutputProvider.GetOutput();
        Assert.Contains("** SUCCESS **", output);
    }

    [Fact]
    public void Command_AllowedValues_InvalidValue_ShowsValidationError()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            ApplicationConstants.CommandName_CommandWithAllowedValues,
            "/environment:production"
        );

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);
        var command = new SampleCommandWithAllowedValues(executionInfo, OutputProvider);

        // act
        command.Execute();

        // assert
        var output = OutputProvider.GetOutput();
        Assert.DoesNotContain("** SUCCESS **", output);
        Assert.Contains("** INVALID ARGUMENT **", output);
        Assert.Contains("environment is not valid or missing", output);
    }

    [Fact]
    public void Command_AllowedValues_OptionalArgWithInvalidValue_ShowsValidationError()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            ApplicationConstants.CommandName_CommandWithAllowedValues,
            "/environment:dev",
            "/mode:turbo"
        );

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);
        var command = new SampleCommandWithAllowedValues(executionInfo, OutputProvider);

        // act
        command.Execute();

        // assert
        var output = OutputProvider.GetOutput();
        Assert.DoesNotContain("** SUCCESS **", output);
        Assert.Contains("** INVALID ARGUMENT **", output);
        Assert.Contains("mode is not valid or missing", output);
    }

    // ---- allowed values belong to string arguments only ----

    [Fact]
    public void AllowedValues_OnInt32Argument_Throws()
    {
        // arrange
        var arg = new ArgumentCollection().AddInt32("count");

        // act & assert -- silently ignoring the list would still ship it in the --json
        // schema, so cmdui would render a dropdown that nothing enforces
        var actual = Assert.Throws<InvalidOperationException>(
            () => arg.AllowedValues = ["1", "2"]);

        Assert.Contains("count", actual.Message);
        Assert.Contains("Int32", actual.Message);
    }

    [Fact]
    public void AllowedValues_OnBooleanArgument_Throws()
    {
        var arg = new ArgumentCollection().AddBoolean("verbose");

        Assert.Throws<InvalidOperationException>(() => arg.AllowedValues = ["true"]);
    }

    [Fact]
    public void AllowedValues_OnDateTimeArgument_Throws()
    {
        var arg = new ArgumentCollection().AddDateTime("asof");

        Assert.Throws<InvalidOperationException>(() => arg.AllowedValues = ["2026-01-01"]);
    }

    [Fact]
    public void AllowedValues_OnNonStringArgument_IsEmptyInTheSchema()
    {
        // arrange
        var args = new ArgumentCollection();
        args.AddInt32("count");
        args.AddBoolean("verbose");
        args.AddDateTime("asof");

        // act & assert
        foreach (var arg in args)
        {
            Assert.Empty(arg.AllowedValues);
        }
    }

    [Fact]
    public void AllowedValues_OnFileArgument_IsEnforced()
    {
        // arrange -- file and directory arguments are string arguments, so they keep the
        // feature and keep enforcing it through base.Validate()
        var arg = new ArgumentCollection().AddFile("template")
            .WithAllowedValues("small.json", "large.json")
            .AsRequired();

        // act & assert
        arg.Value = "small.json";
        Assert.True(arg.Validate());

        arg.Value = "other.json";
        Assert.False(arg.Validate());
    }
}
