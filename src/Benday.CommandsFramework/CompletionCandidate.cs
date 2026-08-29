namespace Benday.CommandsFramework;

/// <summary>
/// One thing a shell could put on the command line, or a directive telling the shell to
/// complete something itself.
/// </summary>
/// <remarks>
/// Paths are the reason directives exist. A tool has no business enumerating the filesystem
/// for a shell that already knows how to do it, handles quoting and escaping correctly, and
/// can show the results the way the user expects.
/// </remarks>
public sealed class CompletionCandidate
{
    private CompletionCandidate(string value, string description, bool isDirective)
    {
        Value = value;
        Description = description;
        IsDirective = isDirective;
    }

    /// <summary>
    /// The text to put on the command line, or the directive when IsDirective is true.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// What it is, for shells that can show it. Empty when there is nothing useful to say.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// True when this is an instruction to the shell rather than a candidate.
    /// </summary>
    public bool IsDirective { get; }

    public static CompletionCandidate ForValue(string value, string description = "")
    {
        return new CompletionCandidate(value, description ?? string.Empty, false);
    }

    /// <summary>
    /// Tells the shell to complete file paths, optionally narrowed to a pattern.
    /// </summary>
    public static CompletionCandidate ForFiles(string pattern = "*")
    {
        return new CompletionCandidate(
            $"{DirectivePrefix}file:{pattern}", string.Empty, true);
    }

    /// <summary>
    /// Tells the shell to complete directory paths.
    /// </summary>
    public static CompletionCandidate ForDirectories()
    {
        return new CompletionCandidate($"{DirectivePrefix}dir", string.Empty, true);
    }

    /// <summary>
    /// Marks the start of a directive line.
    /// </summary>
    public const string DirectivePrefix = ":";

    /// <summary>
    /// The line as a shell stub reads it: the value, then a tab, then the description when
    /// there is one.
    /// </summary>
    /// <remarks>
    /// A tab rather than anything prettier, because that is what shell completion scripts
    /// already split on and it cannot appear in a value.
    /// </remarks>
    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(Description) ? Value : $"{Value}\t{Description}";
    }
}
