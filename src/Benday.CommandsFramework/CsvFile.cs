using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Benday.CommandsFramework
{
    public class CsvFile : IEnumerable<CsvRow>
    {
        private readonly List<CsvRow> _rows = new();
        private readonly Dictionary<string, int> _columnNameToIndex = new();
        private readonly bool _hasHeader;

        public CsvFile(string filePath, bool hasHeader = true)
        {
            _hasHeader = hasHeader;
            Parse(File.ReadAllLines(filePath));
        }

        public CsvFile(IEnumerable<string> lines, bool hasHeader = true)
        {
            _hasHeader = hasHeader;
            Parse(lines.ToArray());
        }

        private void Parse(string[] lines)
        {
            if (lines.Length == 0)
                return;

            int startRow = 0;
            if (_hasHeader)
            {
                var header = SplitCsvLine(lines[0]);
                for (int i = 0; i < header.Count; i++)
                {
                    _columnNameToIndex[header[i]] = i;
                }
                startRow = 1;
            }
            else
            {
                for (int i = 0; i < SplitCsvLine(lines[0]).Count; i++)
                {
                    _columnNameToIndex[$"Column{i}"] = i;
                }
            }

            for (int i = startRow; i < lines.Length; i++)
            {
                var values = SplitCsvLine(lines[i]);
                _rows.Add(new CsvRow(values, _columnNameToIndex));
            }
        }

        private List<string> SplitCsvLine(string line)
        {
            // Simple CSV split, does not handle quoted commas
            return line.Split(',').Select(s => s.Trim()).ToList();
        }

        public CsvRow this[int rowIndex]
        {
            get
            {
                if (rowIndex >= 0 && rowIndex < _rows.Count)
                {
                    return _rows[rowIndex];
                }
                throw new IndexOutOfRangeException($"Row index '{rowIndex}' out of range.");
            }
        }

        public int RowCount => _rows.Count;
        public int ColumnCount => _columnNameToIndex.Count;
        public IEnumerator<CsvRow> GetEnumerator() => _rows.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerable<string> ColumnNames => _columnNameToIndex.Keys;
    }
}
