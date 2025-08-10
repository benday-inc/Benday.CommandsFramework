using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Benday.CommandsFramework.DataFormatting;

/// <summary>
/// A CSV file reader that provides enumerable access to rows in a CSV file.
/// </summary>
public class CsvReader : IEnumerable<CsvRow>
{
    private readonly string _csvContent;
    private Dictionary<string, int>? _columnNameToIndex;

    /// <summary>
    /// Initializes a new instance of the CsvReader class with CSV content.
    /// </summary>
    /// <param name="csvContent">The CSV content as a string.</param>
    /// <exception cref="ArgumentNullException">Thrown when csvContent is null.</exception>
    public CsvReader(string csvContent)
    {
        _csvContent = csvContent ?? throw new ArgumentNullException(nameof(csvContent));
        HasHeaderRow = true;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the first row should be treated as column headers.
    /// Default is true.
    /// </summary>
    public bool HasHeaderRow { get; set; }

    /// <summary>
    /// Creates a CsvReader from a file path.
    /// </summary>
    /// <param name="filePath">The path to the CSV file.</param>
    /// <returns>A new CsvReader instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when filePath is null.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="IOException">Thrown when there's an error reading the file.</exception>
    public static CsvReader FromFile(string filePath)
    {
        if (filePath == null)
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"The file '{filePath}' was not found.", filePath);
        }

        string content = File.ReadAllText(filePath);
        return new CsvReader(content);
    }

    /// <summary>
    /// Gets the column names from the header row, if available.
    /// </summary>
    /// <returns>An array of column names, or null if HasHeaderRow is false or no data is available.</returns>
    public string[]? GetColumnNames()
    {
        if (!HasHeaderRow)
        {
            return null;
        }

        var rows = ParseCsvContent();
        if (rows.Count == 0)
        {
            return null;
        }

        return rows[0];
    }

    /// <summary>
    /// Returns an enumerator that iterates through the CSV rows.
    /// </summary>
    /// <returns>An enumerator for the CSV rows.</returns>
    public IEnumerator<CsvRow> GetEnumerator()
    {
        var rows = ParseCsvContent();
        
        if (rows.Count == 0)
        {
            yield break;
        }

        int startIndex = 0;

        // If there's a header row, set up column name mapping and skip it
        if (HasHeaderRow && rows.Count > 0)
        {
            _columnNameToIndex = new Dictionary<string, int>();
            var headerRow = rows[0];
            
            for (int i = 0; i < headerRow.Length; i++)
            {
                var columnName = headerRow[i]?.Trim() ?? string.Empty;
                if (!string.IsNullOrEmpty(columnName))
                {
                    _columnNameToIndex[columnName] = i;
                }
            }
            
            startIndex = 1; // Skip header row
        }

        // Return data rows
        for (int i = startIndex; i < rows.Count; i++)
        {
            yield return new CsvRow(rows[i], _columnNameToIndex);
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the CSV rows.
    /// </summary>
    /// <returns>An enumerator for the CSV rows.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private List<string[]> ParseCsvContent()
    {
        var rows = new List<string[]>();
        
        if (string.IsNullOrEmpty(_csvContent))
        {
            return rows;
        }

        var fields = new List<string>();
        var currentField = new StringBuilder();
        bool inQuotes = false;
        
        for (int i = 0; i < _csvContent.Length; i++)
        {
            char c = _csvContent[i];
            
            if (c == '"')
            {
                if (inQuotes && i + 1 < _csvContent.Length && _csvContent[i + 1] == '"')
                {
                    // Escaped quote (double quote)
                    currentField.Append('"');
                    i++; // Skip next quote
                }
                else
                {
                    // Toggle quote state
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                // Field separator
                fields.Add(currentField.ToString());
                currentField.Clear();
            }
            else if (IsLineEnding(c, i) && !inQuotes)
            {
                // Row separator - only when not inside quotes
                fields.Add(currentField.ToString());
                
                // Skip empty rows (rows with only empty fields)
                if (!IsEmptyRow(fields))
                {
                    rows.Add(fields.ToArray());
                }
                
                fields.Clear();
                currentField.Clear();
                
                // Handle multi-character line endings like \r\n
                if (c == '\r' && i + 1 < _csvContent.Length && _csvContent[i + 1] == '\n')
                {
                    i++; // Skip the \n part of \r\n
                }
            }
            else
            {
                // Regular character (including newlines inside quotes)
                currentField.Append(c);
            }
        }
        
        // Add the last field and row if there's content
        if (currentField.Length > 0 || fields.Count > 0)
        {
            fields.Add(currentField.ToString());
            if (!IsEmptyRow(fields))
            {
                rows.Add(fields.ToArray());
            }
        }

        return rows;
    }

    private bool IsLineEnding(char c, int position)
    {
        return c == '\n' || c == '\r';
    }

    private bool IsEmptyRow(List<string> fields)
    {
        return fields.Count == 0 || fields.All(field => string.IsNullOrWhiteSpace(field));
    }
}