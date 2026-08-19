using System.Reflection;

using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

/// <summary>
/// Tests for the command registry. Before it existed, seven methods each swept
/// assembly.GetTypes() with their own filter and three places branched on UsesConfiguration
/// to decide which assembly to look in. Everything now goes through one build.
/// </summary>
public class CommandRegistryFixture
{
    private static Assembly SampleAssembly => typeof(SampleCommand1).Assembly;

    private static Assembly FrameworkAssembly => typeof(CommandRegistry).Assembly;

    private static DefaultProgramOptions GetOptions(bool usesConfiguration)
    {
        return new DefaultProgramOptions
        {
            ApplicationName = "Test Sample Application",
            ConfigurationFolderName = "TestSampleApplication-Deleteable",
            OutputProvider = new StringBuilderTextOutputProvider(),
            UsesConfiguration = usesConfiguration
        };
    }

    [Fact]
    public void Build_FindsTheSampleCommands()
    {
        // act
        var registry = CommandRegistry.Build(GetOptions(false), SampleAssembly);

        // assert
        Assert.NotEmpty(registry.Registrations);
        Assert.Contains(
            registry.Registrations,
            x => x.Name == ApplicationConstants.CommandName_Command1);
    }

    [Fact]
    public void Build_SkipsTypesThatAreNotRunnableAndReportsThem()
    {
        // arrange -- the test assembly has a [Command] class that is not a CommandBase
        var registry = CommandRegistry.Build(GetOptions(false), typeof(CommandRegistryFixture).Assembly);

        // assert
        Assert.DoesNotContain(registry.Registrations, x => x.Name == "not-a-commandbase");
        Assert.Contains(registry.Problems, x => x.Contains("not-a-commandbase"));
    }

    [Fact]
    public void BuiltInCommands_AreOrdinaryRegistrationsWhenConfigurationIsUsed()
    {
        // act
        var registry = CommandRegistry.Build(GetOptions(true), SampleAssembly);

        // assert -- they are in the same list as everything else, just marked as built in
        var setConfig = registry.Find(CommandFrameworkConstants.CommandName_SetConfig);

        Assert.NotNull(setConfig);
        Assert.True(setConfig.IsBuiltIn);
        Assert.Equal(FrameworkAssembly, setConfig.SourceAssembly);

        var sampleCommand = registry.Find(ApplicationConstants.CommandName_Command1);

        Assert.NotNull(sampleCommand);
        Assert.False(sampleCommand.IsBuiltIn);
    }

    [Fact]
    public void BuiltInCommands_AreAbsentWhenConfigurationIsNotUsed()
    {
        // act
        var registry = CommandRegistry.Build(GetOptions(false), SampleAssembly);

        // assert
        Assert.Null(registry.Find(CommandFrameworkConstants.CommandName_SetConfig));
    }

    [Fact]
    public void CommandNames_AreMatchedWithoutRegardToCase()
    {
        // arrange -- v5: the registry uses ArgumentCollection.ArgumentNameComparer, so
        // command names finally follow the rule argument names have followed since v4.18
        var registry = CommandRegistry.Build(GetOptions(false), SampleAssembly);

        // act & assert
        Assert.NotNull(registry.Find(ApplicationConstants.CommandName_Command1.ToUpperInvariant()));
        Assert.NotNull(registry.Find(ApplicationConstants.CommandName_Command1.ToLowerInvariant()));
    }

    [Fact]
    public void Find_ResolvesAnAlias()
    {
        // arrange
        var registry = CommandRegistry.Build(GetOptions(false), SampleAssembly);

        // act
        var byAlias = registry.Find("mc");

        // assert
        Assert.NotNull(byAlias);
        Assert.Equal(
            ApplicationConstants.CommandName_CommandWithCommandNameAliases, byAlias.Name);
    }

    [Fact]
    public void Resolve_ReturnsTheRemainingTokens()
    {
        // arrange
        var registry = CommandRegistry.Build(GetOptions(false), SampleAssembly);

        // act
        var resolution = registry.Resolve(
            [ApplicationConstants.CommandName_Command1, "/arg1:value", "/arg2:other"]);

        // assert
        Assert.NotNull(resolution);
        Assert.Equal(ApplicationConstants.CommandName_Command1, resolution.Registration.Name);
        Assert.Equal(2, resolution.RemainingTokens.Count);
        Assert.False(resolution.WasMatchedByAlias);
    }

    [Fact]
    public void Resolve_CarriesThePresetArgumentsFromAnAlias()
    {
        // arrange
        var registry = CommandRegistry.Build(GetOptions(false), SampleAssembly);

        // act
        var resolution = registry.Resolve([ApplicationConstants.CommandAlias_DeployProd]);

        // assert
        Assert.NotNull(resolution);
        Assert.Equal(ApplicationConstants.CommandName_Deploy, resolution.Registration.Name);
        Assert.NotEmpty(resolution.PresetArguments);
        Assert.True(resolution.WasMatchedByAlias);
        Assert.Equal(ApplicationConstants.CommandAlias_DeployProd, resolution.MatchedAs);
    }

    [Fact]
    public void Resolve_ReturnsNullWhenNothingMatches()
    {
        // arrange
        var registry = CommandRegistry.Build(GetOptions(false), SampleAssembly);

        // act & assert
        Assert.Null(registry.Resolve(["no-such-command"]));
        Assert.Null(registry.Resolve([]));
    }

    [Fact]
    public void SampleCommands_HaveNoProblems()
    {
        // act
        var registry = CommandRegistry.Build(GetOptions(true), SampleAssembly);

        // assert -- the problem list is what used to be a method nothing called
        Assert.Empty(registry.Problems);
    }

    [Fact]
    public void Build_ThrowsWhenTwoCommandsClaimTheSameName()
    {
        // arrange -- the test assembly declares a command with the same name as one in the
        // samples assembly, so a registry built over both is ambiguous. Either assembly on
        // its own is fine, which is why this only shows up when they are combined.
        var assemblies = new[] { typeof(CommandRegistryFixture).Assembly, SampleAssembly };

        // act & assert -- picking a winner silently is how a command ends up running the
        // wrong code
        var actual = Assert.Throws<KnownException>(() => CommandRegistry.Build(assemblies));

        Assert.Contains(ConflictingCommands.DuplicateOfASampleCommandName, actual.Message);
        Assert.Contains("claimed by more than one command", actual.Message);
    }

    [Fact]
    public void Build_ThrowsWhenTwoCommandsClaimTheSameAlias()
    {
        // arrange -- 'mc' belongs to a samples command, and the test assembly has one that
        // also claims it. Built from the two types directly, so the duplicate command name
        // that the other test relies on does not get in the way first.
        var types = new[]
        {
            typeof(SampleCommandWithCommandNameAliases),
            typeof(ConflictingCommands.ClaimsATakenAlias)
        };

        // act & assert
        var actual = Assert.Throws<KnownException>(() => CommandRegistry.BuildFromTypes(types));

        Assert.Contains(ConflictingCommands.DuplicateOfASampleAlias, actual.Message);
        Assert.Contains("is ambiguous", actual.Message);
    }

    [Fact]
    public void Build_ReportsAnAliasThatCanNeverBeUsed()
    {
        // arrange -- an alias that is also a real command name is not ambiguous, because the
        // real name wins. It just can never be reached, so it is reported rather than thrown.
        var registry = CommandRegistry.Build([typeof(CommandRegistryFixture).Assembly]);

        // assert
        Assert.Contains(
            registry.Problems,
            x => x.Contains(ConflictingCommands.AliasThatIsAlsoACommandName) &&
                 x.Contains("can never be used"));
    }

    [Fact]
    public void CachedRegistry_IsReusedForTheSameQuestion()
    {
        // arrange
        var options = GetOptions(false);
        var utility = new CommandAttributeUtility(options);

        // act
        var first = utility.GetRegistry(SampleAssembly);
        var second = utility.GetRegistry(SampleAssembly);

        // assert
        Assert.Same(first, second);
        Assert.Same(first, options.CommandRegistry);
    }

    [Fact]
    public void CachedRegistry_IsRebuiltWhenConfigurationIsTurnedOn()
    {
        // arrange -- UsesConfiguration decides whether the built-in commands are registered,
        // so a registry built with it off cannot answer a question asked with it on
        var options = GetOptions(false);
        var utility = new CommandAttributeUtility(options);

        var withoutBuiltIns = utility.GetRegistry(SampleAssembly);

        // act
        options.UsesConfiguration = true;

        var withBuiltIns = utility.GetRegistry(SampleAssembly);

        // assert
        Assert.NotSame(withoutBuiltIns, withBuiltIns);
        Assert.Null(withoutBuiltIns.Find(CommandFrameworkConstants.CommandName_SetConfig));
        Assert.NotNull(withBuiltIns.Find(CommandFrameworkConstants.CommandName_SetConfig));
    }
}

/// <summary>
/// Commands that collide with the ones in the samples assembly. Each is harmless inside this
/// assembly on its own -- the collision only exists in a registry built over both, which is
/// exactly the situation a tool is in when it adds an assembly of commands to another one's.
/// </summary>
internal static class ConflictingCommands
{
    /// <summary>
    /// Same name as a command in the samples assembly.
    /// </summary>
    public const string DuplicateOfASampleCommandName = ApplicationConstants.CommandName_Command1;

    /// <summary>
    /// Same alias as a command in the samples assembly.
    /// </summary>
    public const string DuplicateOfASampleAlias = "mc";

    /// <summary>
    /// An alias that is also the name of a command in this assembly, so the alias can never
    /// be reached.
    /// </summary>
    public const string AliasThatIsAlsoACommandName = "shadowed-alias";

    [Command(Name = DuplicateOfASampleCommandName)]
    internal class ClaimsASampleCommandName : SynchronousCommand
    {
        public ClaimsASampleCommandName(
            CommandExecutionInfo info, ITextOutputProvider outputProvider)
            : base(info, outputProvider) { }

        public override ArgumentCollection GetArguments() => new();

        protected override void OnExecute() { }
    }

    [Command(Name = AliasThatIsAlsoACommandName)]
    internal class ClaimsAName : SynchronousCommand
    {
        public ClaimsAName(CommandExecutionInfo info, ITextOutputProvider outputProvider)
            : base(info, outputProvider) { }

        public override ArgumentCollection GetArguments() => new();

        protected override void OnExecute() { }
    }

    [Command(
        Name = "claims-a-taken-alias",
        Aliases = [DuplicateOfASampleAlias, AliasThatIsAlsoACommandName])]
    internal class ClaimsATakenAlias : SynchronousCommand
    {
        public ClaimsATakenAlias(CommandExecutionInfo info, ITextOutputProvider outputProvider)
            : base(info, outputProvider) { }

        public override ArgumentCollection GetArguments() => new();

        protected override void OnExecute() { }
    }
}
