namespace Benday.CommandsFramework;

/// <summary>
/// One progress report from a running command: what it is doing, and optionally how far
/// through it is.
/// </summary>
/// <remarks>
/// Progress is commentary about the work rather than the work's result, so it travels on the
/// diagnostic channel. That is why git clone's progress still shows when you redirect its
/// output, and why it does not end up inside the file you redirected to.
/// </remarks>
public sealed class CommandProgress
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="message">What the command is doing</param>
    /// <param name="current">How many units are done, when that is known</param>
    /// <param name="total">How many units there are, when that is known</param>
    public CommandProgress(string message, int? current = null, int? total = null)
    {
        Message = message ?? string.Empty;
        Current = current;
        Total = total;
    }

    /// <summary>
    /// What the command is doing.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// How many units are done, or null when the work is not countable.
    /// </summary>
    public int? Current { get; }

    /// <summary>
    /// How many units there are, or null when that is not known up front.
    /// </summary>
    public int? Total { get; }

    /// <summary>
    /// True when both a current and a total are known, so the report can be shown as a
    /// proportion.
    /// </summary>
    public bool IsMeasured => Current.HasValue == true && Total.HasValue == true && Total > 0;

    /// <summary>
    /// How far through the work is, from 0 to 1, or null when that is not known.
    /// </summary>
    public double? Fraction =>
        IsMeasured ? Math.Clamp((double)Current!.Value / Total!.Value, 0, 1) : null;

    /// <summary>
    /// The report as a single line of text.
    /// </summary>
    public override string ToString()
    {
        if (IsMeasured == false)
        {
            return Message;
        }

        var percent = (int)Math.Round(Fraction!.Value * 100);

        return string.IsNullOrWhiteSpace(Message)
            ? $"{Current}/{Total} ({percent}%)"
            : $"{Message} {Current}/{Total} ({percent}%)";
    }
}
