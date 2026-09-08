namespace Benday.CommandsFramework.DataFormatting;

/// <summary>
/// A single field that is longer than a caller-supplied maximum. Returned by
/// <see cref="CsvWriter.GetOversizedFields(int)"/> so that a caller can report
/// or shorten the value rather than writing a file a spreadsheet cannot open.
/// </summary>
/// <param name="RowIndex">
/// Zero-based index of the data row, or -1 for the header row.
/// </param>
/// <param name="ColumnIndex">Zero-based index of the column.</param>
/// <param name="ColumnName">
/// The column header, or null when the writer has no headers.
/// </param>
/// <param name="Length">The actual length of the value, in characters.</param>
public record CsvOversizedField(
    int RowIndex,
    int ColumnIndex,
    string? ColumnName,
    int Length)
{
    /// <summary>
    /// A description of where the value is, suitable for an error message.
    /// </summary>
    public string GetLocationDescription()
    {
        var column = string.IsNullOrEmpty(ColumnName)
            ? $"column {ColumnIndex}"
            : $"column '{ColumnName}'";

        var row = RowIndex < 0 ? "the header row" : $"row {RowIndex}";

        return $"{row}, {column}";
    }
}
