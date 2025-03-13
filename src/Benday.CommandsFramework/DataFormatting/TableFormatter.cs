using System;
using System.Text;

namespace Benday.CommandsFramework.DataFormatting;

/// <summary>
/// This class formats data as a table for display in the console.  It also provides some basic filtering capabilities.
/// </summary>
public class TableFormatter
{
    public TableFormatter()
    {
        Columns = new List<TableColumnDefinition>();
    }

    /// <summary>
    /// The columns in the table.
    /// </summary>
    public List<TableColumnDefinition> Columns { get; }

    /// <summary>
    /// The data in the table. Each string[] represents a row in the table.
    /// </summary>
    public List<string[]> Data { get; } = new List<string[]>();

    /// <summary>
    /// Add a column to the table.
    /// </summary>
    /// <param name="columnName">Name of the column</param>
    /// <returns></returns>
    public TableColumnDefinition AddColumn(string columnName)
    {
        var newItem = new TableColumnDefinition()
        {
            Name = columnName
        };

        Columns.Add(newItem);

        return newItem;
    }

    /// <summary>
    /// Add data to the table.
    /// </summary>
    /// <param name="data">The column values for this row.</param>
    /// <exception cref="ArgumentNullException">This is thrown if the data passed in is null</exception>
    /// <exception cref="ArgumentException">This is thrown if the number of values does not match the 
    /// number of configured columns for the table.</exception>
    public void AddData(params string[] data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data), "data is null.");
        }

        if (data.Length == 0)
        {
            throw new ArgumentException("data is empty.", nameof(data));
        }

        if (data.Length != Columns.Count)
        {
            throw new ArgumentException(
                $"Expected {Columns.Count} columns but received {data.Length} columns.",
                nameof(data));
        }

        Data.Add(data);

        for (var index = 0; index < data.Length; index++)
        {
            var column = Columns[index];

            if (data[index] == null)
            {
                data[index] = string.Empty;
            }

            column.CheckValueLength(data[index]);            
        }
    }

    /// <summary>
    /// Get the table formatted as a string.
    /// </summary>
    /// <returns></returns>
    public string FormatTable()
    {
        var builder = new StringBuilder();

        foreach (var column in Columns)
        {
            builder.Append(column.NamePadded);

            if (column != Columns.Last())
            {
                builder.Append(" ");
            }
        }

        builder.AppendLine();

        foreach (var row in Data)
        {
            var needsSeparator = false;

            for (var index = 0; index < row.Length; index++)
            {
                if (needsSeparator == true)
                {
                    builder.Append(" ");
                }

                var column = Columns[index];

                var columnValue = row[index].PadRight(column.Width);

                builder.Append(columnValue);

                needsSeparator = true;
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    /// <summary>
    /// Add data to the table if any of the column values contain the filter.
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="data"></param>    
    public void AddDataWithFilter(string filter, params string[] data)
    {
        if (data != null && data.Length > 0)
        {
            foreach (var item in data)
            {
                if (item == null)
                {
                    continue;
                }
                
                if (item.Contains(filter, StringComparison.CurrentCultureIgnoreCase) == true)
                {
                    AddData(data);
                    break;
                }
            }
        }
    }
}
