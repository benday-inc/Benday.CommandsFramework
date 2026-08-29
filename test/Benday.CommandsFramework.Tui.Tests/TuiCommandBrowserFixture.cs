using Benday.CommandsFramework;
using Benday.CommandsFramework.Samples;
using Benday.CommandsFramework.Tui.Model;

namespace Benday.CommandsFramework.Tui.Tests;

/// <summary>
/// Tests the command browser's model: what appears, in what order, and how it is arranged.
/// No terminal involved and no command instantiated.
/// </summary>
public class TuiCommandBrowserFixture
{
    private static TuiCommandBrowser GetSystemUnderTest(bool usesConfiguration = true)
    {
        var options = new DefaultProgramOptions
        {
            ApplicationName = "Sample Tool",
            OutputProvider = new StringBuilderTextOutputProvider(),
            UsesConfiguration = usesConfiguration
        };

        return new TuiCommandBrowser(
            TuiSession.Create(options, typeof(SampleCommand1).Assembly));
    }

    [Fact]
    public void AnEmptyFilterShowsEverything()
    {
        // arrange
        var browser = GetSystemUnderTest();

        // act
        var matching = browser.GetMatchingCommands();

        // assert
        Assert.Equal(browser.AllCommands.Count, matching.Count);
    }

    [Fact]
    public void AFilterNarrowsTheList()
    {
        // arrange
        var browser = GetSystemUnderTest();

        // act
        browser.Filter = "configuration";

        var matching = browser.GetMatchingCommands();

        // assert
        Assert.NotEmpty(matching);
        Assert.True(matching.Count < browser.AllCommands.Count);
        Assert.Contains(matching, x => x.Name == CommandFrameworkConstants.CommandName_CheckConfig);
    }

    [Fact]
    public void AFilterSearchesTheDescriptionAsWellAsTheName()
    {
        // arrange -- the user might remember what a command does rather than what it is called
        var browser = GetSystemUnderTest();

        // act
        browser.Filter = "widgets";

        // assert
        Assert.Contains(browser.GetMatchingCommands(), x => x.PathAsString == "widget list");
    }

    [Fact]
    public void AFilterDoesNotMatchLettersScatteredThroughADescription()
    {
        // arrange -- a description long enough to be useful contains almost any four letters
        // somewhere in order, so matching prose loosely matches nearly everything. On a real
        // tool 'list' found 50 commands out of 77 this way, and the eight actually called
        // 'list...' were buried among them.
        var browser = GetSystemUnderTest();

        // act
        browser.Filter = "list";

        var matching = browser.GetMatchingCommands();

        // assert -- 'widget list' is called that; 'discoverycommand' merely has an l, an i, an
        // s and a t scattered through a sentence
        Assert.Contains(matching, x => x.PathAsString == "widget list");
        Assert.DoesNotContain(
            matching,
            x => x.Name == ApplicationConstants.CommandName_CommandWithDiscovery);
    }

    [Fact]
    public void AFilterStillMatchesLettersScatteredThroughAName()
    {
        // arrange -- a name is short and people type an abbreviation of one, which is the case
        // loose matching is for
        var browser = GetSystemUnderTest();

        // act
        browser.Filter = "wl";

        // assert
        Assert.Contains(browser.GetMatchingCommands(), x => x.PathAsString == "widget list");
    }

    [Fact]
    public void WhatIsFoundByNameOutranksWhatIsFoundByDescription()
    {
        // arrange -- someone who types a word is looking for the command called that first
        var browser = GetSystemUnderTest();

        // act
        browser.Filter = "widget";

        var matching = browser.GetMatchingCommands();

        // assert
        Assert.NotEmpty(matching);
        Assert.StartsWith("widget", matching[0].PathAsString, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AFilteredTreePutsTheBestMatchUnderTheFirstHeading()
    {
        // arrange -- headings ordered alphabetically regardless of the ranking put the best
        // match under whichever heading sorted first, which on a tool with many categories
        // means the command the user typed for is off the first screen
        var browser = GetSystemUnderTest();

        browser.Filter = "widget";

        // act
        var tree = browser.GetTree();
        var best = browser.GetMatchingCommands()[0];

        // assert
        Assert.NotEmpty(tree);
        Assert.Contains(
            tree[0].Groups.SelectMany(x => x.Commands),
            x => x.PathAsString == best.PathAsString);
    }

    [Fact]
    public void AnUnfilteredTreeIsStillArrangedAlphabeticallyByHeading()
    {
        // arrange -- ranking only applies when there is something to rank by
        var browser = GetSystemUnderTest();

        // act
        var names = browser.GetTree().Select(x => x.Name).ToList();

        // assert
        Assert.Equal(names.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(), names);
    }

    [Fact]
    public void AFilterSearchesAliasesToo()
    {
        // arrange
        var browser = GetSystemUnderTest();

        // act -- 'showwidget' is the old flat name kept as an alias
        browser.Filter = "showwidget";

        // assert
        Assert.Contains(browser.GetMatchingCommands(), x => x.PathAsString == "widget show");
    }

    [Fact]
    public void AFilterThatMatchesNothingLeavesNothing()
    {
        // arrange
        var browser = GetSystemUnderTest();

        // act
        browser.Filter = "zzzzzzzz";

        // assert
        Assert.True(browser.HasNothingToShow);
        Assert.Empty(browser.GetMatchingCommands());
    }

    [Fact]
    public void TheBuiltInConfigurationCommandsCanBeLeftOut()
    {
        // arrange
        var browser = GetSystemUnderTest();

        // act
        browser.IncludeBuiltInCommands = false;

        // assert
        Assert.DoesNotContain(browser.GetMatchingCommands(), x => x.IsBuiltIn);
        Assert.Contains(browser.AllCommands, x => x.IsBuiltIn);
    }

    [Fact]
    public void TheTreeGroupsCommandsByCategory()
    {
        // arrange
        var browser = GetSystemUnderTest();

        // act
        var tree = browser.GetTree();

        // assert
        Assert.Contains(tree, x => x.Name == CommandFrameworkConstants.CategoryName_Configuration);
        Assert.All(tree, category => Assert.True(category.CommandCount > 0));
    }

    [Fact]
    public void TheTreeNestsAMultiLevelCommandUnderItsGroup()
    {
        // arrange -- 'widget list' and 'widget show' are typed as two tokens, so they belong
        // under 'widget' rather than appearing as two unrelated names
        var browser = GetSystemUnderTest();

        // act
        var group = browser.GetTree()
            .SelectMany(x => x.Groups)
            .Single(x => x.Name == "widget");

        // assert
        Assert.True(group.HasGroup);
        Assert.Contains(group.Commands, x => x.Name == "list");
        Assert.Contains(group.Commands, x => x.Name == "show");
    }

    [Fact]
    public void AFlatCommandSitsInAGroupWithNoName()
    {
        // arrange
        var browser = GetSystemUnderTest();

        // act
        var groups = browser.GetTree().SelectMany(x => x.Groups).ToList();

        // assert
        Assert.Contains(groups, x => x.HasGroup == false);
    }

    [Fact]
    public void AnAliasThatSuppliesArgumentValuesGetsItsOwnSection()
    {
        // arrange -- a preset is a different thing from a rename, and the console usage
        // output separates them for the same reason
        var browser = GetSystemUnderTest();

        // act
        var aliases = browser.GetMatchingAliases();

        // assert
        Assert.NotEmpty(aliases);
        Assert.All(aliases, x => Assert.NotEmpty(x.PresetArguments));
    }

    [Fact]
    public void APlainRenameIsShownNextToTheCommandNameInstead()
    {
        // arrange
        var browser = GetSystemUnderTest();

        // act
        var command = browser.AllCommands.Single(x => x.PathAsString == "widget show");

        // assert
        Assert.Contains("showwidget", command.PlainAliases);
        Assert.DoesNotContain(browser.GetMatchingAliases(), x => x.Name == "showwidget");
    }

    [Fact]
    public void ACommandInsideAGroupIsLabelledWithOnlyItsOwnName()
    {
        // arrange -- the group is already the heading above it
        var browser = GetSystemUnderTest();

        var command = browser.AllCommands.Single(x => x.PathAsString == "widget list");

        // assert
        Assert.Equal("list", command.GetLabel(insideGroup: true));
        Assert.Equal("widget list", command.GetLabel(insideGroup: false));
    }

    [Fact]
    public void ACommandWithAliasesIsLabelledTheWayTheConsoleListLabelsIt()
    {
        // arrange
        var browser = GetSystemUnderTest();

        var command = browser.AllCommands
            .Single(x => x.PathAsString == "command-with-a-long-name");

        // assert
        Assert.Equal(
            "command-with-a-long-name (mc, mycmd)", command.GetLabel(insideGroup: false));
    }

    [Fact]
    public void BuildingTheBrowserInstantiatesNoCommands()
    {
        // arrange -- opening the interface should cost what --complete costs, not what
        // --json costs. --json instantiates every command in the tool.
        var options = new DefaultProgramOptions
        {
            ApplicationName = "Sample Tool",
            OutputProvider = new StringBuilderTextOutputProvider(),
            UsesConfiguration = false
        };

        CountingCommand.ConstructorCount = 0;

        var registry = CommandRegistry.BuildFromTypes([typeof(CountingCommand)]);

        options.CommandRegistry = registry;

        // act
        var browser = new TuiCommandBrowser(
            TuiSession.Create(options, typeof(SampleCommand1).Assembly));

        _ = browser.GetTree();
        _ = browser.GetMatchingCommands();

        // assert
        Assert.Equal(0, CountingCommand.ConstructorCount);
    }

    [Command(Name = "counting", Category = "Counting",
        Description = "Records how many times it is created")]
    private class CountingCommand : Command
    {
        public static int ConstructorCount;

        public CountingCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider)
            : base(info, outputProvider)
        {
            ConstructorCount++;
        }

        public override ArgumentCollection GetArguments() => new();

        protected override Task OnExecute(CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
