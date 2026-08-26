namespace Benday.CommandsFramework.Tui.Model;

/// <summary>
/// How a form draws and edits one argument.
/// </summary>
/// <remarks>
/// This is a rendering decision made in the model layer so that it can be tested without a
/// terminal, and so the same decision is available to anything else that wants to draw a
/// form over these arguments.
/// </remarks>
public enum TuiFieldWidget
{
    /// <summary>
    /// Free text.
    /// </summary>
    Text,

    /// <summary>
    /// Pick one of a fixed set of values.
    /// </summary>
    Selection,

    /// <summary>
    /// A path to a file, with completion.
    /// </summary>
    FilePath,

    /// <summary>
    /// A path to a directory, with completion.
    /// </summary>
    DirectoryPath,

    /// <summary>
    /// On or off.
    /// </summary>
    Toggle,

    /// <summary>
    /// A whole number.
    /// </summary>
    Number,

    /// <summary>
    /// A date and time.
    /// </summary>
    Date
}
