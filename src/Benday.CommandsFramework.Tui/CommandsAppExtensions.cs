using Spectre.Console;

namespace Benday.CommandsFramework.Tui;

/// <summary>
/// Turns on the terminal interface for a tool.
/// </summary>
public static class CommandsAppExtensions
{
    /// <summary>
    /// Gives the tool a terminal interface, reached with the 'tui' keyword.
    /// </summary>
    /// <remarks>
    /// One line, and the only thing a tool has to do. This lives here rather than on
    /// CommandsApp because the core framework does not reference a rendering library and
    /// should not start.
    /// </remarks>
    /// <param name="app">The builder</param>
    /// <param name="console">Where to render, or null for the real terminal</param>
    /// <returns>The builder</returns>
    public static CommandsApp WithTui(this CommandsApp app, IAnsiConsole? console = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.ConfigureOptions(options => options.TuiHost = new SpectreTuiHost(console));
    }
}
