using Benday.CommandsFramework;
using Benday.CommandsFramework.Samples;

using Spectre.Console.Testing;

namespace Benday.CommandsFramework.Tui.Tests;

/// <summary>
/// Tests the one line a tool has to write to get a terminal interface.
/// </summary>
public class WithTuiFixture
{
    [Fact]
    public void WithTui_GivesTheToolATerminalInterface()
    {
        // arrange
        DefaultProgramOptions? options = null;

        // act
        CommandsApp
            .Create<SampleCommand1>([])
            .WithTui()
            .ConfigureOptions(x => options = x);

        // assert
        Assert.NotNull(options);
        Assert.IsType<SpectreTuiHost>(options.TuiHost);
    }

    [Fact]
    public void WithTui_ReturnsTheBuilderSoItKeepsChaining()
    {
        // arrange
        var app = CommandsApp.Create<SampleCommand1>([]);

        // act
        var returned = app.WithTui();

        // assert
        Assert.Same(app, returned);
    }

    [Fact]
    public async Task ATuiEnabledTool_RunsTheInterfaceForTheTuiKeyword()
    {
        // arrange
        var console = new TestConsole();

        console.Profile.Width = 120;

        var options = new DefaultProgramOptions
        {
            ApplicationName = "Sample Tool",
            OutputProvider = new StringBuilderTextOutputProvider(),
            UsesConfiguration = false,
            TuiHost = new SpectreTuiHost(console)
        };

        var program = new DefaultProgram(options, typeof(SampleCommand1).Assembly);

        // act -- end to end through the keyword the user actually types
        var exitCode = await program.RunAsync(
            [ArgumentFrameworkConstants.ArgumentTui], TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(CommandFrameworkConstants.ExitCode_Success, exitCode);
        Assert.Contains("Sample Tool", console.Output);
    }
}
