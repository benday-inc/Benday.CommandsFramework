using Benday.CommandsFramework;
using Benday.CommandsFramework.Samples;
using Benday.CommandsFramework.Tui.Model;
using Benday.CommandsFramework.Tui.Screens;

using Spectre.Console;
using Spectre.Console.Testing;

namespace Benday.CommandsFramework.Tui.Tests;

/// <summary>
/// Tests how the list of commands is driven: what typing does, what enter does, and what
/// escape does. Against Spectre's TestConsole, so no terminal is involved.
/// </summary>
public class CommandBrowserScreenFixture
{
    private static TuiCommandBrowser GetBrowser()
    {
        var options = new DefaultProgramOptions
        {
            ApplicationName = "Sample Tool",
            OutputProvider = new StringBuilderTextOutputProvider(),
            UsesConfiguration = false
        };

        return new TuiCommandBrowser(
            TuiSession.Create(options, typeof(SampleCommand1).Assembly));
    }

    private static TestConsole GetConsole()
    {
        var console = new TestConsole();

        console.Profile.Width = 200;
        console.Profile.Height = 60;
        console.Interactive();

        return console;
    }

    [Fact]
    public async Task TypingJumpsToTheFirstCommandThatMatches()
    {
        // arrange -- a row reading 'Filter...' looks like somewhere to type, and a selection
        // list swallows anything typed at it without a word. Typing now moves the highlight,
        // which is what everyone tries first.
        var console = GetConsole();

        console.Input.PushText("greet");
        console.Input.PushKey(ConsoleKey.Enter);

        var screen = new CommandBrowserScreen(console, GetBrowser());

        // act
        var action = await screen.ShowAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(TuiScreenAction.OpenForm, action);
        Assert.NotNull(screen.SelectedCommand);
        Assert.Contains(
            "greet", screen.SelectedCommand!.PathAsString, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EnterOnTheFirstRowAsksForAFilter()
    {
        // arrange -- the filter is the one that narrows, and it is reached with enter
        var console = GetConsole();

        console.Input.PushKey(ConsoleKey.Enter);
        console.Input.PushText("greeting");
        console.Input.PushKey(ConsoleKey.Enter);

        var screen = new CommandBrowserScreen(console, GetBrowser());

        // act
        var action = await screen.ShowAsync(TestContext.Current.CancellationToken);

        // assert -- the screen asks to be drawn again, now filtered
        Assert.Equal(TuiScreenAction.Stay, action);
        Assert.Equal("greeting", screen.Browser.Filter);
        Assert.All(
            screen.Browser.GetMatchingCommands(),
            x => Assert.Contains("greet", x.PathAsString, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TheFirstRowSaysWhatItIsFor()
    {
        // arrange -- the whole problem was that it did not
        var console = GetConsole();

        console.Input.PushKey(ConsoleKey.Escape);

        var screen = new CommandBrowserScreen(console, GetBrowser());

        // act
        await screen.ShowAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Contains("press enter to narrow the list", console.Output);
        Assert.Contains("type to jump to one", console.Output);
    }

    [Fact]
    public async Task EscapeStillLeaves()
    {
        // arrange -- search takes over the keyboard, so this is worth pinning down
        var console = GetConsole();

        console.Input.PushKey(ConsoleKey.Escape);

        var screen = new CommandBrowserScreen(console, GetBrowser());

        // act
        var action = await screen.ShowAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(TuiScreenAction.Quit, action);
    }
}
