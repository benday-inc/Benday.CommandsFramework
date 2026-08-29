using Benday.CommandsFramework.Samples;
using Benday.CommandsFramework.Samples.Services;

using Microsoft.Extensions.DependencyInjection;

namespace Benday.CommandsFramework.Tests;

/// <summary>
/// Tests that the service provider is built once and shared by every command rather than
/// being rebuilt for each command instance. Rebuilding it per command gives each command
/// its own set of singletons, which is wrong as soon as one command runs another.
/// </summary>
public class SharedServiceProviderFixture
{
    /// <summary>
    /// Singleton service that counts how many instances have been created.
    /// </summary>
    private class InstanceCountingService
    {
        public static int InstanceCount;

        public InstanceCountingService()
        {
            Interlocked.Increment(ref InstanceCount);
        }
    }

    [Command(Name = "counting-command")]
#pragma warning disable CS0618 // the obsolete base class is deliberate here
    private class CountingCommand : DependencyInjectionCommand
#pragma warning restore CS0618
    {
        public CountingCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider)
            : base(info, outputProvider)
        {
        }

        public InstanceCountingService? Service { get; private set; }

        protected override Task OnExecute(CancellationToken cancellationToken)
        {
            Service = GetRequiredService<InstanceCountingService>();

            return Task.CompletedTask;
        }
    }

    private static DefaultProgramOptions GetOptions(ITextOutputProvider outputProvider)
    {
        var services = new ServiceCollection();

        services.AddSingleton<InstanceCountingService>();
        services.AddSingleton<IGreetingService, GreetingService>();

        return new DefaultProgramOptions
        {
            ApplicationName = "Test Sample Application",
            ConfigurationFolderName = "TestSampleApplication-Deleteable",
            OutputProvider = outputProvider,
            UsesConfiguration = false,
            ServiceCollection = services
        };
    }

    [Fact]
    public async Task SingletonIsSharedAcrossCommandsThatShareProgramOptions()
    {
        // arrange
        InstanceCountingService.InstanceCount = 0;

        var outputProvider = new StringBuilderTextOutputProvider();
        var options = GetOptions(outputProvider);

        var first = new CountingCommand(
            new CommandExecutionInfo { Request = new CommandCallRequest("counting-command"), Options = options },
            outputProvider);

        var second = new CountingCommand(
            new CommandExecutionInfo { Request = new CommandCallRequest("counting-command"), Options = options },
            outputProvider);

        // act
        await first.ExecuteAsync(TestContext.Current.CancellationToken);
        await second.ExecuteAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.NotNull(first.Service);
        Assert.Same(first.Service, second.Service);
        Assert.Equal(1, InstanceCountingService.InstanceCount);
    }

    [Fact]
    public async Task ServiceProviderIsBuiltOnceAndCachedOnTheProgramOptions()
    {
        // arrange
        var outputProvider = new StringBuilderTextOutputProvider();
        var options = GetOptions(outputProvider);

        Assert.Null(options.ServiceProvider);

        var command = new CountingCommand(
            new CommandExecutionInfo { Request = new CommandCallRequest("counting-command"), Options = options },
            outputProvider);

        // act
        await command.ExecuteAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.NotNull(options.ServiceProvider);
    }

    [Fact]
    public async Task DisposingACommandDoesNotBreakTheSharedProvider()
    {
        // arrange
        var outputProvider = new StringBuilderTextOutputProvider();
        var options = GetOptions(outputProvider);

        var first = new CountingCommand(
            new CommandExecutionInfo { Request = new CommandCallRequest("counting-command"), Options = options },
            outputProvider);

        await first.ExecuteAsync(TestContext.Current.CancellationToken);
        first.Dispose();

        var second = new CountingCommand(
            new CommandExecutionInfo { Request = new CommandCallRequest("counting-command"), Options = options },
            outputProvider);

        // act
        await second.ExecuteAsync(TestContext.Current.CancellationToken);

        // assert
        // disposing one command disposes its own scope but leaves the shared provider alone
        Assert.NotNull(second.Service);
    }

    [Fact]
    public async Task MissingServiceCollectionStillReportsAHelpfulError()
    {
        // arrange
        var outputProvider = new StringBuilderTextOutputProvider();

        var options = new DefaultProgramOptions
        {
            ApplicationName = "Test Sample Application",
            OutputProvider = outputProvider,
            UsesConfiguration = false,
            ServiceCollection = null
        };

        var command = new CountingCommand(
            new CommandExecutionInfo { Request = new CommandCallRequest("counting-command"), Options = options },
            outputProvider);

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => command.ExecuteAsync(TestContext.Current.CancellationToken));

        // assert
        Assert.Contains("service collection was not populated", exception.Message);
        Assert.Contains("check Program.cs", exception.Message);
    }
}
