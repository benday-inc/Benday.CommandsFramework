using System;
using System.Collections.Generic;

namespace Benday.CommandsFramework.DataFormatting;

/// <summary>
/// Represents a single row in a CSV file with support for accessing values by column index or name.
/// </summary>
public class CsvRow
{
    private readonly string[] _values;
    private readonly Dictionary<string, int>? _columnNameToIndex;

    /// <summary>
    /// Initializes a new instance of the CsvRow class.
    /// </summary>
    /// <param name="values">The values in this row.</param>
    /// <param name="columnNameToIndex">Optional mapping of column names to indices.</param>
    public CsvRow(string[] values, Dictionary<string, int>? columnNameToIndex = null)
    {
        _values = values ?? throw new ArgumentNullException(nameof(values));
        _columnNameToIndex = columnNameToIndex;
    }

    /// <summary>
    /// Gets the number of columns in this row.
    /// </summary>
    public int ColumnCount => _values.Length;

    /// <summary>
    /// Gets the value at the specified column index.
    /// </summary>
    /// <param name="index">The zero-based column index.</param>
    /// <returns>The value at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is out of range.</exception>
    public string this[int index]
    {
        get
        {
            if (index < 0 || index >= _values.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range. Valid range is 0 to {_values.Length - 1}.");
            }
            return _values[index];
        }
    }

    /// <summary>
    /// Gets the value at the specified column name.
    /// </summary>
    /// <param name="columnName">The name of the column.</param>
    /// <returns>The value at the specified column.</returns>
    /// <exception cref="ArgumentNullException">Thrown when columnName is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when column names are not available or the column name is not found.</exception>
    public string this[string columnName]
    {
        get
        {
            if (columnName == null)
            {
                throw new ArgumentNullException(nameof(columnName));
            }

            if (_columnNameToIndex == null)
            {
                throw new InvalidOperationException("Column names are not available. The CSV reader was not configured to use header rows.");
            }

            if (!_columnNameToIndex.TryGetValue(columnName, out int index))
            {
                throw new InvalidOperationException($"Column '{columnName}' was not found in the CSV header.");
            }

            return _values[index];
        }
    }

    /// <summary>
    /// Determines whether the specified column name exists in this row.
    /// </summary>
    /// <param name="columnName">The name of the column to check.</param>
    /// <returns>true if the column exists; otherwise, false.</returns>
    public bool HasColumn(string columnName)
    {
        return _columnNameToIndex?.ContainsKey(columnName) ?? false;
    }

    /// <summary>
    /// Gets all values in this row as an array.
    /// </summary>
    /// <returns>An array containing all values in this row.</returns>
    public string[] GetValues()
    {
        return (string[])_values.Clone();
    }

    /// <summary>
    /// Gets the available column names if header row was used.
    /// </summary>
    /// <returns>An array of column names, or null if no header row was used.</returns>
    public string[]? GetColumnNames()
    {
        if (_columnNameToIndex == null)
        {
            return null;
        }

        var columnNames = new string[_values.Length];
        foreach (var kvp in _columnNameToIndex)
        {
            if (kvp.Value < columnNames.Length)
            {
                columnNames[kvp.Value] = kvp.Key;
            }
        }
        return columnNames;
    }
}