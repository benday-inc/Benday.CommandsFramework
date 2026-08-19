namespace Benday.CommandsFramework.CmdUi.Models;

/// <summary>
/// A tool's --json output after it has been read, whichever shape it arrived in.
/// </summary>
/// <remarks>
/// cmdui has a compatibility problem a normal library does not: it is a global tool that
/// probes whatever tools happen to be installed, so one copy has to read both the 4.x and
/// the 5.x schema on the same machine. The 4.x form is a bare JSON array and the 5.x form
/// is an object, so the root token alone says which is which -- see
/// ToolSchemaService.ParseSchema.
/// </remarks>
public class ToolSchemaDocument
{
    /// <summary>
    /// Schema version reported by the tool. 1 for the 4.x bare array, which never said so
    /// itself -- cmdui infers it from the shape.
    /// </summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>
    /// Application name reported by the tool. Empty for a 4.x schema, which did not carry it.
    /// </summary>
    public string ApplicationName { get; set; } = string.Empty;

    /// <summary>
    /// Application version reported by the tool. Empty for a 4.x schema.
    /// </summary>
    public string ApplicationVersion { get; set; } = string.Empty;

    public List<ToolCommandInfo> Commands { get; set; } = new();
}
