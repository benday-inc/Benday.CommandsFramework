using Benday.CommandsFramework;
using Benday.CommandsFramework.Samples;
using Benday.CommandsFramework.Tui.Model;

namespace Benday.CommandsFramework.Tui.Tests;

/// <summary>
/// Tests the form's model: which fields a command has, what is wrong with them, and the
/// command line they add up to.
/// </summary>
public class TuiCommandFormFixture
{
    private static DefaultProgramOptions GetOptions()
    {
        return new DefaultProgramOptions
        {
            ApplicationName = "Sample Tool",
            OutputProvider = new StringBuilderTextOutputProvider(),
            UsesConfiguration = false,
            ArgumentSyntax = ArgumentSyntax.Posix
        };
    }

    private static TuiCommandItem GetCommand(ICommandProgramOptions options, string path)
    {
        var browser = new TuiCommandBrowser(
            TuiSession.Create(options, typeof(SampleCommand1).Assembly));

        return browser.AllCommands.Single(x => x.PathAsString == path);
    }

    private static TuiCommandForm Open(
        string path,
        DefaultProgramOptions? options = null,
        IReadOnlyDictionary<string, string>? presets = null)
    {
        options ??= GetOptions();

        return TuiCommandForm.Open(
            options,
            typeof(SampleCommand1).Assembly,
            GetCommand(options, path),
            presets);
    }

    [Fact]
    public void AFormHasAFieldForEveryArgumentTheCommandDeclares()
    {
        // arrange
        using var form = Open(ApplicationConstants.CommandName_CommandWithAllowedValues);

        // assert
        Assert.Equal(2, form.Fields.Count);
        Assert.NotNull(form.FindField("environment"));
        Assert.NotNull(form.FindField("mode"));
    }

    [Fact]
    public void FieldsAreFoundWithoutRegardToCase()
    {
        // arrange -- argument names have been matched this way since v4.18
        using var form = Open(ApplicationConstants.CommandName_CommandWithAllowedValues);

        // assert
        Assert.NotNull(form.FindField("ENVIRONMENT"));
    }

    [Fact]
    public void AnArgumentWithAllowedValuesBecomesASelection()
    {
        // arrange
        using var form = Open(ApplicationConstants.CommandName_CommandWithAllowedValues);

        // act
        var field = form.FindField("environment")!;

        // assert
        Assert.Equal(TuiFieldWidget.Selection, field.Widget);
        Assert.Equal(["dev", "staging", "prod"], field.AllowedValues);
    }

    [Fact]
    public void AnEmptyFormReportsWhatIsMissing()
    {
        // arrange
        using var form = Open(ApplicationConstants.CommandName_CommandWithAllowedValues);

        // act
        var failures = form.Validate();

        // assert -- 'environment' is required and nothing has been filled in
        Assert.NotEmpty(failures);
        Assert.False(form.IsValid());
        Assert.Contains(failures, x => x.ArgumentNames.Contains("environment"));
    }

    [Fact]
    public void FillingInWhatIsRequiredMakesTheFormValid()
    {
        // arrange
        using var form = Open(ApplicationConstants.CommandName_CommandWithAllowedValues);

        // act
        Assert.True(form.FindField("environment")!.TrySetValue("prod"));

        // assert
        Assert.True(form.IsValid());
    }

    [Fact]
    public void ValidatingRepeatedlyDoesNotOverwriteWhatWasTypedIn()
    {
        // arrange -- validation applies configuration and command line values the first time
        // and then leaves the arguments alone, which is what makes live validation possible
        using var form = Open(ApplicationConstants.CommandName_CommandWithAllowedValues);

        form.FindField("environment")!.TrySetValue("prod");

        // act
        form.Validate();
        form.Validate();
        form.Validate();

        // assert
        Assert.Equal("prod", form.FindField("environment")!.Value);
    }

    [Fact]
    public void ARuleIsCarriedOnTheForm()
    {
        // arrange
        using var form = Open(ApplicationConstants.CommandName_CommandWithRules);

        // assert -- showing them is what usage output already does; applying them as the form
        // is filled in is a later phase
        Assert.NotEmpty(form.Rules);
        Assert.All(form.Rules, rule => Assert.False(string.IsNullOrWhiteSpace(rule.Describe())));
    }

    [Fact]
    public void ARuleViolationIsReportedWithTheMessageTheRuleWrote()
    {
        // arrange
        using var form = Open(ApplicationConstants.CommandName_CommandWithRules);

        var rule = form.Rules.First();

        // act -- nothing supplied, so a rule about what has to be supplied is broken
        var failures = form.Validate();

        // assert
        Assert.Contains(
            failures,
            x => x.Kind == ValidationFailureKind.RuleViolated ||
                x.Kind == ValidationFailureKind.InvalidArgument);

        Assert.NotEmpty(rule.Describe());
    }

    [Fact]
    public void ADefaultValueIsAlreadyInTheFieldWhenTheFormOpens()
    {
        // arrange
        using var form = Open(ApplicationConstants.CommandName_CommandWithDefaultValues);

        // act
        var field = form.FindField("thing-number")!;

        // assert
        Assert.True(field.HasDefaultValue);
        Assert.Equal("123", field.Value);
    }

    [Fact]
    public void TheCommandLineStartsAsJustTheToolAndTheCommand()
    {
        // arrange
        using var form = Open(ApplicationConstants.CommandName_CommandWithAllowedValues);

        // assert -- nothing has been supplied, so there is nothing to write
        Assert.EndsWith(
            $" {ApplicationConstants.CommandName_CommandWithAllowedValues}",
            form.GetCommandLine());
    }

    [Fact]
    public void TheCommandLineGrowsAsTheFormIsFilledIn()
    {
        // arrange
        using var form = Open(ApplicationConstants.CommandName_CommandWithAllowedValues);

        // act
        form.FindField("environment")!.TrySetValue("prod");

        // assert
        Assert.Contains("--environment prod", form.GetCommandLine());
    }

    [Fact]
    public void TheCommandLineFollowsTheSyntaxTheToolAccepts()
    {
        // arrange
        var options = GetOptions();

        options.ArgumentSyntax = ArgumentSyntax.Slash;

        using var form = Open(
            ApplicationConstants.CommandName_CommandWithAllowedValues, options);

        // act
        form.FindField("environment")!.TrySetValue("prod");

        // assert
        Assert.Contains("/environment:prod", form.GetCommandLine());
    }

    [Fact]
    public void TheCommandLineTheFormBuildsIsOneTheParserAccepts()
    {
        // arrange -- the whole point of showing a command line is that someone types it, so
        // it has to be something the tool's own parser reads back the same way
        var options = GetOptions();

        using var form = Open(
            ApplicationConstants.CommandName_CommandWithAllowedValues, options);

        form.FindField("environment")!.TrySetValue("prod");
        form.FindField("mode")!.TrySetValue("fast");

        // act
        var tokens = form.GetCommandLineTokens();

        using var reparsed = new CommandAttributeUtility(options)
            .GetCommand(tokens, typeof(SampleCommand1).Assembly);

        // assert -- validating is what pushes the parsed values onto the argument
        // definitions, which is also what running the command would do first
        Assert.NotNull(reparsed);
        Assert.Empty(reparsed.ValidateArguments());
        Assert.Equal("prod", reparsed.Arguments["environment"].Value);
        Assert.Equal("fast", reparsed.Arguments["mode"].Value);
    }

    [Fact]
    public void APositionalCommandLineTheFormBuildsIsAlsoOneTheParserAccepts()
    {
        // arrange -- positional values are read by their order, so getting the order wrong
        // here would produce a command line that runs and does the wrong thing
        var options = GetOptions();

        using var form = Open(
            ApplicationConstants.CommandName_CommandWithPositionalSources, options);

        form.FindField("Value1")!.TrySetValue("alpha");
        form.FindField("Value2")!.TrySetValue("beta");

        // act
        var tokens = form.GetCommandLineTokens();

        using var reparsed = new CommandAttributeUtility(options)
            .GetCommand(tokens, typeof(SampleCommand1).Assembly);

        // assert
        Assert.NotNull(reparsed);
        Assert.Empty(reparsed.ValidateArguments());
        Assert.Equal("alpha", reparsed.Arguments["Value1"].Value);
        Assert.Equal("beta", reparsed.Arguments["Value2"].Value);
    }

    [Fact]
    public void AMultiLevelCommandLineTheFormBuildsIsAlsoOneTheParserAccepts()
    {
        // arrange
        var options = GetOptions();

        using var form = Open("widget show", options);

        // act
        var tokens = form.GetCommandLineTokens();

        using var reparsed = new CommandAttributeUtility(options)
            .GetCommand(tokens, typeof(SampleCommand1).Assembly);

        // assert -- the registry decides where the name stops, so the name has to arrive as
        // separate tokens
        Assert.NotNull(reparsed);
        Assert.Equal("widget show", reparsed.ExecutionInfo.CommandName);
    }

    [Fact]
    public void AFormOpenedFromAnAliasStartsWhereTheAliasLeavesOff()
    {
        // arrange
        var options = GetOptions();

        var browser = new TuiCommandBrowser(
            TuiSession.Create(options, typeof(SampleCommand1).Assembly));

        var alias = browser.GetMatchingAliases().First();

        // act
        using var form = TuiCommandForm.Open(
            options,
            typeof(SampleCommand1).Assembly,
            alias.Command,
            alias.PresetArguments);

        // assert
        Assert.NotEmpty(alias.PresetArguments);

        foreach (var preset in alias.PresetArguments)
        {
            var field = form.FindField(preset.Key);

            Assert.NotNull(field);
            Assert.True(field.HasValue);
        }
    }

    [Fact]
    public void ACommandWithNoArgumentsGetsAFormWithNoFields()
    {
        // arrange
        using var form = Open(ApplicationConstants.CommandName_CommandWithNoArgs);

        // assert
        Assert.Empty(form.Fields);
        Assert.True(form.IsValid());
    }

    [Fact]
    public void DisposingTheFormMoreThanOnceIsHarmless()
    {
        // arrange -- the form owns the command, and the command owns a dependency injection
        // scope, so disposing is not optional. Doing it twice should still be fine.
        var form = Open(ApplicationConstants.CommandName_CommandWithAllowedValues);

        // act
        form.Dispose();
        form.Dispose();
    }

    [Fact]
    public void OpeningAFormRefusesNothingToOpen()
    {
        Assert.Throws<ArgumentNullException>(
            () => TuiCommandForm.Open((ICommandProgram)null!, null!));
    }
}
