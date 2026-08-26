using Benday.CommandsFramework;
using Benday.CommandsFramework.Samples;
using Benday.CommandsFramework.Tui.Model;

namespace Benday.CommandsFramework.Tui.Tests;

/// <summary>
/// Tests the model layer. Nothing here needs a terminal, which is the whole point of having
/// the layer: if a test needs one, the logic is in the wrong place.
/// </summary>
public class TuiSessionFixture
{
    private static DefaultProgramOptions GetOptions()
    {
        return new DefaultProgramOptions
        {
            ApplicationName = "Sample Tool",
            Version = "9.9.9",
            Website = "https://www.benday.com",
            OutputProvider = new StringBuilderTextOutputProvider(),
            UsesConfiguration = false
        };
    }

    [Fact]
    public void Create_ReadsTheToolsIdentityFromItsOptions()
    {
        // arrange
        var options = GetOptions();

        // act
        var session = TuiSession.Create(options, typeof(SampleCommand1).Assembly);

        // assert
        Assert.Equal("Sample Tool", session.Title);
        Assert.Equal("9.9.9", session.Version);
        Assert.Equal("https://www.benday.com", session.Website);
    }

    [Fact]
    public void Create_FallsBackToTheAssemblyNameWhenThereIsNoApplicationName()
    {
        // arrange
        var options = GetOptions();
        options.ApplicationName = string.Empty;

        var assembly = typeof(SampleCommand1).Assembly;

        // act
        var session = TuiSession.Create(options, assembly);

        // assert -- a blank banner is worse than an unglamorous one
        Assert.Equal(assembly.GetName().Name, session.Title);
    }

    [Fact]
    public void Create_FindsTheToolsCommands()
    {
        // arrange
        var options = GetOptions();

        // act
        var session = TuiSession.Create(options, typeof(SampleCommand1).Assembly);

        // assert
        Assert.True(session.CommandCount > 0);
        Assert.Equal(session.Registry.Registrations.Count, session.CommandCount);
    }

    [Fact]
    public void Create_SharesTheRegistryTheRestOfTheProcessAlreadyBuilt()
    {
        // arrange
        var options = GetOptions();

        var existing = new CommandAttributeUtility(options)
            .GetRegistry(typeof(SampleCommand1).Assembly);

        // act
        var session = TuiSession.Create(options, typeof(SampleCommand1).Assembly);

        // assert -- opening the interface should not rescan the assembly
        Assert.Same(existing, session.Registry);
    }

    [Fact]
    public void Create_IncludesTheBuiltInConfigurationCommandsWhenTheToolUsesThem()
    {
        // arrange
        var options = GetOptions();
        options.UsesConfiguration = true;

        // act
        var session = TuiSession.Create(options, typeof(SampleCommand1).Assembly);

        // assert
        Assert.Contains(
            session.Registry.Registrations,
            x => x.Name == CommandFrameworkConstants.CommandName_CheckConfig);
    }

    [Fact]
    public void Problems_AreEmptyForAToolWithNothingWrongWithIt()
    {
        // arrange
        var options = GetOptions();

        // act
        var session = TuiSession.Create(options, typeof(SampleCommand1).Assembly);

        // assert
        Assert.False(session.HasProblems);
        Assert.Empty(session.Problems);
    }

    [Fact]
    public void FromProgram_ReadsEverythingOffTheProgram()
    {
        // arrange
        var options = GetOptions();

        var program = new DefaultProgram(options, typeof(SampleCommand1).Assembly);

        // act
        var session = TuiSession.FromProgram(program);

        // assert
        Assert.Equal("Sample Tool", session.Title);
        Assert.True(session.CommandCount > 0);
    }

    [Fact]
    public void FromProgram_RefusesNothingToShow()
    {
        Assert.Throws<ArgumentNullException>(() => TuiSession.FromProgram(null!));
    }
}
