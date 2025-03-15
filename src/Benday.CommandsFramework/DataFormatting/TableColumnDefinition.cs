namespace Benday.CommandsFramework.DataFormatting;

/// <summary>
/// Definition of a column in a table.
/// </summary>
public class TableColumnDefinition
{
    /// <summary>
    /// The name of the column.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The name of the column, padded to the width of the longest value in the column.
    /// </summary>
    public string NamePadded
    {
        get
        {
            return Name.PadRight(Width);
        }
    }

    /// <summary>
    /// The width of the longest value in the column.
    /// </summary>
    public int WidthOfLongestValue { get; set; }

    /// <summary>
    /// The width of the column.  This is the greater of the length of the column name or the width of the longest value.
    /// </summary>
    public int Width
    {
        get
        {
            return Math.Max(Name.Length, WidthOfLongestValue);
        }
    }

    /// <summary>
    /// Is the column name longer than the longest value in the column?
    /// </summary>
    public bool IsColumnNameLongerThanLongestValue
    {
        get
        {
            return Name.Length > WidthOfLongestValue;
        }
    }

    /// <summary>
    /// Check the length of the value to see if it is longer than the current longest value. 
    /// If it is, update the WidthOfLongestValue property.
    /// </summary>
    /// <param name="newValue"></param>
    public void CheckValueLength(string newValue)
    {
        if (newValue == null)
        {
            return;
        }

        if (WidthOfLongestValue < newValue.Length)
        {
            WidthOfLongestValue = newValue.Length;
        }
    }

}