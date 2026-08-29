namespace Benday.CommandsFramework;

/// <summary>
/// Whether an argument's value is a path and, when it is, what kind of thing the
/// path points at.
/// </summary>
/// <remarks>
/// This is separate from ArgumentDataType because a file argument and a directory
/// argument are both string arguments as far as parsing and conversion go -- what
/// makes them different is what the value means and how it is validated. Splitting
/// it out means DataType keeps reporting String for them, so nothing that switches
/// on DataType has to change.
/// </remarks>
public enum ArgumentPathType
{
    /// <summary>
    /// The value is not a path.
    /// </summary>
    None,

    /// <summary>
    /// The value is a path to a file.
    /// </summary>
    File,

    /// <summary>
    /// The value is a path to a directory.
    /// </summary>
    Directory
}
