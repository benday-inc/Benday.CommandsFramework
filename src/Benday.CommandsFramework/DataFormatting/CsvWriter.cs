using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Benday.CommandsFramework.DataFormatting;

/// <summary>
/// A CSV file writer that allows building and editing CSV data in memory before writing to disk or converting to string.
/// </summary>
public class CsvWriter
{
    /// <summary>
    /// The maximum number of characters Excel will hold in a single cell.
    /// A longer value does not merely get truncated on import: Excel fills the
    /// cell to this length, loses track of the quoted field, and spills the
    /// remainder into following rows split on every comma, which misaligns the
    /// rest of the sheet.
    /// </summary>
    public const int ExcelMaxCellLength = 32767;

    private readonly List<CsvRow> _rows;
    private string[]? _headers;
    private bool _hasDataRows;

    /// <summary>
    /// Initializes a new instance of the CsvWriter class.
    /// </summary>
    public CsvWriter()
    {
        _rows = new List<CsvRow>();
        _hasDataRows = false;
        HasHeaderRow = true;
    }

    /// <summary>
    /// Initializes a new instance of the CsvWriter class with the specified header row setting.
    /// </summary>
    /// <param name="hasHeaderRow">Whether the CSV should include a header row.</param>
    public CsvWriter(bool hasHeaderRow)
    {
        _rows = new List<CsvRow>();
        _hasDataRows = false;
        HasHeaderRow = hasHeaderRow;
    }

    /// <summary>
    /// Initializes a new instance of the CsvWriter class from an existing CsvReader for editing.
    /// </summary>
    /// <param name="reader">The CsvReader to load data from.</param>
    /// <exception cref="ArgumentNullException">Thrown when reader is null.</exception>
    public CsvWriter(CsvReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        _rows = new List<CsvRow>();
        _hasDataRows = false;
        HasHeaderRow = reader.HasHeaderRow;

        // Load headers if present
        if (HasHeaderRow)
        {
            var headers = reader.GetColumnNames();
            if (headers != null)
            {
                _headers = (string[])headers.Clone();
            }
        }

        // Load all data rows
        foreach (var row in reader)
        {
            _rows.Add(row);
            _hasDataRows = true;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the first row should be treated as column headers.
    /// Default is true.
    /// </summary>
    public bool HasHeaderRow { get; set; }

    /// <summary>
    /// Gets or sets the maximum length allowed for a single field. When set,
    /// <see cref="ToCsvString"/> and <see cref="SaveToFile"/> throw rather than
    /// writing a file that a spreadsheet will mangle. Null (the default) does
    /// not enforce a limit, so existing callers are unaffected.
    /// Set it to <see cref="ExcelMaxCellLength"/> for a file destined for Excel.
    /// </summary>
    /// <remarks>
    /// Use <see cref="GetOversizedFields(int)"/> to find the offending values
    /// without an exception.
    /// </remarks>
    public int? MaxFieldLength { get; set; }

    /// <summary>
    /// Gets the number of data rows in the CSV (excluding header row).
    /// </summary>
    public int RowCount => _rows.Count;

    /// <summary>
    /// Gets the number of columns in the CSV. Returns 0 if no data or headers are present.
    /// </summary>
    public int ColumnCount
    {
        get
        {
            if (HasHeaderRow && _headers != null)
            {
                return _headers.Length;
            }

            if (_rows.Count > 0)
            {
                return _rows[0].ColumnCount;
            }

            return 0;
        }
    }

    /// <summary>
    /// Sets the column headers.
    /// </summary>
    /// <param name="headers">The column headers.</param>
    /// <exception cref="ArgumentNullException">Thrown when headers is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when HasHeaderRow is false or data rows have already been added.</exception>
    public void SetHeaders(string[] headers)
    {
        if (headers == null)
        {
            throw new ArgumentNullException(nameof(headers));
        }

        if (!HasHeaderRow)
        {
            throw new InvalidOperationException("Cannot set headers when HasHeaderRow is false.");
        }

        if (_hasDataRows)
        {
            throw new InvalidOperationException("Cannot modify headers after data rows have been added.");
        }

        _headers = (string[])headers.Clone();
    }

    /// <summary>
    /// Adds a single column by name.
    /// </summary>
    /// <param name="columnName">The name of the column to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when columnName is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when HasHeaderRow is false or data rows have already been added.</exception>
    public void AddColumn(string columnName)
    {
        if (columnName == null)
        {
            throw new ArgumentNullException(nameof(columnName));
        }

        if (!HasHeaderRow)
        {
            throw new InvalidOperationException("Cannot add columns when HasHeaderRow is false.");
        }

        if (_hasDataRows)
        {
            throw new InvalidOperationException("Cannot add columns after data rows have been added.");
        }

        if (_headers == null)
        {
            _headers = new[] { columnName };
        }
        else
        {
            var newHeaders = new string[_headers.Length + 1];
            Array.Copy(_headers, newHeaders, _headers.Length);
            newHeaders[_headers.Length] = columnName;
            _headers = newHeaders;
        }
    }

    /// <summary>
    /// Adds multiple columns by name.
    /// </summary>
    /// <param name="columnNames">The names of the columns to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when columnNames is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when HasHeaderRow is false or data rows have already been added.</exception>
    public void AddColumns(params string[] columnNames)
    {
        if (columnNames == null)
        {
            throw new ArgumentNullException(nameof(columnNames));
        }

        if (!HasHeaderRow)
        {
            throw new InvalidOperationException("Cannot add columns when HasHeaderRow is false.");
        }

        if (_hasDataRows)
        {
            throw new InvalidOperationException("Cannot add columns after data rows have been added.");
        }

        foreach (var columnName in columnNames)
        {
            if (columnName == null)
            {
                throw new ArgumentNullException(nameof(columnNames), "Column names cannot contain null values.");
            }
        }

        if (_headers == null)
        {
            _headers = (string[])columnNames.Clone();
        }
        else
        {
            var newHeaders = new string[_headers.Length + columnNames.Length];
            Array.Copy(_headers, newHeaders, _headers.Length);
            Array.Copy(columnNames, 0, newHeaders, _headers.Length, columnNames.Length);
            _headers = newHeaders;
        }
    }

    /// <summary>
    /// Gets the column headers.
    /// </summary>
    /// <returns>An array of column headers, or null if HasHeaderRow is false or no headers are set.</returns>
    public string[]? GetHeaders()
    {
        if (!HasHeaderRow || _headers == null)
        {
            return null;
        }

        return (string[])_headers.Clone();
    }

    /// <summary>
    /// Adds a new row to the end of the CSV.
    /// </summary>
    /// <param name="values">The values for the new row.</param>
    /// <exception cref="ArgumentNullException">Thrown when values is null.</exception>
    public void AddRow(params string[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        var columnMapping = HasHeaderRow && _headers != null
            ? CreateColumnMapping(_headers)
            : null;

        var row = new CsvRow((string[])values.Clone(), columnMapping);
        _rows.Add(row);
        _hasDataRows = true;
    }

    /// <summary>
    /// Adds a new row to the end of the CSV from a CsvRow.
    /// </summary>
    /// <param name="row">The CsvRow to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when row is null.</exception>
    public void AddRow(CsvRow row)
    {
        if (row == null)
        {
            throw new ArgumentNullException(nameof(row));
        }

        var columnMapping = HasHeaderRow && _headers != null
            ? CreateColumnMapping(_headers)
            : null;

        // Create a new CsvRow with our column mapping
        var newRow = new CsvRow(row.GetValues(), columnMapping);
        _rows.Add(newRow);
        _hasDataRows = true;
    }

    /// <summary>
    /// Inserts a new row at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index at which to insert the row.</param>
    /// <param name="values">The values for the new row.</param>
    /// <exception cref="ArgumentNullException">Thrown when values is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is out of range.</exception>
    public void InsertRow(int index, params string[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        if (index < 0 || index > _rows.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range. Valid range is 0 to {_rows.Count}.");
        }

        var columnMapping = HasHeaderRow && _headers != null
            ? CreateColumnMapping(_headers)
            : null;

        var row = new CsvRow((string[])values.Clone(), columnMapping);
        _rows.Insert(index, row);
        _hasDataRows = true;
    }

    /// <summary>
    /// Removes the row at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the row to remove.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is out of range.</exception>
    public void RemoveRow(int index)
    {
        if (index < 0 || index >= _rows.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range. Valid range is 0 to {_rows.Count - 1}.");
        }

        _rows.RemoveAt(index);
        
        // Update _hasDataRows if no rows remain
        if (_rows.Count == 0)
        {
            _hasDataRows = false;
        }
    }

    /// <summary>
    /// Gets the row at the specified index as a CsvRow.
    /// </summary>
    /// <param name="index">The zero-based index of the row.</param>
    /// <returns>A CsvRow representing the row at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is out of range.</exception>
    public CsvRow GetRow(int index)
    {
        if (index < 0 || index >= _rows.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range. Valid range is 0 to {_rows.Count - 1}.");
        }

        return _rows[index];
    }

    /// <summary>
    /// Sets the values for the row at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the row.</param>
    /// <param name="values">The new values for the row.</param>
    /// <exception cref="ArgumentNullException">Thrown when values is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is out of range.</exception>
    public void SetRow(int index, params string[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        if (index < 0 || index >= _rows.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range. Valid range is 0 to {_rows.Count - 1}.");
        }

        var columnMapping = HasHeaderRow && _headers != null
            ? CreateColumnMapping(_headers)
            : null;

        _rows[index] = new CsvRow((string[])values.Clone(), columnMapping);
    }

    /// <summary>
    /// Gets the value at the specified row and column.
    /// </summary>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="column">The zero-based column index.</param>
    /// <returns>The value at the specified position.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when row or column index is out of range.</exception>
    public string GetValue(int row, int column)
    {
        if (row < 0 || row >= _rows.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(row), $"Row index {row} is out of range. Valid range is 0 to {_rows.Count - 1}.");
        }

        var csvRow = _rows[row];
        if (column < 0 || column >= csvRow.ColumnCount)
        {
            throw new ArgumentOutOfRangeException(nameof(column), $"Column index {column} is out of range. Valid range is 0 to {csvRow.ColumnCount - 1}.");
        }

        return csvRow[column];
    }

    /// <summary>
    /// Gets the value at the specified row and column name.
    /// </summary>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="columnName">The name of the column.</param>
    /// <returns>The value at the specified position.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when row index is out of range.</exception>
    /// <exception cref="ArgumentNullException">Thrown when columnName is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when headers are not available or column name is not found.</exception>
    public string GetValue(int row, string columnName)
    {
        if (columnName == null)
        {
            throw new ArgumentNullException(nameof(columnName));
        }

        if (row < 0 || row >= _rows.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(row), $"Row index {row} is out of range. Valid range is 0 to {_rows.Count - 1}.");
        }

        if (!HasHeaderRow || _headers == null)
        {
            throw new InvalidOperationException("Column names are not available. The CSV writer was not configured to use header rows or headers have not been set.");
        }

        var csvRow = _rows[row];
        try
        {
            return csvRow[columnName];
        }
        catch (InvalidOperationException)
        {
            throw new InvalidOperationException($"Column '{columnName}' was not found in the CSV headers.");
        }
    }

    /// <summary>
    /// Sets the value at the specified row and column.
    /// </summary>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="column">The zero-based column index.</param>
    /// <param name="value">The value to set.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when row or column index is out of range.</exception>
    public void SetValue(int row, int column, string value)
    {
        if (row < 0 || row >= _rows.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(row), $"Row index {row} is out of range. Valid range is 0 to {_rows.Count - 1}.");
        }

        var csvRow = _rows[row];
        if (column < 0 || column >= csvRow.ColumnCount)
        {
            throw new ArgumentOutOfRangeException(nameof(column), $"Column index {column} is out of range. Valid range is 0 to {csvRow.ColumnCount - 1}.");
        }

        var values = csvRow.GetValues();
        values[column] = value ?? string.Empty;
        
        var columnMapping = HasHeaderRow && _headers != null
            ? CreateColumnMapping(_headers)
            : null;
        
        _rows[row] = new CsvRow(values, columnMapping);
    }

    /// <summary>
    /// Sets the value at the specified row and column name.
    /// </summary>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="columnName">The name of the column.</param>
    /// <param name="value">The value to set.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when row index is out of range.</exception>
    /// <exception cref="ArgumentNullException">Thrown when columnName is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when headers are not available or column name is not found.</exception>
    public void SetValue(int row, string columnName, string value)
    {
        if (columnName == null)
        {
            throw new ArgumentNullException(nameof(columnName));
        }

        if (!HasHeaderRow || _headers == null)
        {
            throw new InvalidOperationException("Column names are not available. The CSV writer was not configured to use header rows or headers have not been set.");
        }

        var columnIndex = Array.IndexOf(_headers, columnName);
        if (columnIndex == -1)
        {
            throw new InvalidOperationException($"Column '{columnName}' was not found in the CSV headers.");
        }

        SetValue(row, columnIndex, value);
    }

    /// <summary>
    /// Finds every field longer than <paramref name="maxLength"/> characters.
    /// Pass <see cref="ExcelMaxCellLength"/> to find the values that would make
    /// Excel spill a cell across following rows.
    /// </summary>
    /// <param name="maxLength">The maximum allowed field length.</param>
    /// <returns>
    /// The oversized fields, in row then column order. Empty when everything
    /// fits.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when maxLength is less than 1.
    /// </exception>
    public IReadOnlyList<CsvOversizedField> GetOversizedFields(int maxLength)
    {
        if (maxLength < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxLength), $"Value {maxLength} must be at least 1.");
        }

        var results = new List<CsvOversizedField>();

        if (HasHeaderRow && _headers != null)
        {
            AddOversizedFields(results, _headers, -1);
        }

        for (int i = 0; i < _rows.Count; i++)
        {
            AddOversizedFields(results, _rows[i].GetValues(), i);
        }

        return results;

        void AddOversizedFields(
            List<CsvOversizedField> target, string[] values, int rowIndex)
        {
            for (int column = 0; column < values.Length; column++)
            {
                var value = values[column];

                if (value != null && value.Length > maxLength)
                {
                    target.Add(new CsvOversizedField(
                        rowIndex, column, GetColumnName(column), value.Length));
                }
            }
        }
    }

    private string? GetColumnName(int columnIndex)
    {
        if (HasHeaderRow == false || _headers == null ||
            columnIndex < 0 || columnIndex >= _headers.Length)
        {
            return null;
        }

        return _headers[columnIndex];
    }

    /// <summary>
    /// Throws when any field exceeds <see cref="MaxFieldLength"/>. Does nothing
    /// when no limit is set.
    /// </summary>
    private void AssertFieldLengths()
    {
        if (MaxFieldLength.HasValue == false)
        {
            return;
        }

        var maxLength = MaxFieldLength.Value;
        var oversized = GetOversizedFields(maxLength);

        if (oversized.Count == 0)
        {
            return;
        }

        var first = oversized[0];

        var message =
            $"CSV field length limit of {maxLength} exceeded by " +
            $"{oversized.Count} value(s). The first is at " +
            $"{first.GetLocationDescription()} and is {first.Length} characters.";

        if (maxLength == ExcelMaxCellLength)
        {
            message +=
                " Excel cannot hold more than " +
                $"{ExcelMaxCellLength} characters in a cell and will spill the " +
                "overflow into following rows. Shorten the value or move it to " +
                "a row of its own.";
        }

        throw new InvalidOperationException(message);
    }

    /// <summary>
    /// Converts the CSV data to a string representation.
    /// </summary>
    /// <returns>A string containing the CSV data.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="MaxFieldLength"/> is set and a field exceeds it.
    /// </exception>
    public string ToCsvString()
    {
        AssertFieldLengths();

        var result = new StringBuilder();

        // Add header row if applicable
        if (HasHeaderRow && _headers != null)
        {
            result.AppendLine(FormatCsvLine(_headers));
        }

        // Add data rows
        foreach (var row in _rows)
        {
            result.AppendLine(FormatCsvLine(row.GetValues()));
        }

        // Remove the final newline if present
        if (result.Length > 0 && result[result.Length - 1] == '\n')
        {
            result.Length--;
            if (result.Length > 0 && result[result.Length - 1] == '\r')
            {
                result.Length--;
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Saves the CSV data to a file.
    /// </summary>
    /// <param name="filePath">The path where the CSV file should be saved.</param>
    /// <param name="writeByteOrderMark">Whether to write a UTF-8 byte order mark (BOM). Defaults to true so that Excel and other tools correctly detect the file as UTF-8.</param>
    /// <exception cref="ArgumentNullException">Thrown when filePath is null.</exception>
    /// <exception cref="IOException">Thrown when there's an error writing the file.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="MaxFieldLength"/> is set and a field exceeds it.
    /// </exception>
    public void SaveToFile(string filePath, bool writeByteOrderMark = true)
    {
        if (filePath == null)
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        var csvContent = ToCsvString();
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: writeByteOrderMark);
        File.WriteAllText(filePath, csvContent, encoding);
    }

    private Dictionary<string, int> CreateColumnMapping(string[] headers)
    {
        var mapping = new Dictionary<string, int>();
        for (int i = 0; i < headers.Length; i++)
        {
            var columnName = headers[i]?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(columnName))
            {
                mapping[columnName] = i;
            }
        }
        return mapping;
    }

    private string FormatCsvLine(string[] values)
    {
        var formattedValues = values.Select(FormatCsvField);
        return string.Join(",", formattedValues);
    }

    private string FormatCsvField(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        // Check if the field needs to be quoted
        var needsQuoting = value.Contains(',') || 
                          value.Contains('\n') || 
                          value.Contains('\r') || 
                          value.Contains('"');

        if (!needsQuoting)
        {
            return value;
        }

        // Escape any existing quotes by doubling them
        var escapedValue = value.Replace("\"", "\"\"");

        // Wrap in quotes
        return $"\"{escapedValue}\"";
    }
}