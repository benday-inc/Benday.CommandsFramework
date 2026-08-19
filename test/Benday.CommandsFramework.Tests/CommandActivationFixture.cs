using Benday.CommandsFramework.Samples;
using Benday.CommandsFramework.Samples.Services;

using Microsoft.Extensions.DependencyInjection;

namespace Benday.CommandsFramework.Tests;

/// <summary>
/// Tests how commands are created. The framework used to look for one hardcoded two argument
/// constructor, which meant adding a framework parameter would break every downstream command
/// at run time rather than at compile time -- the lookup just returned null. Commands are
/// created through ActivatorUtilities now, so they declare what they need.
/// </summary>
public class CommandActivationFixture
{
    /// <summary>
    /// A service that records how many scopes it was resolved in, so a leak shows up.
    /// </summary>
    private class ScopedService : IDisposable
    {
        public static int LiveCount;

        public ScopedService()
        {
            Interlocked.Increment(ref LiveCount);
        }

        public void Dispose()
        {
            Interlocked.Decrement(ref LiveCount);
        }
    }

    [Command(Name = "scoped-service-sample", Description = "Resolves a scoped service")]
    private class ScopedServiceCommand : Command
    {
        public ScopedServiceCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider)
            : base(info, outputProvider)
        {
        }

        public override ArgumentCollection GetArguments() => new();

        protected override Task OnExecute(CancellationToken cancellationToken)
        {
            GetRequiredService<ScopedService>();

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Registers this assembly's services without Program.cs knowing about them.
    /// </summary>
    public class TestServiceRegistrar : IServiceRegistrar
    {
        public static int RegisterCallCount;

        public void Register(IServiceCollection services)
        {
            Interlocked.Increment(ref RegisterCallCount);

            services.AddSingleton<IRegistrarProvidedService, RegistrarProvidedService>();
        }
    }

    public interface IRegistrarProvidedService
    {
        string Describe();
    }

    public class RegistrarProvidedService : IRegistrarProvidedService
    {
        public string Describe() => "registered by the registrar";
    }

    private static DefaultProgramOptions GetOptions(
        ITextOutputProvider output, Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();

        services.AddSingleton<IGreetingService, GreetingService>();
        services.AddScoped<ScopedService>();

        configure?.Invoke(services);

        return new DefaultProgramOptions
        {
            ApplicationName = "Test Sample Application",
            ConfigurationFolderName = "TestSampleApplication-Deleteable",
            OutputProvider = output,
            UsesConfiguration = false,
            ServiceCollection = services
        };
    }

    [Fact]
    public async Task Command_GetsItsDependencyInjectedIntoItsConstructor()
    {
        // arrange
        var output = new StringBuilderTextOutputProvider();
        var options = GetOptions(output);

        var utility = new CommandAttributeUtility(options);

        // act
        using var command = utility.GetCommand(
            [ApplicationConstants.CommandName_ConstructorInjection, "/name:Ben"],
            typeof(SampleCommand1).Assembly);

        // assert
        var typed = Assert.IsType<SampleConstructorInjectionCommand>(command);

        await typed.ExecuteAsync(TestContext.Current.CancellationToken);

        Assert.Contains("Ben", typed.Greeting);
    }

    [Fact]
    public async Task Command_DisposesItsServiceScope()
    {
        // arrange -- nothing used to dispose a command, so the scope was never released
        var output = new StringBuilderTextOutputProvider();
        var options = GetOptions(output);

        var utility = new CommandAttributeUtility(options);

        var before = ScopedService.LiveCount;

        // act
        var command = utility.GetCommand(["scoped-service-sample"], typeof(ScopedServiceCommand).Assembly);

        Assert.NotNull(command);

        await ((Command)command).ExecuteAsync(TestContext.Current.CancellationToken);

        var whileAlive = ScopedService.LiveCount;

        command.Dispose();

        // assert
        Assert.Equal(before + 1, whileAlive);
        Assert.Equal(before, ScopedService.LiveCount);
    }

    [Fact]
    public async Task Program_DisposesTheCommandItRan()
    {
        // arrange
        var output = new StringBuilderTextOutputProvider();
        var options = GetOptions(output);

        var program = new DefaultProgram(options, typeof(ScopedServiceCommand).Assembly);

        var before = ScopedService.LiveCount;

        // act
        await program.RunAsync(["scoped-service-sample"], TestContext.Current.CancellationToken);

        // assert -- create, activate, run, dispose
        Assert.Equal(before, ScopedService.LiveCount);
    }

    [Fact]
    public async Task NestedCommand_SharesTheCallersScope()
    {
        // arrange -- a call chain that is logically one operation should see one set of
        // scoped services. Giving the child its own scope made them inconsistent.
        var output = new StringBuilderTextOutputProvider();
        var options = GetOptions(output);

        var utility = new CommandAttributeUtility(options);

        var before = ScopedService.LiveCount;

        using var command = utility.GetCommand(
            [ApplicationConstants.CommandName_CallsOtherCommands, "/names:Alice,Bob"],
            typeof(SampleCommand1).Assembly);

        Assert.NotNull(command);

        // act
        await ((Command)command).ExecuteAsync(TestContext.Current.CancellationToken);

        // assert -- three commands ran and there is still only the one scope
        Assert.Equal(before, ScopedService.LiveCount);
    }

    [Fact]
    public void Schema_SurvivesACommandWhoseDependencyIsNotRegistered()
    {
        // arrange -- the schema path instantiates every command in the tool, so one command
        // with an unregistered dependency must not take down the whole --json dump. This is
        // safe because GetArguments() cannot depend on injected state anyway: CommandBase's
        // constructor calls it, before any derived field has been assigned.
        var options = new DefaultProgramOptions
        {
            ApplicationName = "Test Sample Application",
            ConfigurationFolderName = "TestSampleApplication-Deleteable",
            OutputProvider = new StringBuilderTextOutputProvider(),
            UsesConfiguration = false,
            ServiceCollection = new ServiceCollection()
        };

        var utility = new CommandAttributeUtility(options);

        // act
        var usages = utility.GetAllCommandUsages(typeof(SampleCommand1).Assembly);

        // assert
        var injected = usages.Single(
            x => x.Name == ApplicationConstants.CommandName_ConstructorInjection);

        Assert.Contains(injected.Arguments, x => x.Name == "name");
        Assert.Contains(usages, x => x.Name == ApplicationConstants.CommandName_Command1);
    }

    [Fact]
    public async Task RunPath_SaysSoWhenADependencyIsNotRegistered()
    {
        // arrange -- running a command is where a missing registration should be loud
        var options = new DefaultProgramOptions
        {
            ApplicationName = "Test Sample Application",
            ConfigurationFolderName = "TestSampleApplication-Deleteable",
            OutputProvider = new StringBuilderTextOutputProvider(),
            UsesConfiguration = false,
            ServiceCollection = new ServiceCollection()
        };

        var utility = new CommandAttributeUtility(options);

        // act & assert
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Task.FromResult(utility.GetCommand(
                [ApplicationConstants.CommandName_ConstructorInjection, "/name:Ben"],
                typeof(SampleCommand1).Assembly)));

        Assert.Contains("IGreetingService", actual.Message);
    }

    [Fact]
    public void ServiceRegistrars_AreDiscoveredByTheRegistry()
    {
        // act
        var registry = CommandRegistry.BuildFromTypes([typeof(TestServiceRegistrar)]);

        // assert
        Assert.Contains(typeof(TestServiceRegistrar), registry.ServiceRegistrarTypes);
    }

    [Fact]
    public async Task ServiceRegistrars_RunBeforeTheProviderIsBuilt()
    {
        // arrange -- registrations are sealed at BuildServiceProvider(), so a hook that ran
        // any later would compile, run, and silently do nothing
        var output = new StringBuilderTextOutputProvider();

        var before = TestServiceRegistrar.RegisterCallCount;

        // act
        await CommandsApp
            .Create([ApplicationConstants.CommandName_Command1])
            .ConfigureOptions(o =>
            {
                o.ApplicationName = "Test Sample Application";
                o.ConfigurationFolderName = "TestSampleApplication-Deleteable";
                o.OutputProvider = output;
                o.UsesConfiguration = false;
            })
            .RunAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.True(TestServiceRegistrar.RegisterCallCount > before);
    }

    [Fact]
    public async Task ObsoleteDependencyInjectionCommand_StillResolvesServices()
    {
        // arrange
        var output = new StringBuilderTextOutputProvider();
        var options = GetOptions(output);

        var utility = new CommandAttributeUtility(options);

        // act
        using var command = utility.GetCommand(
            ["greet", "/name:Ben"],
            typeof(SampleCommand1).Assembly);

        Assert.NotNull(command);

        var result = await ((Command)command).ExecuteAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.True(result.IsSuccess);
    }
}
