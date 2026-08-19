using System.Reflection;

namespace Benday.CommandsFramework.Tests;

/// <summary>
/// Tests the one line bootstrap. Create&lt;TCommand&gt;(args) takes a type purely as an
/// assembly marker, which every consumer has to have explained to them; these overloads
/// default the assembly to the entry assembly and the name, version and website to that
/// assembly's own metadata.
/// </summary>
public class CommandsAppBootstrapFixture
{
    public const string CommandName = "bootstrap-sample";
    public const string ExecutedMarker = "** BOOTSTRAP SAMPLE RAN **";

    /// <summary>
    /// A command in the entry assembly, which during a test run is the test executable.
    /// </summary>
    [Command(Name = CommandName, Description = "Command used by the bootstrap tests")]
    public class BootstrapSampleCommand : Command
    {
        public BootstrapSampleCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider)
            : base(info, outputProvider)
        {
        }

        public override ArgumentCollection GetArguments()
        {
            return new ArgumentCollection();
        }

        protected override Task OnExecute(CancellationToken cancellationToken)
        {
            WriteLine(ExecutedMarker);

            return Task.CompletedTask;
        }
    }

    private static Assembly EntryAssembly =>
        Assembly.GetEntryAssembly() ??
            throw new InvalidOperationException("No entry assembly in this test run.");

    [Fact]
    public async Task Create_WithoutATypeArgument_DiscoversCommandsInTheEntryAssembly()
    {
        // arrange
        var output = new StringBuilderTextOutputProvider();

        await // act
        CommandsApp
            .Create([CommandName])
            .ConfigureOptions(options =>
            {
                options.ApplicationName = "Bootstrap Test App";
                options.OutputProvider = output;
                options.UsesConfiguration = false;
            })
            .RunAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Contains(ExecutedMarker, output.GetOutput());
    }

    [Fact]
    public async Task WithAppInfoFromAssembly_FillsInNameAndVersion()
    {
        // arrange
        DefaultProgramOptions? captured = null;

        await // act
        CommandsApp
            .Create([CommandName])
            .WithAppInfoFromAssembly()
            .ConfigureOptions(options =>
            {
                options.OutputProvider = new StringBuilderTextOutputProvider();
                options.UsesConfiguration = false;
                captured = options;
            })
            .RunAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.NotNull(captured);
        Assert.False(string.IsNullOrWhiteSpace(captured.ApplicationName));
        Assert.False(string.IsNullOrWhiteSpace(captured.Version));
        Assert.StartsWith("v", captured.Version);

        // the name comes from the entry assembly, which is this test executable
        Assert.Equal(EntryAssembly.GetName().Name, captured.ApplicationName);
    }

    [Fact]
    public async Task WithAppInfoFromAssembly_DoesNotOverwriteValuesThatWereAlreadySet()
    {
        // arrange
        DefaultProgramOptions? captured = null;

        await // act
        CommandsApp
            .Create([CommandName])
            .WithAppInfo("Explicit Name", "v9.9.9", "https://www.example.com")
            .WithAppInfoFromAssembly()
            .ConfigureOptions(options =>
            {
                options.OutputProvider = new StringBuilderTextOutputProvider();
                options.UsesConfiguration = false;
                captured = options;
            })
            .RunAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.NotNull(captured);
        Assert.Equal("Explicit Name", captured.ApplicationName);
        Assert.Equal("v9.9.9", captured.Version);
        Assert.Equal("https://www.example.com", captured.Website);
    }

    [Fact]
    public async Task Version_HasTheSourceRevisionSuffixTrimmed()
    {
        // arrange -- the SDK appends "+<commit sha>" to the informational version
        var informational = EntryAssembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        DefaultProgramOptions? captured = null;

        await // act
        CommandsApp
            .Create([CommandName])
            .WithAppInfoFromAssembly()
            .ConfigureOptions(options =>
            {
                options.OutputProvider = new StringBuilderTextOutputProvider();
                options.UsesConfiguration = false;
                captured = options;
            })
            .RunAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.NotNull(captured);
        Assert.DoesNotContain("+", captured.Version);

        if (string.IsNullOrWhiteSpace(informational) == false)
        {
            var expected = informational.Split('+')[0];

            Assert.Equal($"v{expected}", captured.Version);
        }
    }

    [Fact]
    public async Task UsageHeader_SkipsValuesThatWereNeverConfigured()
    {
        // arrange -- a bootstrapped tool usually has no website in its assembly metadata,
        // and an empty value used to print as a blank line
        var output = new StringBuilderTextOutputProvider();

        await // act
        CommandsApp
            .Create([])
            .ConfigureOptions(options =>
            {
                options.ApplicationName = "Bootstrap Test App";
                options.Version = string.Empty;
                options.Website = string.Empty;
                options.OutputProvider = output;
                options.UsesConfiguration = false;
            })
            .RunAsync(TestContext.Current.CancellationToken);

        // assert
        var lines = output.GetOutput()
            .Split(Environment.NewLine)
            .TakeWhile(x => x.StartsWith("Available commands") == false)
            .ToList();

        Assert.Contains("Bootstrap Test App", lines);
        Assert.Single(lines, x => string.IsNullOrWhiteSpace(x) == false);
    }
}
