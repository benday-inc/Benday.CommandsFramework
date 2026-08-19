using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

/// <summary>
/// Tests dynamic shell completion. A generated static script goes stale the moment the tool is
/// updated, so the shell asks the tool instead -- which is only affordable because this path
/// stays cheap: completing a command name reads the registry and instantiates nothing.
/// </summary>
public class ShellCompletionFixture
{
    private static DefaultProgramOptions GetOptions(StringBuilderTextOutputProvider output)
    {
        return new DefaultProgramOptions
        {
            ApplicationName = "Test Sample Application",
            ConfigurationFolderName = "TestSampleApplication-Deleteable",
            OutputProvider = output,
            UsesConfiguration = false
        };
    }

    private static List<CompletionCandidate> Complete(string commandLine)
    {
        var options = GetOptions(new StringBuilderTextOutputProvider());

        var utility = new CommandAttributeUtility(options);

        var engine = new CompletionEngine(
            utility,
            utility.GetRegistry(typeof(SampleCommand1).Assembly),
            typeof(SampleCommand1).Assembly);

        return engine.GetCandidates(commandLine);
    }

    private static List<string> Values(string commandLine)
    {
        return [.. Complete(commandLine).Select(x => x.Value)];
    }

    [Fact]
    public void EmptyLine_OffersEveryCommand()
    {
        var values = Values("samples ");

        Assert.Contains(ApplicationConstants.CommandName_Command1, values);
        Assert.Contains(ArgumentFrameworkConstants.ArgumentJson, values);
    }

    [Fact]
    public void PartialCommandName_NarrowsToMatches()
    {
        var values = Values("samples wid");

        Assert.All(values, x => Assert.StartsWith("wid", x, StringComparison.OrdinalIgnoreCase));
        Assert.Contains("widget list", values);
    }

    [Fact]
    public void Group_IsOfferedAsWellAsItsCommands()
    {
        // 'wid<TAB>' offering nothing because a group is not itself a command would be no use
        var candidates = Complete("samples wid");

        var group = candidates.Single(x => x.Value == ApplicationConstants.CommandGroup_Widget);

        Assert.Contains("commands", group.Description);
    }

    [Fact]
    public void Aliases_AreOffered()
    {
        var candidates = Complete("samples showw");

        var alias = Assert.Single(candidates);

        Assert.Equal(ApplicationConstants.CommandAlias_ShowWidget, alias.Value);
        Assert.Contains("alias for", alias.Description);
    }

    [Fact]
    public void ResolvedCommand_OffersItsArguments()
    {
        var candidates = Complete($"samples {ApplicationConstants.CommandName_Greeting} ");

        Assert.Contains(candidates, x => x.Value == "/name:");
        Assert.Contains(candidates, x => x.Value == ArgumentFrameworkConstants.ArgumentHelpString);
    }

    [Fact]
    public void ArgumentsAlreadySupplied_AreNotOfferedAgain()
    {
        var values = Values($"samples {ApplicationConstants.CommandName_Greeting} /name:Ben ");

        Assert.DoesNotContain("/name:", values);
        Assert.Contains("/salutation:", values);
    }

    [Fact]
    public void BooleanFlag_IsOfferedWithoutAColon()
    {
        // '/verbose' is typed on its own; '/verbose:' would be wrong
        var values = Values($"samples {ApplicationConstants.CommandName_Deploy} ");

        Assert.Contains("/verbose", values);
        Assert.DoesNotContain("/verbose:", values);
    }

    [Fact]
    public void AllowedValues_AreOfferedAfterTheColon()
    {
        var values = Values(
            $"samples {ApplicationConstants.CommandName_CommandWithAllowedValues} /environment:");

        Assert.Equal(
            ["/environment:dev", "/environment:staging", "/environment:prod"], values);
    }

    [Fact]
    public void AllowedValues_NarrowToWhatIsTypedSoFar()
    {
        var values = Values(
            $"samples {ApplicationConstants.CommandName_CommandWithAllowedValues} /environment:s");

        Assert.Equal(["/environment:staging"], values);
    }

    [Fact]
    public void FileArgument_DelegatesToTheShell()
    {
        // the tool has no business enumerating the filesystem for a shell that already knows
        // how to do it, and how to quote what it finds
        var candidate = Assert.Single(Complete(
            $"samples {ApplicationConstants.CommandName_CommandWithFileAndDirectoryArgs} " +
            "/inputfile:"));

        Assert.True(candidate.IsDirective);
        Assert.StartsWith(":file:", candidate.Value);
    }

    [Fact]
    public void FileArgumentWithADiscoveryPattern_NarrowsTheDirective()
    {
        var candidate = Assert.Single(Complete(
            $"samples {ApplicationConstants.CommandName_CommandWithDiscovery} /inputfile:"));

        Assert.Equal(":file:*.json", candidate.Value);
    }

    [Fact]
    public void DirectoryArgument_DelegatesToTheShell()
    {
        var candidate = Assert.Single(Complete(
            $"samples {ApplicationConstants.CommandName_CommandWithFileAndDirectoryArgs} " +
            "/outputdir:"));

        Assert.True(candidate.IsDirective);
        Assert.Equal(":dir", candidate.Value);
    }

    [Fact]
    public void Candidate_IsWrittenAsValueTabDescription()
    {
        var withDescription = CompletionCandidate.ForValue("thing", "what it does");
        var withoutDescription = CompletionCandidate.ForValue("thing");

        Assert.Equal("thing\twhat it does", withDescription.ToString());
        Assert.Equal("thing", withoutDescription.ToString());
    }

    [Theory]
    [InlineData("mytool convert file.json", 3)]
    [InlineData("mytool convert \"two words\"", 3)]
    [InlineData("  mytool   convert  ", 2)]
    public void Tokenize_RespectsQuotesAndWhitespace(string commandLine, int expectedCount)
    {
        Assert.Equal(expectedCount, CompletionEngine.Tokenize(commandLine).Count);
    }

    [Fact]
    public void ShellFunctionName_IsSanitized()
    {
        // a tool named after its assembly has dots in it, which a shell function name cannot
        Assert.Equal(
            "Benday_CommandsFramework_Samples",
            CompletionScripts.GetShellFunctionName("Benday.CommandsFramework.Samples"));

        Assert.Equal("_9lives", CompletionScripts.GetShellFunctionName("9lives"));
    }

    [Theory]
    [InlineData("pwsh")]
    [InlineData("zsh")]
    [InlineData("bash")]
    public void EveryShell_GetsAStubThatCallsBackIntoTheTool(string shell)
    {
        var script = CompletionScripts.GetScript(shell, "mytool");

        Assert.Contains("mytool", script);
        Assert.Contains(ArgumentFrameworkConstants.ArgumentComplete, script);

        // the stub handles both directives, or paths would not complete
        Assert.Contains(":dir", script);
        Assert.Contains(":file:", script);
    }

    [Fact]
    public void UnknownShell_SaysWhichOnesThereAre()
    {
        var actual = Assert.Throws<KnownException>(
            () => CompletionScripts.GetScript("fish", "mytool"));

        Assert.Contains("pwsh", actual.Message);
    }

    [Fact]
    public async Task CompleteKeyword_WritesOneCandidatePerLine()
    {
        // arrange
        var output = new StringBuilderTextOutputProvider();

        var program = new DefaultProgram(GetOptions(output), typeof(SampleCommand1).Assembly);

        // act
        var exitCode = await program.RunAsync(
            [ArgumentFrameworkConstants.ArgumentComplete, "samples widget "],
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(CommandFrameworkConstants.ExitCode_Success, exitCode);

        var lines = output.GetResultOutput()
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.Contains(lines, x => x.StartsWith("widget list"));
    }

    [Fact]
    public async Task CompletionCommand_PrintsTheStub()
    {
        // arrange
        var output = new StringBuilderTextOutputProvider();

        var program = new DefaultProgram(GetOptions(output), typeof(SampleCommand1).Assembly);

        // act
        await program.RunAsync(
            [ArgumentFrameworkConstants.CommandCompletion,
             $"/{DefaultProgram.CompletionShellArgumentName}:bash"],
            TestContext.Current.CancellationToken);

        // assert
        Assert.Contains("complete -F", output.GetResultOutput());
    }

    [Fact]
    public async Task CompletionCommandWithNoShell_SaysWhatToAskFor()
    {
        // arrange
        var output = new StringBuilderTextOutputProvider();

        var program = new DefaultProgram(GetOptions(output), typeof(SampleCommand1).Assembly);

        // act
        await program.RunAsync(
            [ArgumentFrameworkConstants.CommandCompletion],
            TestContext.Current.CancellationToken);

        // assert
        foreach (var shell in CompletionScripts.SupportedShells)
        {
            Assert.Contains($"/{DefaultProgram.CompletionShellArgumentName}:{shell}",
                output.GetResultOutput());
        }
    }

    [Fact]
    public void CompletionKeyword_CannotBeTakenByACommand()
    {
        // --complete is not listed in usage output because it is for shells rather than for
        // people, but it is still a name a command cannot have
        Assert.Contains(ArgumentFrameworkConstants.ArgumentComplete, ReservedKeywords.AllNames);
        Assert.Contains(ArgumentFrameworkConstants.CommandCompletion, ReservedKeywords.AllNames);
    }
}
