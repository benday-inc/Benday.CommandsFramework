using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

/// <summary>
/// Tests finding an argument's value rather than requiring it to be supplied. Finding nothing
/// and finding several are different situations and get different messages -- telling them
/// apart is most of what this is for.
/// </summary>
public class SingleMatchDiscoveryFixture : IDisposable
{
    private readonly string _Directory;

    public SingleMatchDiscoveryFixture()
    {
        _Directory = Path.Combine(Path.GetTempPath(), $"discovery-{Guid.NewGuid():N}");

        Directory.CreateDirectory(_Directory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_Directory) == true)
        {
            Directory.Delete(_Directory, true);
        }

        GC.SuppressFinalize(this);
    }

    private string CreateFile(string name)
    {
        var path = Path.Combine(_Directory, name);

        File.WriteAllText(path, "{}");

        return path;
    }

    /// <summary>
    /// A command that searches a directory the test picks, so nothing has to change the
    /// process's working directory.
    /// </summary>
    [Command(Name = "discovery-test", Description = "Finds its input")]
    private class DiscoveryTestCommand : Command
    {
        public static string SearchDirectory = string.Empty;
        public static bool IsRequired = true;

        public DiscoveryTestCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider)
            : base(info, outputProvider)
        {
        }

        public override ArgumentCollection GetArguments()
        {
            var args = new ArgumentCollection();

            var argument = args.AddFile("inputfile")
                .DiscoverSingleMatch("*.json", SearchDirectory)
                .WithDescription("File to read");

            if (IsRequired == true)
            {
                argument.AsRequired();
            }
            else
            {
                argument.AsNotRequired();
            }

            return args;
        }

        public string InputFileUsed { get; private set; } = string.Empty;

        protected override Task OnExecute(CancellationToken cancellationToken)
        {
            InputFileUsed = Arguments.GetStringValue("inputfile");

            return Task.CompletedTask;
        }
    }

    private async Task<(CommandResult Result, DiscoveryTestCommand Command, string Output)> Run(
        bool required = true, params string[] args)
    {
        DiscoveryTestCommand.SearchDirectory = _Directory;
        DiscoveryTestCommand.IsRequired = required;

        var output = new StringBuilderTextOutputProvider();

        var executionInfo = new ArgumentCollectionFactory().Parse(
            Utilities.GetStringArray(new[] { "discovery-test" }.Concat(args).ToArray()));

        using var command = new DiscoveryTestCommand(executionInfo, output);

        var result = await command.ExecuteAsync(TestContext.Current.CancellationToken);

        return (result, command, output.GetOutput());
    }

    [Fact]
    public async Task ExactlyOneMatch_IsUsed()
    {
        // arrange
        var expected = CreateFile("only.json");

        // act
        var (result, command, _) = await Run();

        // assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expected, command.InputFileUsed);
    }

    [Fact]
    public async Task NoMatches_SaysNothingWasFound()
    {
        // act
        var (result, _, output) = await Run();

        // assert
        Assert.Equal(CommandExecutionStatus.ValidationFailed, result.Status);
        Assert.Contains(
            result.ValidationFailures, x => x.Kind == ValidationFailureKind.DiscoveryFailed);
        Assert.Contains("no files matching '*.json' were found", output);
    }

    [Fact]
    public async Task SeveralMatches_SaysWhichOnesAndAsksForAChoice()
    {
        // arrange -- a different situation from finding nothing, and a different message
        CreateFile("first.json");
        CreateFile("second.json");

        // act
        var (result, _, output) = await Run();

        // assert
        Assert.Equal(CommandExecutionStatus.ValidationFailed, result.Status);
        Assert.Contains("2 files match", output);
        Assert.Contains("first.json", output);
        Assert.Contains("second.json", output);
        Assert.Contains("to choose one", output);
    }

    [Fact]
    public async Task SuppliedValue_IsNotOverriddenByTheSearch()
    {
        // arrange -- several would otherwise be an error
        CreateFile("first.json");
        CreateFile("second.json");

        var chosen = Path.Combine(_Directory, "second.json");

        // act
        var (result, command, _) = await Run(true, $"/inputfile:{chosen}");

        // assert
        Assert.True(result.IsSuccess);
        Assert.Equal(chosen, command.InputFileUsed);
    }

    [Fact]
    public async Task OptionalArgument_FindingNothingIsNotAFailure()
    {
        // act -- an optional argument that finds nothing is simply not supplied
        var (result, command, _) = await Run(required: false);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, command.InputFileUsed);
    }

    [Fact]
    public async Task OptionalArgument_SeveralMatchesIsStillAFailure()
    {
        // arrange -- "I found four, which one?" is a real question even for an optional
        // argument, because the command cannot pick
        CreateFile("first.json");
        CreateFile("second.json");

        // act
        var (result, _, output) = await Run(required: false);

        // assert
        Assert.Equal(CommandExecutionStatus.ValidationFailed, result.Status);
        Assert.Contains("2 files match", output);
    }

    [Fact]
    public void DiscoverSingleMatch_ThrowsOnAnArgumentWithNothingToSearchFor()
    {
        // arrange
        var args = new ArgumentCollection();

        // act & assert
        Assert.Throws<InvalidOperationException>(
            () => args.AddString("text").DiscoverSingleMatch("*.json"));

        Assert.Throws<InvalidOperationException>(
            () => args.AddInt32("count").DiscoverSingleMatch("*.json"));
    }

    [Fact]
    public void Schema_SaysWhichArgumentsCanBeFound()
    {
        // arrange
        var args = new ArgumentCollection();

        args.AddFile("found").DiscoverSingleMatch("*.sln");
        args.AddFile("supplied");

        // assert
        Assert.True(args["found"].IsDiscoverable);
        Assert.Equal("*.sln", args["found"].DiscoveryPattern);

        Assert.False(args["supplied"].IsDiscoverable);
        Assert.Equal(string.Empty, args["supplied"].DiscoveryPattern);
    }

    [Fact]
    public void Discovery_DoesNotRunWhenTheSchemaIsBuilt()
    {
        // arrange -- globbing here would mean --json hit the disk once per command in the
        // tool, every time anything asked for the schema
        var options = new DefaultProgramOptions
        {
            ApplicationName = "Test Sample Application",
            ConfigurationFolderName = "TestSampleApplication-Deleteable",
            OutputProvider = new StringBuilderTextOutputProvider(),
            UsesConfiguration = false
        };

        var utility = new CommandAttributeUtility(options);

        // act
        var usages = utility.GetAllCommandUsages(typeof(SampleCommand1).Assembly);

        // assert -- the sample's file argument is declared discoverable and still has no value
        var command = usages.Single(
            x => x.Name == ApplicationConstants.CommandName_CommandWithDiscovery);

        var argument = command.Arguments[SampleCommandWithDiscovery.ArgumentName_InputFile];

        Assert.True(argument.IsDiscoverable);
        Assert.False(argument.HasValue);
    }

    [Fact]
    public void SampleCommands_HaveNoArgumentProblems()
    {
        // arrange -- an optional positional declared before a required one shifts every
        // position after it and the command reads the wrong values with no error at all
        var options = new DefaultProgramOptions
        {
            ApplicationName = "Test Sample Application",
            ConfigurationFolderName = "TestSampleApplication-Deleteable",
            OutputProvider = new StringBuilderTextOutputProvider(),
            UsesConfiguration = true
        };

        var utility = new CommandAttributeUtility(options);

        // act
        var problems = utility.GetArgumentProblems(typeof(SampleCommand1).Assembly);

        // assert
        Assert.Empty(problems);
    }
}
