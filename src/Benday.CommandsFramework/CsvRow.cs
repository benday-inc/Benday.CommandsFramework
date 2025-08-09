using System.Collections.Generic;

namespace Benday.CommandsFramework
{
    public class CsvRow
    {
        private readonly Dictionary<string, int> _columnNameToIndex;
        private readonly List<string> _values;

        public CsvRow(List<string> values, Dictionary<string, int> columnNameToIndex)
        {
            _values = values;
            _columnNameToIndex = columnNameToIndex;
        }

        public IColumnValue this[string columnName]
        {
            get
            {
                if (_columnNameToIndex.TryGetValue(columnName, out var index))
                {
                    return new ColumnValue(_values[index]);
                }
                throw new KeyNotFoundException($"Column name '{columnName}' not found.");
            }
        }

        public IColumnValue this[int columnIndex]
        {
            get
            {
                if (columnIndex >= 0 && columnIndex < _values.Count)
                {
                    return new ColumnValue(_values[columnIndex]);
                }
                throw new KeyNotFoundException($"Column index '{columnIndex}' out of range.");
            }
        }

        public int ColumnCount => _values.Count;
    }
}
