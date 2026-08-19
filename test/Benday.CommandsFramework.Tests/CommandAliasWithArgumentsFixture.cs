using System.Reflection;

using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

/// <summary>
/// Tests for aliases that supply argument values. The values are injected as though they
/// had been typed on the command line, so the existing command line over config over
/// default order of precedence applies without any extra rules.
/// </summary>
public class CommandAliasWithArgumentsFixture
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

    [Fact]
    public void AliasSuppliesItsArgumentValues()
    {
        // act
        var command = SystemUnderTest.GetCommand(
            Utilities.GetStringArray(ApplicationConstants.CommandAlias_DeployProd),
            SampleAssembly);

        // assert
        Assert.NotNull(command);
        Assert.Equal("production", command.ExecutionInfo.Arguments["environment"]);
        Assert.Equal(string.Empty, command.ExecutionInfo.Arguments["verbose"]);
    }

    [Fact]
    public void AliasResolvesToTheRealCommandName()
    {
        // act
        var command = SystemUnderTest.GetCommand(
            Utilities.GetStringArray(ApplicationConstants.CommandAlias_DeployProd),
            SampleAssembly);

        // assert
        Assert.NotNull(command);
        Assert.Equal(ApplicationConstants.CommandName_Deploy, command.ExecutionInfo.CommandName);
    }

    [Fact]
    public async Task RunningTheAliasAppliesTheArgumentValues()
    {
        // arrange
        var program = new DefaultProgram(ProgramOptions, SampleAssembly);

        await // act
        program.RunAsync(Utilities.GetStringArray(ApplicationConstants.CommandAlias_DeployProd), TestContext.Current.CancellationToken);

        // assert
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);

        Assert.Contains("** SUCCESS **", output);
        Assert.Contains("environment: production", output);
        Assert.Contains("verbose: True", output);
    }

    [Fact]
    public async Task SecondAliasSuppliesItsOwnValues()
    {
        // arrange
        var program = new DefaultProgram(ProgramOptions, SampleAssembly);

        await // act
        program.RunAsync(Utilities.GetStringArray(ApplicationConstants.CommandAlias_DeployDev), TestContext.Current.CancellationToken);

        // assert
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);

        Assert.Contains("environment: development", output);

        // this alias does not set verbose, so the argument keeps its own behavior
        Assert.Contains("verbose: False", output);
    }

    [Fact]
    public async Task CommandLineWinsOverTheAlias()
    {
        // arrange
        var program = new DefaultProgram(ProgramOptions, SampleAssembly);

        await // act
        // the alias sets environment to production, but staging is typed explicitly
        program.RunAsync(Utilities.GetStringArray(
            ApplicationConstants.CommandAlias_DeployProd,
            "/environment:staging"), TestContext.Current.CancellationToken);

        // assert
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);

        Assert.Contains("environment: staging", output);
    }

    [Fact]
    public async Task ArgumentDefaultsStillApplyUnderAnAlias()
    {
        // arrange
        var program = new DefaultProgram(ProgramOptions, SampleAssembly);

        await // act
        program.RunAsync(Utilities.GetStringArray(ApplicationConstants.CommandAlias_DeployProd), TestContext.Current.CancellationToken);

        // assert
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);

        // 'thing' is not supplied by the alias or the command line, so its default applies
        Assert.Contains("thing: the-usual-thing", output);
    }

    [Fact]
    public async Task RunningTheRealCommandNameIsUnaffectedByTheAliases()
    {
        // arrange
        var program = new DefaultProgram(ProgramOptions, SampleAssembly);

        await // act
        program.RunAsync(Utilities.GetStringArray(
            ApplicationConstants.CommandName_Deploy,
            "/environment:staging"), TestContext.Current.CancellationToken);

        // assert
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);

        Assert.Contains("environment: staging", output);
        Assert.Contains("verbose: False", output);
    }

    [Fact]
    public void GetCommandAliases_IncludesBothKindsOfAlias()
    {
        // act
        var aliases = SystemUnderTest.GetCommandAliases(SampleAssembly);

        // assert
        var plain = Assert.Single(aliases, x => x.Alias == "mc");
        Assert.Equal(ApplicationConstants.CommandName_CommandWithCommandNameAliases, plain.CommandName);
        Assert.False(plain.HasArguments);

        var withArgs = Assert.Single(aliases, x => x.Alias == ApplicationConstants.CommandAlias_DeployProd);
        Assert.Equal(ApplicationConstants.CommandName_Deploy, withArgs.CommandName);
        Assert.True(withArgs.HasArguments);
        Assert.Equal("production", withArgs.Arguments["environment"]);
    }

    [Fact]
    public void GetAllCommandUsages_ReportsAliasesThatSupplyArguments()
    {
        // the --json schema is what cmdui and other tooling read, so aliases that supply
        // argument values have to show up there and stay distinguishable from plain renames

        // act
        var usages = SystemUnderTest.GetAllCommandUsages(SampleAssembly);

        // assert
        var deploy = Assert.Single(usages, x => x.Name == ApplicationConstants.CommandName_Deploy);

        Assert.Empty(deploy.Aliases);
        Assert.Equal(2, deploy.CommandAliases.Count);

        var prod = Assert.Single(deploy.CommandAliases,
            x => x.Alias == ApplicationConstants.CommandAlias_DeployProd);

        Assert.Equal("production", prod.Arguments["environment"]);
        Assert.Equal(string.Empty, prod.Arguments["verbose"]);
        Assert.Equal("Deploy to production with verbose output", prod.Description);
    }

    [Fact]
    public void GetAllCommandUsages_PlainAliasesAreNotReportedAsArgumentSupplyingAliases()
    {
        // act
        var usages = SystemUnderTest.GetAllCommandUsages(SampleAssembly);

        // assert
        var withPlainAliases = Assert.Single(usages,
            x => x.Name == ApplicationConstants.CommandName_CommandWithCommandNameAliases);

        Assert.Equal(new[] { "mc", "mycmd" }, withPlainAliases.Aliases);
        Assert.Empty(withPlainAliases.CommandAliases);
    }

    [Fact]
    public void GetCommandAlias_ReturnsNullForARealCommandName()
    {
        // act
        var actual = SystemUnderTest.GetCommandAlias(
            SampleAssembly, ApplicationConstants.CommandName_Deploy);

        // assert
        Assert.Null(actual);
    }

    [Fact]
    public async Task DisplayUsage_ListsAliasesThatSupplyArguments()
    {
        // arrange
        var program = new DefaultProgram(ProgramOptions, SampleAssembly);

        await // act
        program.RunAsync([], TestContext.Current.CancellationToken);

        // assert
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);

        Assert.Contains("Command aliases:", output);
        Assert.Contains(ApplicationConstants.CommandAlias_DeployProd, output);
        Assert.Contains("Deploy to production with verbose output", output);
        Assert.Contains("/environment:production", output);
        Assert.Contains("/verbose", output);
    }

    [Fact]
    public void ArgumentEntryWithNoValueBecomesAnEmptyValue()
    {
        // arrange
        var attribute = new CommandAliasAttribute("whatever", "flagarg", "keyed=value");

        // act
        var actual = attribute.GetArgumentValues();

        // assert
        Assert.Equal(string.Empty, actual["flagarg"]);
        Assert.Equal("value", actual["keyed"]);
    }

    [Fact]
    public void ArgumentValueCanContainAnEqualsSign()
    {
        // arrange
        var attribute = new CommandAliasAttribute("whatever", "connection=server=localhost;db=test");

        // act
        var actual = attribute.GetArgumentValues();

        // assert
        Assert.Equal("server=localhost;db=test", actual["connection"]);
    }

    [Fact]
    public void EmptyArgumentEntriesAreIgnored()
    {
        // arrange
        var attribute = new CommandAliasAttribute("whatever", "", "  ", "real=value");

        // act
        var actual = attribute.GetArgumentValues();

        // assert
        Assert.Single(actual);
        Assert.Equal("value", actual["real"]);
    }
}
