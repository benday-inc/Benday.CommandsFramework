namespace Benday.CommandsFramework.Tui.Screens;

/// <summary>
/// What a screen asks the interface to do next.
/// </summary>
public enum TuiScreenAction
{
    /// <summary>
    /// Draw this screen again.
    /// </summary>
    Stay,

    /// <summary>
    /// Go back to whatever opened this screen.
    /// </summary>
    Back,

    /// <summary>
    /// Open the form for a command.
    /// </summary>
    OpenForm,

    /// <summary>
    /// Close the interface.
    /// </summary>
    Quit
}
