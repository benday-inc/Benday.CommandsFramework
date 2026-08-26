using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

/// <summary>
/// Tests the POSIX argument syntax -- '--name value', '--name=value', '--name:value', '-n
/// value' and the bare '--flag' -- alongside the deprecated '/name:value' form.
/// </summary>
/// <remarks>
/// The space separated form is the interesting one. Nothing in the two tokens '--name' and
/// 'value' says whether 'value' belongs to '--name' or is a positional argument that follows a
/// boolean flag; only the command's own argument definitions say that. So these tests mostly
/// go through CommandAttributeUtility.GetCommand(), which is where the definitions are
/// available, rather than through the parser on its own.
/// </remarks>
public class PosixArgumentSyntaxFixture
{
    private static DefaultProgramOptions GetOptions(
        ArgumentSyntax syntax = ArgumentSyntax.Both,
        StringBuilderTextOutputProvider? output = null)
    {
        return new DefaultProgramOptions
        {
            ApplicationName = "Test Sample Application",
            ConfigurationFolderName = "TestSampleApplication-Deleteable",
            OutputProvider = output ?? new StringBuilderTextOutputProvider(),
            UsesConfiguration = false,
            ArgumentSyntax = syntax
        };
    }

    /// <summary>
    /// Runs a command line through the same path the framework uses and returns the parsed
    /// argument values.
    /// </summary>
    private static Dictionary<string, string> ParseThroughGetCommand(
        string[] args,
        ArgumentSyntax syntax = ArgumentSyntax.Both,
        StringBuilderTextOutputProvider? output = null)
    {
        var utility = new CommandAttributeUtility(GetOptions(syntax, output));

        using var command = utility.GetCommand(args, typeof(SampleCommand1).Assembly);

        Assert.NotNull(command);

        return command.ExecutionInfo.Arguments;
    }

    /// <summary>
    /// The argument definitions after values have been set, which is what a command actually
    /// reads.
    /// </summary>
    private static async Task<ArgumentCollection> GetValidatedArguments(
        string[] args, ArgumentSyntax syntax = ArgumentSyntax.Both)
    {
        var utility = new CommandAttributeUtility(GetOptions(syntax));

        using var command = utility.GetCommand(args, typeof(SampleCommand1).Assembly);

        Assert.NotNull(command);

        // Validate() is protected, so the values get set the way they do in production
        await ((Command)command).ExecuteAsync(TestContext.Current.CancellationToken);

        return command.Arguments;
    }

    /// <summary>
    /// Runs a command with --help and returns everything it wrote.
    /// </summary>
    private static async Task<string> GetUsageOutput(
        string commandName, ArgumentSyntax syntax)
    {
        var output = new StringBuilderTextOutputProvider();

        var utility = new CommandAttributeUtility(GetOptions(syntax, output));

        using var command = utility.GetCommand(
            [commandName, ArgumentFrameworkConstants.ArgumentHelpString],
            typeof(SampleCommand1).Assembly);

        Assert.NotNull(command);

        await ((Command)command).ExecuteAsync(TestContext.Current.CancellationToken);

        return output.GetOutput();
    }

    [Fact]
    public void SpaceSeparatedValue_BindsToTheOption()
    {
        var arguments = ParseThroughGetCommand(
            [ApplicationConstants.CommandName_Command1, "--arg1", "hello"]);

        Assert.Equal("hello", arguments["arg1"]);
    }

    [Fact]
    public void EqualsSeparatedValue_BindsToTheOption()
    {
        var arguments = ParseThroughGetCommand(
            [ApplicationConstants.CommandName_Command1, "--arg1=hello"]);

        Assert.Equal("hello", arguments["arg1"]);
    }

    [Fact]
    public void ColonSeparatedValue_BindsToTheOption()
    {
        // System.CommandLine accepts a colon too, which makes '--arg1:hello' a one character
        // migration from the slash form
        var arguments = ParseThroughGetCommand(
            [ApplicationConstants.CommandName_Command1, "--arg1:hello"]);

        Assert.Equal("hello", arguments["arg1"]);
    }

    [Fact]
    public void BooleanFlag_DoesNotConsumeTheNextToken()
    {
        // 'verbose' allows an empty value, so it is a flag and 'leftover' belongs to whatever
        // comes next -- here, the first positional slot
        var arguments = ParseThroughGetCommand(
            [ApplicationConstants.CommandName_CommandWithPositionalSources,
             "--isThingy", "leftover"]);

        Assert.Equal(string.Empty, arguments["isThingy"]);
        Assert.Equal("leftover", arguments["POSITION_1"]);
    }

    [Fact]
    public void NonFlagArgument_ConsumesTheNextToken()
    {
        // the mirror image of the test above, and the whole reason the parser needs the
        // argument definitions
        var arguments = ParseThroughGetCommand(
            [ApplicationConstants.CommandName_CommandWithPositionalSources,
             "--thing-number", "42"]);

        Assert.Equal("42", arguments["thing-number"]);
        Assert.DoesNotContain("POSITION_1", arguments.Keys);
    }

    [Fact]
    public async Task ShortOption_BindsThroughTheArgumentAlias()
    {
        var arguments = ParseThroughGetCommand(
            [ApplicationConstants.CommandName_CommandWithAliases, "-Alias1", "abc"]);

        var validated = await GetValidatedArguments(
            [ApplicationConstants.CommandName_CommandWithAliases, "-Alias1", "abc"]);

        Assert.Equal("abc", arguments["Alias1"]);
        Assert.Equal("abc", validated["Value1"].Value);
    }

    [Fact]
    public async Task ShortOptionWithAnEqualsSign_BindsThroughTheArgumentAlias()
    {
        var validated = await GetValidatedArguments(
            [ApplicationConstants.CommandName_CommandWithAliases, "-Alias1=abc"]);

        Assert.Equal("abc", validated["Value1"].Value);
    }

    [Fact]
    public void NegativeNumber_IsAValueAndNotAnOption()
    {
        var arguments = ParseThroughGetCommand(
            [ApplicationConstants.CommandName_CommandWithPositionalSources,
             "--thing-number", "-5"]);

        Assert.Equal("-5", arguments["thing-number"]);
    }

    [Fact]
    public void UnixPath_IsAValueAndNotAnOption()
    {
        var arguments = ParseThroughGetCommand(
            [ApplicationConstants.CommandName_Command1, "--arg1", "/etc/hosts"]);

        Assert.Equal("/etc/hosts", arguments["arg1"]);
    }

    [Fact]
    public void EndOfOptionsMarker_MakesEverythingAfterItAValue()
    {
        // the only way to type a positional value that starts with a dash
        var arguments = ParseThroughGetCommand(
            [ApplicationConstants.CommandName_CommandWithPositionalSources,
             "--", "--not-an-option"]);

        Assert.Equal("--not-an-option", arguments["POSITION_1"]);
        Assert.DoesNotContain("not-an-option", arguments.Keys);
    }

    [Fact]
    public void QuotedValue_HasItsQuotesRemoved()
    {
        var arguments = ParseThroughGetCommand(
            [ApplicationConstants.CommandName_Command1, "--arg1=\"value with spaces\""]);

        Assert.Equal("value with spaces", arguments["arg1"]);
    }

    [Fact]
    public async Task OptionNames_AreMatchedWithoutRegardToCase()
    {
        var validated = await GetValidatedArguments(
            [ApplicationConstants.CommandName_Command1, "--ARG1", "hello"]);

        Assert.Equal("hello", validated["arg1"].Value);
    }

    [Fact]
    public async Task PositionalArguments_StillBindByPosition()
    {
        var validated = await GetValidatedArguments(
            [ApplicationConstants.CommandName_CommandWithPositionalSources, "first", "second"]);

        Assert.Equal("first", validated["Value1"].Value);
        Assert.Equal("second", validated["Value2"].Value);
    }

    [Fact]
    public async Task NamedOptions_DoNotShiftPositionalArguments()
    {
        var validated = await GetValidatedArguments(
            [ApplicationConstants.CommandName_CommandWithPositionalSources,
             "first", "--thing-number", "42", "second"]);

        Assert.Equal("first", validated["Value1"].Value);
        Assert.Equal("second", validated["Value2"].Value);
        Assert.Equal(42, ((Int32Argument)validated["thing-number"]).Value);
    }

    [Fact]
    public void SlashSyntax_StillParsesByDefault()
    {
        // deprecated, not removed
        var arguments = ParseThroughGetCommand(
            [ApplicationConstants.CommandName_Command1, "/arg1:hello"]);

        Assert.Equal("hello", arguments["arg1"]);
    }

    [Fact]
    public void SlashSyntax_ProducesADeprecationWarning()
    {
        var output = new StringBuilderTextOutputProvider();

        ParseThroughGetCommand(
            [ApplicationConstants.CommandName_Command1, "/arg1:hello"],
            ArgumentSyntax.Both, output);

        Assert.Contains("deprecated", output.GetStatusOutput());
        Assert.Contains("--arg1 hello", output.GetStatusOutput());
    }

    [Fact]
    public void SlashDeprecationWarning_GoesToTheDiagnosticChannel()
    {
        // a tool whose result is being piped into a JSON reader must not get a deprecation
        // notice in the middle of its output
        var output = new StringBuilderTextOutputProvider();

        ParseThroughGetCommand(
            [ApplicationConstants.CommandName_Command1, "/arg1:hello"],
            ArgumentSyntax.Both, output);

        Assert.DoesNotContain("deprecated", output.GetResultOutput());
    }

    [Fact]
    public void PosixSyntax_ProducesNoDeprecationWarning()
    {
        var output = new StringBuilderTextOutputProvider();

        ParseThroughGetCommand(
            [ApplicationConstants.CommandName_Command1, "--arg1", "hello"],
            ArgumentSyntax.Both, output);

        Assert.DoesNotContain("deprecated", output.GetStatusOutput());
    }

    [Fact]
    public void WarningCanBeTurnedOff()
    {
        var output = new StringBuilderTextOutputProvider();

        var options = GetOptions(ArgumentSyntax.Both, output);

        options.WarnOnDeprecatedArgumentSyntax = false;

        var utility = new CommandAttributeUtility(options);

        using var command = utility.GetCommand(
            [ApplicationConstants.CommandName_Command1, "/arg1:hello"],
            typeof(SampleCommand1).Assembly);

        Assert.DoesNotContain("deprecated", output.GetStatusOutput());
    }

    [Fact]
    public void SlashModeDoesNotWarn()
    {
        // a program that has deliberately selected the slash syntax has said what it wants
        var output = new StringBuilderTextOutputProvider();

        ParseThroughGetCommand(
            [ApplicationConstants.CommandName_Command1, "/arg1:hello"],
            ArgumentSyntax.Slash, output);

        Assert.DoesNotContain("deprecated", output.GetStatusOutput());
    }

    [Fact]
    public void PosixOnlyMode_TreatsASlashArgumentAsAPositionalValue()
    {
        var arguments = ParseThroughGetCommand(
            [ApplicationConstants.CommandName_CommandWithPositionalSources, "/arg1:hello"],
            ArgumentSyntax.Posix);

        Assert.DoesNotContain("arg1", arguments.Keys);
        Assert.Equal("/arg1:hello", arguments["POSITION_1"]);
    }

    [Fact]
    public void SlashOnlyMode_TreatsAPosixArgumentAsAPositionalValue()
    {
        var arguments = ParseThroughGetCommand(
            [ApplicationConstants.CommandName_CommandWithPositionalSources, "--arg1=hello"],
            ArgumentSyntax.Slash);

        Assert.DoesNotContain("arg1", arguments.Keys);
        Assert.Equal("--arg1=hello", arguments["POSITION_1"]);
    }

    [Fact]
    public void HelpKeyword_KeepsItsLiteralKey()
    {
        // every command checks for '--help' by that exact key, dashes and all. Parsing it as
        // an ordinary option would file it under 'help' and nothing would find it.
        var arguments = ParseThroughGetCommand(
            [ApplicationConstants.CommandName_Command1,
             ArgumentFrameworkConstants.ArgumentHelpString]);

        Assert.Contains(ArgumentFrameworkConstants.ArgumentHelpString, arguments.Keys);
    }

    [Fact]
    public void QuietKeyword_WorksInThePosixForm()
    {
        var arguments = ParseThroughGetCommand(
            [ApplicationConstants.CommandName_Command1, "--arg1", "hello", "--quiet"]);

        Assert.Contains(CommandFrameworkConstants.CommandArgName_QuietMode, arguments.Keys);
    }

    [Fact]
    public void TrailingOptionWithNoValue_DoesNotReadPastTheEnd()
    {
        var arguments = ParseThroughGetCommand(
            [ApplicationConstants.CommandName_Command1, "--arg1"]);

        Assert.Equal(string.Empty, arguments["arg1"]);
    }

    [Fact]
    public void TwoOptionsInARow_DoNotConsumeEachOther()
    {
        var arguments = ParseThroughGetCommand(
            [ApplicationConstants.CommandName_Command1, "--arg1", "--verbose"]);

        Assert.Equal(string.Empty, arguments["arg1"]);
        Assert.Contains("verbose", arguments.Keys);
    }

    [Fact]
    public async Task UsageOutput_RendersThePosixForm()
    {
        var text = await GetUsageOutput(
            ApplicationConstants.CommandName_Command1, ArgumentSyntax.Both);

        Assert.Contains("--arg1", text);
        Assert.DoesNotContain("/arg1:", text);
    }

    [Fact]
    public async Task UsageOutput_RendersTheSlashFormInSlashMode()
    {
        var text = await GetUsageOutput(
            ApplicationConstants.CommandName_Command1, ArgumentSyntax.Slash);

        Assert.Contains("/arg1", text);
    }

    [Fact]
    public async Task UsageOutput_ShowsAFlagWithoutAValue()
    {
        var text = await GetUsageOutput(
            ApplicationConstants.CommandName_Command1, ArgumentSyntax.Both);

        // 'verbose' is a flag, so it is typed on its own
        Assert.Contains("[--verbose]", text);
    }
}
