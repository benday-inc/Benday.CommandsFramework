using System.Text.Json;

using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

/// <summary>
/// Tests rules about combinations of arguments. Validation used to be able to express a
/// failure only as an IArgument, and the proof that this was too narrow was UnknownArgument,
/// a fake argument invented to stand for a failure that was not about one. A rule has no
/// single argument to blame either.
/// </summary>
public class ArgumentRuleFixture
{
    private static async Task<string> Run(params string[] args)
    {
        var output = new StringBuilderTextOutputProvider();

        var executionInfo = new ArgumentCollectionFactory().Parse(
            Utilities.GetStringArray(
                new[] { ApplicationConstants.CommandName_CommandWithRules }.Concat(args).ToArray()));

        using var command = new SampleCommandWithRules(executionInfo, output);

        await command.ExecuteAsync(TestContext.Current.CancellationToken);

        return output.GetOutput();
    }

    [Fact]
    public async Task ExactlyOneOf_ZeroSuppliedSaysOneIsRequired()
    {
        var output = await Run();

        Assert.DoesNotContain("** SUCCESS **", output);
        Assert.Contains("One of 'token', 'windowsauth' is required.", output);
    }

    [Fact]
    public async Task ExactlyOneOf_SeveralSuppliedSaysOnlyOne()
    {
        // zero and several are different mistakes and deserve different messages
        var output = await Run("/token:abc", "/windowsauth");

        Assert.DoesNotContain("** SUCCESS **", output);
        Assert.Contains("Only one of", output);
    }

    [Fact]
    public async Task ExactlyOneOf_OneSuppliedPasses()
    {
        var output = await Run("/token:abc");

        Assert.Contains("** SUCCESS **", output);
    }

    [Fact]
    public async Task RequiredTogether_HalfAPairFails()
    {
        var output = await Run("/token:abc", "/username:ben");

        Assert.Contains("'username' requires 'password'.", output);
    }

    [Fact]
    public async Task RequiredTogether_NeitherIsFine()
    {
        var output = await Run("/token:abc");

        Assert.Contains("** SUCCESS **", output);
    }

    [Fact]
    public async Task RequiredTogether_BothIsFine()
    {
        var output = await Run("/token:abc", "/username:ben", "/password:secret");

        Assert.Contains("** SUCCESS **", output);
    }

    [Fact]
    public async Task When_RequiresWhatTheConditionCallsFor()
    {
        var output = await Run("/token:abc", "/mode:advanced");

        Assert.Contains("'mode' is 'advanced', so 'level' is required.", output);
    }

    [Fact]
    public async Task When_ForbidsWhatTheConditionRulesOut()
    {
        var output = await Run("/token:abc", "/mode:simple", "/level:3");

        Assert.Contains("'mode' is 'simple', so 'level' cannot be used.", output);
    }

    [Fact]
    public async Task When_DoesNotApplyWhenTheConditionDoesNotHold()
    {
        // no mode at all, so neither conditional rule is in play
        var output = await Run("/token:abc", "/level:3");

        Assert.Contains("** SUCCESS **", output);
    }

    [Fact]
    public async Task Usage_ListsTheRules()
    {
        // rules are not visible on any single argument, so without this the only way to
        // discover them is to get one wrong
        var output = await Run(ArgumentFrameworkConstants.ArgumentHelpString);

        Assert.Contains("** RULES **", output);
        Assert.Contains("Supply exactly one of 'token', 'windowsauth'.", output);
    }

    [Fact]
    public async Task RulesAreNotCheckedWhileIndividualValuesAreStillInvalid()
    {
        // arrange -- 'mode' has allowed values, so this fails at the argument level first.
        // A rule about the combination has nothing useful to say yet.
        var output = await Run("/token:abc", "/mode:nonsense");

        // assert
        Assert.Contains("mode is not valid or missing", output);
        Assert.DoesNotContain("so 'level' is required", output);
    }

    [Fact]
    public void MutuallyExclusive_AllowsNoneAndOne()
    {
        // arrange
        var args = new ArgumentCollection();
        args.AddString("first").AsNotRequired();
        args.AddString("second").AsNotRequired();
        args.MutuallyExclusive("first", "second");

        var rule = Assert.Single(args.Rules);

        // act & assert
        Assert.Null(rule.Check(args));

        args["first"].TrySetValue("a");
        Assert.Null(rule.Check(args));

        args["second"].TrySetValue("b");
        Assert.NotNull(rule.Check(args));
    }

    [Fact]
    public void AtLeastOneOf_NeedsOne()
    {
        // arrange
        var args = new ArgumentCollection();
        args.AddString("first").AsNotRequired();
        args.AddString("second").AsNotRequired();
        args.AtLeastOneOf("first", "second");

        var rule = Assert.Single(args.Rules);

        // act & assert
        Assert.NotNull(rule.Check(args));

        args["second"].TrySetValue("b");
        Assert.Null(rule.Check(args));

        args["first"].TrySetValue("a");
        Assert.Null(rule.Check(args));
    }

    [Fact]
    public void BooleanFlagThatIsFalse_DoesNotCountAsSupplied()
    {
        // arrange -- '/windowsauth:false' is not a choice of windows auth
        var args = new ArgumentCollection();
        args.AddString("token").AsNotRequired();
        args.AddBoolean("windowsauth").AsNotRequired().AllowEmptyValue();
        args.ExactlyOneOf("token", "windowsauth");

        var rule = Assert.Single(args.Rules);

        args["windowsauth"].TrySetValue("false");

        // act & assert
        Assert.NotNull(rule.Check(args));

        args["windowsauth"].TrySetValue("true");
        Assert.Null(rule.Check(args));
    }

    [Fact]
    public void When_WithNoValueMeansWheneverTheArgumentIsSupplied()
    {
        // arrange
        var args = new ArgumentCollection();
        args.AddBoolean("publish").AsNotRequired().AllowEmptyValue();
        args.AddString("target").AsNotRequired();
        args.When("publish").Require("target");

        var rule = Assert.Single(args.Rules);

        // act & assert
        Assert.Null(rule.Check(args));

        args["publish"].TrySetValue("true");
        Assert.NotNull(rule.Check(args));

        args["target"].TrySetValue("production");
        Assert.Null(rule.Check(args));
    }

    [Fact]
    public void When_RequireAndForbidBuildOneRule()
    {
        // arrange
        var args = new ArgumentCollection();
        args.AddString("mode").AsNotRequired();
        args.AddString("needed").AsNotRequired();
        args.AddString("banned").AsNotRequired();

        // act
        args.When("mode", "advanced").Require("needed").Forbid("banned");

        // assert -- one rule, not two
        var rule = Assert.IsType<ConditionalRule>(Assert.Single(args.Rules));

        Assert.Equal(["needed"], rule.RequiredNames);
        Assert.Equal(["banned"], rule.ForbiddenNames);
    }

    [Fact]
    public void ValidationFailure_KnowsWhatKindItIs()
    {
        // arrange
        var args = new ArgumentCollection();
        args.AddString("thing").AsRequired();

        var argumentFailure = ValidationFailure.ForArgument(args["thing"]);
        var unknownFailure = ValidationFailure.ForUnknownArgument("mystery");
        var ruleFailure = ValidationFailure.ForRule(
            new ExactlyOneOfRule(["a", "b"]), "pick one");

        // assert
        Assert.Equal(ValidationFailureKind.InvalidArgument, argumentFailure.Kind);
        Assert.NotNull(argumentFailure.Argument);

        Assert.Equal(ValidationFailureKind.UnknownArgument, unknownFailure.Kind);
        Assert.Null(unknownFailure.Argument);
        Assert.Equal(["mystery"], unknownFailure.ArgumentNames);

        Assert.Equal(ValidationFailureKind.RuleViolated, ruleFailure.Kind);
        Assert.Null(ruleFailure.Argument);
        Assert.Equal(["a", "b"], ruleFailure.ArgumentNames);
    }

    [Fact]
    public async Task Schema_CarriesTheRules()
    {
        // arrange -- declarative so a form can apply them as it is filled in
        var output = new StringBuilderTextOutputProvider();

        var options = new DefaultProgramOptions
        {
            ApplicationName = "Test Sample Application",
            ConfigurationFolderName = "TestSampleApplication-Deleteable",
            OutputProvider = output,
            UsesConfiguration = false
        };

        var program = new DefaultProgram(options, typeof(SampleCommand1).Assembly);

        // act
        await program.RunAsync(
            [ArgumentFrameworkConstants.ArgumentJson], TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(output.GetResultOutput());

        var command = document.RootElement.GetProperty("Commands")
            .EnumerateArray()
            .Single(x => x.GetProperty("Name").GetString() ==
                ApplicationConstants.CommandName_CommandWithRules);

        var rules = command.GetProperty("Rules").EnumerateArray().ToList();

        // assert
        Assert.Equal(4, rules.Count);

        var exactlyOne = rules.Single(x => x.GetProperty("RuleType").GetString() == "ExactlyOneOf");

        Assert.Equal(
            ["token", "windowsauth"],
            exactlyOne.GetProperty("ArgumentNames").EnumerateArray().Select(x => x.GetString()));

        var conditional = rules.First(x => x.GetProperty("RuleType").GetString() == "When");

        Assert.Equal("mode", conditional.GetProperty("WhenArgumentName").GetString());
        Assert.False(string.IsNullOrEmpty(conditional.GetProperty("Description").GetString()));
    }
}
