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
    public async Task TheListSaysHowToLeave()
    {
        // arrange -- the way out is a row at the bottom of the list, which is off the screen
        // on a tool with a lot of commands
        var console = GetConsole();

        console.Input.PushKey(ConsoleKey.Escape);

        var screen = new CommandBrowserScreen(console, GetBrowser());

        // act
        await screen.ShowAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Contains("Press esc to quit", console.Output);
    }

    [Fact]
    public async Task AListTallerThanTheWindowSaysItAtTheBottomToo()
    {
        // arrange -- the title is the first thing to scroll away when the list is taller than
        // the terminal, which is exactly when someone is looking for the way out. The
        // more-choices line is drawn at the bottom of the visible rows, and only in this case.
        var console = GetConsole();

        console.Profile.Height = 12;

        console.Input.PushKey(ConsoleKey.Escape);

        var screen = new CommandBrowserScreen(console, GetBrowser());

        // act
        await screen.ShowAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Contains("esc quits", console.Output);
    }

    [Fact]
    public async Task AListShorterThanTheWindowIsShownWhole()
    {
        // arrange -- the page used to be twenty rows whatever the window was, which left most
        // of a tall terminal empty and made a long list feel longer than it is
        var console = GetConsole();

        console.Profile.Height = 60;

        console.Input.PushKey(ConsoleKey.Escape);

        var browser = GetBrowser();
        var screen = new CommandBrowserScreen(console, browser);

        // act
        await screen.ShowAsync(TestContext.Current.CancellationToken);

        // assert -- the last command in the list reached the screen without scrolling
        var lastGroup = browser.GetTree()[^1].Groups[^1];
        var last = lastGroup.Commands[^1];

        Assert.Contains(last.GetLabel(lastGroup.HasGroup), console.Output);
        Assert.DoesNotContain("move up and down for more", console.Output);
    }

    [Fact]
    public async Task TheWayOutIsARowInTheListAsWellAsAKeystroke()
    {
        // arrange -- a short enough list that the bottom of it is on screen, which is when the
        // row is what someone finds
        var console = GetConsole();
        var browser = GetBrowser();

        browser.Filter = "greeting";

        console.Input.PushKey(ConsoleKey.Escape);

        var screen = new CommandBrowserScreen(console, browser);

        // act
        await screen.ShowAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Contains("Quit", console.Output);
        Assert.Contains("or press esc", console.Output);
    }

    [Fact]
    public async Task EscapeLeavesEvenWithSomethingTypedIntoTheSearch()
    {
        // arrange -- the hint says esc quits without qualification, so this is what makes that
        // true rather than nearly true
        var console = GetConsole();

        console.Input.PushText("greet");
        console.Input.PushKey(ConsoleKey.Escape);

        var screen = new CommandBrowserScreen(console, GetBrowser());

        // act
        var action = await screen.ShowAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(TuiScreenAction.Quit, action);
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
