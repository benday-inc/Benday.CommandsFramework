using System;
using System.IO;
using System.Linq;
using Benday.CommandsFramework.DataFormatting;

namespace Benday.CommandsFramework.Tests;

public class CsvRowFixture
{
    public CsvRowFixture()
    {
        _SystemUnderTest = null;
    }

    private CsvRow? _SystemUnderTest;

    private CsvRow SystemUnderTest
    {
        get
        {
            if (_SystemUnderTest == null)
            {
                _SystemUnderTest = new CsvRow(new[] { "value1", "value2", "value3" });
            }

            return _SystemUnderTest;
        }
    }

    [Fact]
    public void Constructor_WithValidValues_InitializesCorrectly()
    {
        // arrange
        var values = new[] { "a", "b", "c" };

        // act
        var result = new CsvRow(values);

        // assert
        Assert.Equal(3, result.ColumnCount);
        Assert.Equal("a", result[0]);
        Assert.Equal("b", result[1]);
        Assert.Equal("c", result[2]);
    }

    [Fact]
    public void Constructor_WithNullValues_ThrowsArgumentNullException()
    {
        // arrange
        string[]? values = null;

        // act & assert
        Assert.Throws<ArgumentNullException>(() => new CsvRow(values!));
    }

    [Fact]
    public void Indexer_ByIndex_ReturnsCorrectValue()
    {
        // arrange
        var values = new[] { "first", "second", "third" };
        var row = new CsvRow(values);

        // act & assert
        Assert.Equal("first", row[0]);
        Assert.Equal("second", row[1]);
        Assert.Equal("third", row[2]);
    }

    [Fact]
    public void Indexer_ByIndex_OutOfRange_ThrowsArgumentOutOfRangeException()
    {
        // arrange
        var values = new[] { "a", "b" };
        var row = new CsvRow(values);

        // act & assert
        Assert.Throws<ArgumentOutOfRangeException>(() => row[2]);
        Assert.Throws<ArgumentOutOfRangeException>(() => row[-1]);
    }

    [Fact]
    public void Indexer_ByColumnName_WithMapping_ReturnsCorrectValue()
    {
        // arrange
        var values = new[] { "John", "Doe", "john@example.com" };
        var columnMapping = new Dictionary<string, int>
        {
            ["FirstName"] = 0,
            ["LastName"] = 1,
            ["Email"] = 2
        };
        var row = new CsvRow(values, columnMapping);

        // act & assert
        Assert.Equal("John", row["FirstName"]);
        Assert.Equal("Doe", row["LastName"]);
        Assert.Equal("john@example.com", row["Email"]);
    }

    [Fact]
    public void Indexer_ByColumnName_WithoutMapping_ThrowsInvalidOperationException()
    {
        // arrange
        var values = new[] { "a", "b", "c" };
        var row = new CsvRow(values);

        // act & assert
        Assert.Throws<InvalidOperationException>(() => row["SomeColumn"]);
    }

    [Fact]
    public void Indexer_ByColumnName_WithNullColumnName_ThrowsArgumentNullException()
    {
        // arrange
        var values = new[] { "a", "b", "c" };
        var columnMapping = new Dictionary<string, int> { ["Col1"] = 0 };
        var row = new CsvRow(values, columnMapping);

        // act & assert
        Assert.Throws<ArgumentNullException>(() => row[null!]);
    }

    [Fact]
    public void Indexer_ByColumnName_NonExistentColumn_ThrowsInvalidOperationException()
    {
        // arrange
        var values = new[] { "a", "b", "c" };
        var columnMapping = new Dictionary<string, int> { ["Col1"] = 0 };
        var row = new CsvRow(values, columnMapping);

        // act & assert
        Assert.Throws<InvalidOperationException>(() => row["NonExistentColumn"]);
    }

    [Fact]
    public void HasColumn_ExistingColumn_ReturnsTrue()
    {
        // arrange
        var values = new[] { "a", "b", "c" };
        var columnMapping = new Dictionary<string, int>
        {
            ["Col1"] = 0,
            ["Col2"] = 1
        };
        var row = new CsvRow(values, columnMapping);

        // act & assert
        Assert.True(row.HasColumn("Col1"));
        Assert.True(row.HasColumn("Col2"));
    }

    [Fact]
    public void HasColumn_NonExistentColumn_ReturnsFalse()
    {
        // arrange
        var values = new[] { "a", "b", "c" };
        var columnMapping = new Dictionary<string, int> { ["Col1"] = 0 };
        var row = new CsvRow(values, columnMapping);

        // act & assert
        Assert.False(row.HasColumn("NonExistentColumn"));
    }

    [Fact]
    public void HasColumn_WithoutMapping_ReturnsFalse()
    {
        // arrange
        var values = new[] { "a", "b", "c" };
        var row = new CsvRow(values);

        // act & assert
        Assert.False(row.HasColumn("AnyColumn"));
    }

    [Fact]
    public void GetValues_ReturnsClonedArray()
    {
        // arrange
        var originalValues = new[] { "a", "b", "c" };
        var row = new CsvRow(originalValues);

        // act
        var result = row.GetValues();

        // assert
        Assert.Equal(originalValues, result);
        Assert.NotSame(originalValues, result); // Should be a copy
    }

    [Fact]
    public void GetColumnNames_WithMapping_ReturnsColumnNames()
    {
        // arrange
        var values = new[] { "a", "b", "c" };
        var columnMapping = new Dictionary<string, int>
        {
            ["First"] = 0,
            ["Second"] = 1,
            ["Third"] = 2
        };
        var row = new CsvRow(values, columnMapping);

        // act
        var result = row.GetColumnNames();

        // assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Length);
        Assert.Equal("First", result[0]);
        Assert.Equal("Second", result[1]);
        Assert.Equal("Third", result[2]);
    }

    [Fact]
    public void GetColumnNames_WithoutMapping_ReturnsNull()
    {
        // arrange
        var values = new[] { "a", "b", "c" };
        var row = new CsvRow(values);

        // act
        var result = row.GetColumnNames();

        // assert
        Assert.Null(result);
    }
}