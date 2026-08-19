using System.Reflection;

using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

/// <summary>
/// Tests for command name aliases. Aliases are resolved to the real command name at a
/// single point before the rest of the framework sees the command name, so the tests
/// here cover both resolution and the fact that everything downstream reports the real
/// command name.
/// </summary>
public class CommandNameAliasFixture
{
    private CommandAttributeUtility? _SystemUnderTest;

    private CommandAttributeUtility SystemUnderTest
    {
        get
        {
            _SystemUnderTest ??= new CommandAttributeUtility(ProgramOptions);

            return _SystemUnderTest;
        }
    }

    private DefaultProgramOptions? _ProgramOptions;

    private DefaultProgramOptions ProgramOptions
    {
        get
        {
            _ProgramOptions ??= new DefaultProgramOptions
            {
                ApplicationName = "Test Sample Application",
                Version = "1.0.0",
                Website = "https://www.benday.com",
                ConfigurationFolderName = "TestSampleApplication-Deleteable",
                OutputProvider = OutputProvider,
                UsesConfiguration = false
            };

            return _ProgramOptions;
        }
    }

    private StringBuilderTextOutputProvider? _OutputProvider;

    private StringBuilderTextOutputProvider OutputProvider
    {
        get
        {
            _OutputProvider ??= new StringBuilderTextOutputProvider();

            return _OutputProvider;
        }
    }

    private static Assembly SampleAssembly => typeof(SampleCommand1).Assembly;

    [Theory]
    [InlineData("mc")]
    [InlineData("mycmd")]
    public void ResolveCommandName_ReturnsRealNameForAlias(string alias)
    {
        // act
        var actual = SystemUnderTest.ResolveCommandName(SampleAssembly, alias);

        // assert
        Assert.Equal(ApplicationConstants.CommandName_CommandWithCommandNameAliases, actual);
    }

    [Fact]
    public void ResolveCommandName_ReturnsRealNameForRealName()
    {
        // act
        var actual = SystemUnderTest.ResolveCommandName(
            SampleAssembly, ApplicationConstants.CommandName_Command1);

        // assert
        Assert.Equal(ApplicationConstants.CommandName_Command1, actual);
    }

    [Fact]
    public void ResolveCommandName_ReturnsNullForUnknownName()
    {
        // act
        var actual = SystemUnderTest.ResolveCommandName(SampleAssembly, "no-such-command");

        // assert
        Assert.Null(actual);
    }

    [Fact]
    public void ResolveCommandName_ReturnsNullForEmptyName()
    {
        // act
        var actual = SystemUnderTest.ResolveCommandName(SampleAssembly, string.Empty);

        // assert
        Assert.Null(actual);
    }

    [Fact]
    public void ResolveCommandName_IsNotCaseSensitive()
    {
        // v5: the registry is keyed with ArgumentCollection.ArgumentNameComparer, so command
        // names and aliases follow the same rule argument names have followed since v4.18.
        // In v4 this returned null and 'MC' simply did not work.

        // act
        var actual = SystemUnderTest.ResolveCommandName(SampleAssembly, "MC");

        // assert
        Assert.Equal(ApplicationConstants.CommandName_CommandWithCommandNameAliases, actual);
    }

    [Fact]
    public void ResolveCommandName_MatchesARealNameWithoutRegardToCase()
    {
        // act
        var actual = SystemUnderTest.ResolveCommandName(
            SampleAssembly, ApplicationConstants.CommandName_Command1.ToUpperInvariant());

        // assert
        Assert.Equal(ApplicationConstants.CommandName_Command1, actual);
    }

    [Fact]
    public void GetCommand_ByAlias_ReturnsCommandWithRealCommandName()
    {
        // arrange
        var args = Utilities.GetStringArray("mc", "/message:hello there");

        // act
        var actual = SystemUnderTest.GetCommand(args, SampleAssembly);

        // assert
        Assert.NotNull(actual);
        Assert.IsType<SampleCommandWithCommandNameAliases>(actual);

        Assert.Equal(
            ApplicationConstants.CommandName_CommandWithCommandNameAliases,
            actual.ExecutionInfo.CommandName);
    }

    [Fact]
    public void GetCommand_ByAlias_KeepsArgumentValues()
    {
        // arrange
        var args = Utilities.GetStringArray("mc", "/message:hello there");

        // act
        var actual = SystemUnderTest.GetCommand(args, SampleAssembly);

        // assert
        Assert.NotNull(actual);
        Assert.Equal("hello there", actual.ExecutionInfo.Arguments["message"]);
    }

    [Fact]
    public void RunByAlias_ExecutesTheCommand()
    {
        // arrange
        var program = new DefaultProgram(ProgramOptions, SampleAssembly);

        // act
        program.Run(Utilities.GetStringArray("mc", "/message:via the alias"));

        // assert
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);

        Assert.Contains("** SUCCESS **", output);
        Assert.Contains("message: via the alias", output);

        // the command sees its real name, not the alias that was typed
        Assert.Contains(
            $"command name: {ApplicationConstants.CommandName_CommandWithCommandNameAliases}",
            output);
    }

    [Fact]
    public void RunByRealName_StillWorks()
    {
        // arrange
        var program = new DefaultProgram(ProgramOptions, SampleAssembly);

        // act
        program.Run(Utilities.GetStringArray(
            ApplicationConstants.CommandName_CommandWithCommandNameAliases,
            "/message:via the real name"));

        // assert
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);

        Assert.Contains("** SUCCESS **", output);
        Assert.Contains("message: via the real name", output);
    }

    [Fact]
    public void RunByUnknownName_ReportsInvalidCommandName()
    {
        // arrange
        var program = new DefaultProgram(ProgramOptions, SampleAssembly);

        // act
        program.Run(Utilities.GetStringArray("no-such-command"));

        // assert
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);

        Assert.Contains("Invalid command name 'no-such-command'.", output);
    }

    [Fact]
    public void RunByAlias_DoesNotMutateTheCallersArgumentArray()
    {
        // arrange
        var program = new DefaultProgram(ProgramOptions, SampleAssembly);
        var args = Utilities.GetStringArray("mc");

        // act
        program.Run(args);

        // assert
        Assert.Equal("mc", args[0]);
    }

    [Fact]
    public void DisplayUsage_ShowsAliasesNextToTheCommandName()
    {
        // arrange
        var program = new DefaultProgram(ProgramOptions, SampleAssembly);

        // act
        program.Run([]);

        // assert
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);

        Assert.Contains(
            $"{ApplicationConstants.CommandName_CommandWithCommandNameAliases} (mc, mycmd)",
            output);
    }

    [Fact]
    public void SampleCommandsHaveNoCommandNameProblems()
    {
        // act
        var problems = SystemUnderTest.GetCommandNameProblems(SampleAssembly);

        // assert
        Assert.Empty(problems);
    }

    [Fact]
    public void FrameworkCommandsHaveNoCommandNameProblems()
    {
        // arrange
        ProgramOptions.UsesConfiguration = true;

        // act
        var problems = SystemUnderTest.GetCommandNameProblems(
            typeof(CommandAttributeUtility).Assembly);

        // assert
        Assert.Empty(problems);
    }

    [Fact]
    public void GetCommandNameProblems_DetectsAliasThatCollidesWithACommandName()
    {
        // arrange
        var attributes = new List<CommandAttribute>
        {
            new() { Name = "alpha", Aliases = ["bravo"] },
            new() { Name = "bravo" }
        };

        // act
        var problems = SystemUnderTest.GetCommandNameProblems(attributes);

        // assert
        var problem = Assert.Single(problems);
        Assert.Contains("Alias 'bravo' on command 'alpha' is also the name of a command", problem);
    }

    [Fact]
    public void GetCommandNameProblems_DetectsAliasClaimedByTwoCommands()
    {
        // arrange
        var attributes = new List<CommandAttribute>
        {
            new() { Name = "alpha", Aliases = ["shared"] },
            new() { Name = "bravo", Aliases = ["shared"] }
        };

        // act
        var problems = SystemUnderTest.GetCommandNameProblems(attributes);

        // assert
        var problem = Assert.Single(problems);
        Assert.Contains("Alias 'shared' is claimed by more than one command: alpha, bravo.", problem);
    }

    [Fact]
    public void GetCommandNameProblems_DetectsDuplicateCommandNames()
    {
        // arrange
        var attributes = new List<CommandAttribute>
        {
            new() { Name = "alpha" },
            new() { Name = "alpha" }
        };

        // act
        var problems = SystemUnderTest.GetCommandNameProblems(attributes);

        // assert
        var problem = Assert.Single(problems);
        Assert.Contains("Command name 'alpha' is used by more than one command.", problem);
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("--json")]
    [InlineData("gui")]
    public void GetCommandNameProblems_DetectsAliasThatCollidesWithAReservedKeyword(string reserved)
    {
        // arrange
        var attributes = new List<CommandAttribute>
        {
            new() { Name = "alpha", Aliases = [reserved] }
        };

        // act
        var problems = SystemUnderTest.GetCommandNameProblems(attributes);

        // assert
        var problem = Assert.Single(problems);
        Assert.Contains($"Alias '{reserved}' on command 'alpha' is a reserved framework keyword", problem);
    }

    [Fact]
    public void GetCommandNameProblems_DetectsEmptyAlias()
    {
        // arrange
        var attributes = new List<CommandAttribute>
        {
            new() { Name = "alpha", Aliases = [""] }
        };

        // act
        var problems = SystemUnderTest.GetCommandNameProblems(attributes);

        // assert
        var problem = Assert.Single(problems);
        Assert.Contains("Command 'alpha' has an empty alias.", problem);
    }

    [Fact]
    public void ResolveCommandName_RealCommandNameWinsOverAnAlias()
    {
        // arrange
        var attributes = new List<CommandAttribute>
        {
            new() { Name = "alpha", Aliases = ["bravo"] },
            new() { Name = "bravo" }
        };

        // act
        var actual = SystemUnderTest.ResolveCommandName(attributes, "bravo");

        // assert
        Assert.Equal("bravo", actual);
    }

    [Fact]
    public void ResolveCommandName_ThrowsWhenAnAliasIsAmbiguous()
    {
        // arrange
        var attributes = new List<CommandAttribute>
        {
            new() { Name = "alpha", Aliases = ["shared"] },
            new() { Name = "bravo", Aliases = ["shared"] }
        };

        // act
        var exception = Assert.Throws<KnownException>(
            () => SystemUnderTest.ResolveCommandName(attributes, "shared"));

        // assert
        Assert.Contains("The alias 'shared' is ambiguous", exception.Message);
        Assert.Contains("alpha, bravo", exception.Message);
    }

    [Fact]
    public void GetAllCommandUsages_IncludesAliases()
    {
        // act
        var usages = SystemUnderTest.GetAllCommandUsages(SampleAssembly);

        // assert
        var match = Assert.Single(usages,
            x => x.Name == ApplicationConstants.CommandName_CommandWithCommandNameAliases);

        Assert.Equal(new[] { "mc", "mycmd" }, match.Aliases);
    }
}
