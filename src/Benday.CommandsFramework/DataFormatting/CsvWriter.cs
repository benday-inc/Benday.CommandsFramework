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
    private readonly List<string[]> _rows;
    private string[]? _headers;

    /// <summary>
    /// Initializes a new instance of the CsvWriter class.
    /// </summary>
    public CsvWriter()
    {
        _rows = new List<string[]>();
        HasHeaderRow = true;
    }

    /// <summary>
    /// Initializes a new instance of the CsvWriter class with the specified header row setting.
    /// </summary>
    /// <param name="hasHeaderRow">Whether the CSV should include a header row.</param>
    public CsvWriter(bool hasHeaderRow)
    {
        _rows = new List<string[]>();
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

        _rows = new List<string[]>();
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
            _rows.Add(row.GetValues());
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the first row should be treated as column headers.
    /// Default is true.
    /// </summary>
    public bool HasHeaderRow { get; set; }

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
                return _rows[0].Length;
            }

            return 0;
        }
    }

    /// <summary>
    /// Sets the column headers.
    /// </summary>
    /// <param name="headers">The column headers.</param>
    /// <exception cref="ArgumentNullException">Thrown when headers is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when HasHeaderRow is false.</exception>
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

        _headers = (string[])headers.Clone();
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

        _rows.Add((string[])values.Clone());
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

        AddRow(row.GetValues());
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

        _rows.Insert(index, (string[])values.Clone());
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

        var columnMapping = HasHeaderRow && _headers != null
            ? CreateColumnMapping(_headers)
            : null;

        return new CsvRow(_rows[index], columnMapping);
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

        _rows[index] = (string[])values.Clone();
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

        var rowData = _rows[row];
        if (column < 0 || column >= rowData.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(column), $"Column index {column} is out of range. Valid range is 0 to {rowData.Length - 1}.");
        }

        return rowData[column];
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

        if (!HasHeaderRow || _headers == null)
        {
            throw new InvalidOperationException("Column names are not available. The CSV writer was not configured to use header rows or headers have not been set.");
        }

        var columnIndex = Array.IndexOf(_headers, columnName);
        if (columnIndex == -1)
        {
            throw new InvalidOperationException($"Column '{columnName}' was not found in the CSV headers.");
        }

        return GetValue(row, columnIndex);
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

        var rowData = _rows[row];
        if (column < 0 || column >= rowData.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(column), $"Column index {column} is out of range. Valid range is 0 to {rowData.Length - 1}.");
        }

        rowData[column] = value ?? string.Empty;
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
    /// Converts the CSV data to a string representation.
    /// </summary>
    /// <returns>A string containing the CSV data.</returns>
    public string ToCsvString()
    {
        var result = new StringBuilder();

        // Add header row if applicable
        if (HasHeaderRow && _headers != null)
        {
            result.AppendLine(FormatCsvLine(_headers));
        }

        // Add data rows
        foreach (var row in _rows)
        {
            result.AppendLine(FormatCsvLine(row));
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
    /// <exception cref="ArgumentNullException">Thrown when filePath is null.</exception>
    /// <exception cref="IOException">Thrown when there's an error writing the file.</exception>
    public void SaveToFile(string filePath)
    {
        if (filePath == null)
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        var csvContent = ToCsvString();
        File.WriteAllText(filePath, csvContent);
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