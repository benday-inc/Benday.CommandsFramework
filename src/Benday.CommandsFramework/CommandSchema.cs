namespace Benday.CommandsFramework;

/// <summary>
/// The envelope around the command schema that --json writes. Everything about a tool's
/// commands travels inside this, and SchemaVersion says which shape it is in.
/// </summary>
/// <remarks>
/// Before this existed, --json wrote a bare JSON array with nothing identifying it, so a
/// consumer reading the output of an arbitrary tool had no way to know what it was looking
/// at or when it changed. Because the old form is an <b>array</b> and this one is an
/// <b>object</b>, a consumer can tell them apart from the root token alone -- no version
/// negotiation, and no changes needed in tools that shipped against 4.x.
/// </remarks>
public class CommandSchema
{
    /// <summary>
    /// Version of the schema shape. Read this before anything else; a consumer that does
    /// not recognise the version should say so rather than guess.
    /// </summary>
    public int SchemaVersion { get; set; } = CommandFrameworkConstants.CurrentSchemaVersion;

    /// <summary>
    /// Name of the tool the schema came from.
    /// </summary>
    public string ApplicationName { get; set; } = string.Empty;

    /// <summary>
    /// Version of the tool the schema came from. This is the tool's version, not the
    /// schema's -- SchemaVersion is the schema's.
    /// </summary>
    public string ApplicationVersion { get; set; } = string.Empty;

    /// <summary>
    /// Every command the tool exposes.
    /// </summary>
    public List<CommandInfo> Commands { get; set; } = new();
}
