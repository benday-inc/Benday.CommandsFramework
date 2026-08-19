using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

/// <summary>
/// Tests multi-level command names: 'mytool widget list' rather than 'mytool widgetlist'.
/// The group is declared explicitly rather than derived from Category, because categories
/// hold display strings like "Widget Management" that nobody would type.
/// </summary>
public class MultiLevelCommandFixture
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

    private static async Task<string> Run(params string[] args)
    {
        var output = new StringBuilderTextOutputProvider();

        var program = new DefaultProgram(GetOptions(output), typeof(SampleCommand1).Assembly);

        await program.RunAsync(args, TestContext.Current.CancellationToken);

        return output.GetOutput();
    }

    [Fact]
    public async Task GroupedCommand_RunsFromItsFullName()
    {
        // act
        var output = await Run(
            ApplicationConstants.CommandGroup_Widget,
            ApplicationConstants.CommandName_WidgetList,
            "/filter:blue");

        // assert
        Assert.Contains("** SUCCESS **", output);
        Assert.Contains("filter: 'blue'", output);
    }

    [Fact]
    public async Task GroupedCommand_ArgumentsAfterTheNameAreParsed()
    {
        // act -- the registry decides where the name stops and the arguments start
        var output = await Run(
            ApplicationConstants.CommandGroup_Widget,
            ApplicationConstants.CommandName_WidgetShow,
            "/name:sprocket");

        // assert
        Assert.Contains("widget: sprocket", output);
    }

    [Fact]
    public async Task GroupedCommand_StillRunsFromItsOldFlatNameAsAnAlias()
    {
        // act -- this is how an existing tool adopts groups without breaking scripts
        var output = await Run(ApplicationConstants.CommandAlias_ShowWidget, "/name:sprocket");

        // assert
        Assert.Contains("widget: sprocket", output);
    }

    [Fact]
    public async Task GroupedCommand_ShowsItsFullNameInUsage()
    {
        // act
        var output = await Run(
            ApplicationConstants.CommandGroup_Widget,
            ApplicationConstants.CommandName_WidgetShow,
            ArgumentFrameworkConstants.ArgumentHelpString);

        // assert
        Assert.Contains(
            $"Command name: {ApplicationConstants.CommandGroup_Widget} " +
            $"{ApplicationConstants.CommandName_WidgetShow}",
            output);
    }

    [Fact]
    public async Task CommandList_ShowsTheGroupAsPartOfTheName()
    {
        // act
        var output = await Run();

        // assert
        Assert.Contains(
            $"{ApplicationConstants.CommandGroup_Widget} {ApplicationConstants.CommandName_WidgetList}",
            output);
    }

    [Fact]
    public async Task GroupWithNoCommandAfterIt_IsNotACommand()
    {
        // act
        var output = await Run(ApplicationConstants.CommandGroup_Widget);

        // assert -- a group is not a command, so this is an invalid command name
        Assert.Contains($"Invalid command name '{ApplicationConstants.CommandGroup_Widget}'", output);
    }

    [Fact]
    public void Resolution_IsGreedyLongestFirst()
    {
        // arrange -- a two segment name has to win over a one segment name that happens to
        // match the first token
        var registry = CommandRegistry.BuildFromTypes(
            [typeof(GreedyResolutionCommands.Flat), typeof(GreedyResolutionCommands.Grouped)]);

        // act
        var twoSegments = registry.Resolve(["thing", "list", "/arg:value"]);
        var oneSegment = registry.Resolve(["thing", "/arg:value"]);

        // assert
        Assert.NotNull(twoSegments);
        Assert.Equal(typeof(GreedyResolutionCommands.Grouped), twoSegments.Registration.CommandType);
        Assert.Single(twoSegments.RemainingTokens);

        Assert.NotNull(oneSegment);
        Assert.Equal(typeof(GreedyResolutionCommands.Flat), oneSegment.Registration.CommandType);
        Assert.Single(oneSegment.RemainingTokens);
    }

    [Fact]
    public void Registration_ReportsItsPath()
    {
        // arrange
        var registry = CommandRegistry.Build(
            GetOptions(new StringBuilderTextOutputProvider()), typeof(SampleCommand1).Assembly);

        // act
        var grouped = registry.Find(
            $"{ApplicationConstants.CommandGroup_Widget} {ApplicationConstants.CommandName_WidgetList}");

        var flat = registry.Find(ApplicationConstants.CommandName_Command1);

        // assert
        Assert.NotNull(grouped);
        Assert.Equal(
            [ApplicationConstants.CommandGroup_Widget, ApplicationConstants.CommandName_WidgetList],
            grouped.Path);
        Assert.Equal(ApplicationConstants.CommandGroup_Widget, grouped.Group);
        Assert.Equal(ApplicationConstants.CommandName_WidgetList, grouped.Name);

        Assert.NotNull(flat);
        Assert.Single(flat.Path);
        Assert.Equal(string.Empty, flat.Group);
    }

    [Fact]
    public async Task Schema_CarriesTheGroup()
    {
        // act
        var output = await Run(ArgumentFrameworkConstants.ArgumentJson);

        using var document = System.Text.Json.JsonDocument.Parse(output);

        var commands = document.RootElement.GetProperty("Commands").EnumerateArray().ToList();

        // assert
        var grouped = commands.Single(
            x => x.GetProperty("Name").GetString() == ApplicationConstants.CommandName_WidgetList);

        Assert.Equal(
            ApplicationConstants.CommandGroup_Widget, grouped.GetProperty("Group").GetString());

        var flat = commands.Single(
            x => x.GetProperty("Name").GetString() == ApplicationConstants.CommandName_Command1);

        Assert.Equal(string.Empty, flat.GetProperty("Group").GetString());
    }
}

/// <summary>
/// Commands used by the greedy resolution test. They only ever appear in a registry built
/// from these two types on purpose.
/// </summary>
internal static class GreedyResolutionCommands
{
    [Command(Name = "thing")]
    internal class Flat : Command
    {
        public Flat(CommandExecutionInfo info, ITextOutputProvider outputProvider)
            : base(info, outputProvider) { }

        public override ArgumentCollection GetArguments() => new();

        protected override Task OnExecute(CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    [Command(Group = "thing", Name = "list")]
    internal class Grouped : Command
    {
        public Grouped(CommandExecutionInfo info, ITextOutputProvider outputProvider)
            : base(info, outputProvider) { }

        public override ArgumentCollection GetArguments() => new();

        protected override Task OnExecute(CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
