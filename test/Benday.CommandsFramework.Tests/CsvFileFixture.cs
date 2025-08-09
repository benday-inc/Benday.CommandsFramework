using System;
using System.IO;
using System.Linq;
using Benday.CommandsFramework;
using Xunit;

namespace Benday.CommandsFramework.Tests
{
    public class CsvFileFixture
    {
        [Fact]
        public void CanParseCsvWithHeader()
        {
            var lines = new[]
            {
                "Name,Age,IsActive",
                "Alice,30,true",
                "Bob,25,false"
            };
            var csv = new CsvFile(lines, hasHeader: true);
            Assert.Equal(2, csv.RowCount);
            Assert.Equal(3, csv.ColumnCount);
            Assert.Contains("Name", csv.ColumnNames);
            Assert.Contains("Age", csv.ColumnNames);
            Assert.Contains("IsActive", csv.ColumnNames);
        }

        [Fact]
        public void CanAccessColumnByNameAndIndex()
        {
            var lines = new[]
            {
                "Name,Age,IsActive",
                "Alice,30,true"
            };
            var csv = new CsvFile(lines, hasHeader: true);
            var row = csv[0];
            Assert.Equal("Alice", row["Name"].AsString());
            Assert.Equal(30, row["Age"].AsInt());
            Assert.True(row["IsActive"].AsBool());
            Assert.Equal("Alice", row[0].AsString());
            Assert.Equal(30, row[1].AsInt());
            Assert.True(row[2].AsBool());
        }

        [Fact]
        public void CanAccessRowsByIndex()
        {
            var lines = new[]
            {
                "Name,Age",
                "Alice,30",
                "Bob,25"
            };
            var csv = new CsvFile(lines, hasHeader: true);
            Assert.Equal("Alice", csv[0]["Name"].AsString());
            Assert.Equal("Bob", csv[1]["Name"].AsString());
        }

        [Fact]
        public void CanEnumerateRows()
        {
            var lines = new[]
            {
                "Name,Age",
                "Alice,30",
                "Bob,25"
            };
            var csv = new CsvFile(lines, hasHeader: true);
            var names = csv.Select(r => r["Name"].AsString()).ToList();
            Assert.Contains("Alice", names);
            Assert.Contains("Bob", names);
        }

        [Fact]
        public void CanParseCsvWithoutHeader()
        {
            var lines = new[]
            {
                "Alice,30,true",
                "Bob,25,false"
            };
            var csv = new CsvFile(lines, hasHeader: false);
            Assert.Equal(2, csv.RowCount);
            Assert.Equal(3, csv.ColumnCount);
            Assert.Equal("Alice", csv[0][0].AsString());
            Assert.Equal(30, csv[0][1].AsInt());
            Assert.True(csv[0][2].AsBool());
        }

        [Fact]
        public void ColumnValueTypeConversions()
        {
            var value = new ColumnValue("42");
            Assert.Equal(42, value.AsInt());
            Assert.Equal(42.0, value.AsDouble());
            Assert.Equal(42.0f, value.AsFloat());
            Assert.Equal(42m, value.AsDecimal());
            Assert.Equal("42", value.AsString());
        }
    }
}
