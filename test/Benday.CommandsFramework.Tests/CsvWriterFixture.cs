using System;
using System.IO;
using System.Linq;
using Benday.CommandsFramework.DataFormatting;

namespace Benday.CommandsFramework.Tests;

public class CsvWriterFixture
{
    public CsvWriterFixture()
    {
        _SystemUnderTest = null;
    }

    private CsvWriter? _SystemUnderTest;

    private CsvWriter SystemUnderTest
    {
        get
        {
            if (_SystemUnderTest == null)
            {
                _SystemUnderTest = new CsvWriter();
            }

            return _SystemUnderTest;
        }
    }

    [Fact]
    public void Constructor_Default_InitializesCorrectly()
    {
        // arrange & act
        var result = new CsvWriter();

        // assert
        Assert.NotNull(result);
        Assert.True(result.HasHeaderRow); // Default should be true
        Assert.Equal(0, result.RowCount);
        Assert.Equal(0, result.ColumnCount);
    }

    [Fact]
    public void Constructor_WithHasHeaderRowFalse_InitializesCorrectly()
    {
        // arrange & act
        var result = new CsvWriter(false);

        // assert
        Assert.NotNull(result);
        Assert.False(result.HasHeaderRow);
        Assert.Equal(0, result.RowCount);
        Assert.Equal(0, result.ColumnCount);
    }

    [Fact]
    public void Constructor_WithCsvReader_LoadsDataCorrectly()
    {
        // arrange
        var csvContent = "Name,Age,City\nJohn,30,New York\nJane,25,Boston";
        var reader = new CsvReader(csvContent);

        // act
        var writer = new CsvWriter(reader);

        // assert
        Assert.True(writer.HasHeaderRow);
        Assert.Equal(2, writer.RowCount);
        Assert.Equal(3, writer.ColumnCount);
        
        var headers = writer.GetHeaders();
        Assert.NotNull(headers);
        Assert.Equal(new[] { "Name", "Age", "City" }, headers);
        
        Assert.Equal("John", writer.GetValue(0, "Name"));
        Assert.Equal("30", writer.GetValue(0, "Age"));
        Assert.Equal("New York", writer.GetValue(0, "City"));
        Assert.Equal("Jane", writer.GetValue(1, "Name"));
        Assert.Equal("25", writer.GetValue(1, "Age"));
        Assert.Equal("Boston", writer.GetValue(1, "City"));
    }

    [Fact]
    public void Constructor_WithNullCsvReader_ThrowsArgumentNullException()
    {
        // arrange
        CsvReader? reader = null;

        // act & assert
        Assert.Throws<ArgumentNullException>(() => new CsvWriter(reader!));
    }

    [Fact]
    public void SetHeaders_WithValidHeaders_SetsHeadersCorrectly()
    {
        // arrange
        var writer = new CsvWriter();
        var headers = new[] { "Name", "Age", "Email" };

        // act
        writer.SetHeaders(headers);

        // assert
        var result = writer.GetHeaders();
        Assert.NotNull(result);
        Assert.Equal(headers, result);
        Assert.Equal(3, writer.ColumnCount);
    }

    [Fact]
    public void SetHeaders_WithNullHeaders_ThrowsArgumentNullException()
    {
        // arrange
        var writer = new CsvWriter();
        string[]? headers = null;

        // act & assert
        Assert.Throws<ArgumentNullException>(() => writer.SetHeaders(headers!));
    }

    [Fact]
    public void SetHeaders_WithHasHeaderRowFalse_ThrowsInvalidOperationException()
    {
        // arrange
        var writer = new CsvWriter(false);
        var headers = new[] { "Name", "Age" };

        // act & assert
        Assert.Throws<InvalidOperationException>(() => writer.SetHeaders(headers));
    }

    [Fact]
    public void GetHeaders_WithNoHeaders_ReturnsNull()
    {
        // arrange
        var writer = new CsvWriter();

        // act
        var result = writer.GetHeaders();

        // assert
        Assert.Null(result);
    }

    [Fact]
    public void GetHeaders_WithHasHeaderRowFalse_ReturnsNull()
    {
        // arrange
        var writer = new CsvWriter(false);

        // act
        var result = writer.GetHeaders();

        // assert
        Assert.Null(result);
    }

    [Fact]
    public void AddRow_WithStringArray_AddsRowCorrectly()
    {
        // arrange
        var writer = new CsvWriter();
        var values = new[] { "John", "30", "Engineer" };

        // act
        writer.AddRow(values);

        // assert
        Assert.Equal(1, writer.RowCount);
        Assert.Equal("John", writer.GetValue(0, 0));
        Assert.Equal("30", writer.GetValue(0, 1));
        Assert.Equal("Engineer", writer.GetValue(0, 2));
    }

    [Fact]
    public void AddRow_WithCsvRow_AddsRowCorrectly()
    {
        // arrange
        var writer = new CsvWriter();
        var csvRow = new CsvRow(new[] { "John", "30", "Engineer" });

        // act
        writer.AddRow(csvRow);

        // assert
        Assert.Equal(1, writer.RowCount);
        Assert.Equal("John", writer.GetValue(0, 0));
        Assert.Equal("30", writer.GetValue(0, 1));
        Assert.Equal("Engineer", writer.GetValue(0, 2));
    }

    [Fact]
    public void AddRow_WithNullStringArray_ThrowsArgumentNullException()
    {
        // arrange
        var writer = new CsvWriter();
        string[]? values = null;

        // act & assert
        Assert.Throws<ArgumentNullException>(() => writer.AddRow(values!));
    }

    [Fact]
    public void AddRow_WithNullCsvRow_ThrowsArgumentNullException()
    {
        // arrange
        var writer = new CsvWriter();
        CsvRow? row = null;

        // act & assert
        Assert.Throws<ArgumentNullException>(() => writer.AddRow(row!));
    }

    [Fact]
    public void InsertRow_AtValidIndex_InsertsRowCorrectly()
    {
        // arrange
        var writer = new CsvWriter();
        writer.AddRow("First", "Row");
        writer.AddRow("Third", "Row");

        // act
        writer.InsertRow(1, "Second", "Row");

        // assert
        Assert.Equal(3, writer.RowCount);
        Assert.Equal("First", writer.GetValue(0, 0));
        Assert.Equal("Second", writer.GetValue(1, 0));
        Assert.Equal("Third", writer.GetValue(2, 0));
    }

    [Fact]
    public void InsertRow_AtBeginning_InsertsRowCorrectly()
    {
        // arrange
        var writer = new CsvWriter();
        writer.AddRow("Second", "Row");

        // act
        writer.InsertRow(0, "First", "Row");

        // assert
        Assert.Equal(2, writer.RowCount);
        Assert.Equal("First", writer.GetValue(0, 0));
        Assert.Equal("Second", writer.GetValue(1, 0));
    }

    [Fact]
    public void InsertRow_AtEnd_InsertsRowCorrectly()
    {
        // arrange
        var writer = new CsvWriter();
        writer.AddRow("First", "Row");

        // act
        writer.InsertRow(1, "Second", "Row");

        // assert
        Assert.Equal(2, writer.RowCount);
        Assert.Equal("First", writer.GetValue(0, 0));
        Assert.Equal("Second", writer.GetValue(1, 0));
    }

    [Fact]
    public void InsertRow_WithInvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        // arrange
        var writer = new CsvWriter();
        writer.AddRow("Test", "Row");

        // act & assert
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.InsertRow(-1, "Invalid"));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.InsertRow(2, "Invalid"));
    }

    [Fact]
    public void InsertRow_WithNullValues_ThrowsArgumentNullException()
    {
        // arrange
        var writer = new CsvWriter();
        string[]? values = null;

        // act & assert
        Assert.Throws<ArgumentNullException>(() => writer.InsertRow(0, values!));
    }

    [Fact]
    public void RemoveRow_WithValidIndex_RemovesRowCorrectly()
    {
        // arrange
        var writer = new CsvWriter();
        writer.AddRow("First", "Row");
        writer.AddRow("Second", "Row");
        writer.AddRow("Third", "Row");

        // act
        writer.RemoveRow(1);

        // assert
        Assert.Equal(2, writer.RowCount);
        Assert.Equal("First", writer.GetValue(0, 0));
        Assert.Equal("Third", writer.GetValue(1, 0));
    }

    [Fact]
    public void RemoveRow_WithInvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        // arrange
        var writer = new CsvWriter();
        writer.AddRow("Test", "Row");

        // act & assert
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.RemoveRow(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.RemoveRow(1));
    }

    [Fact]
    public void GetRow_WithValidIndex_ReturnsCorrectCsvRow()
    {
        // arrange
        var writer = new CsvWriter();
        writer.SetHeaders(new[] { "Name", "Age" });
        writer.AddRow("John", "30");

        // act
        var row = writer.GetRow(0);

        // assert
        Assert.NotNull(row);
        Assert.Equal("John", row["Name"]);
        Assert.Equal("30", row["Age"]);
        Assert.Equal("John", row[0]);
        Assert.Equal("30", row[1]);
    }

    [Fact]
    public void GetRow_WithInvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        // arrange
        var writer = new CsvWriter();
        writer.AddRow("Test", "Row");

        // act & assert
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.GetRow(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.GetRow(1));
    }

    [Fact]
    public void SetRow_WithValidIndex_SetsRowCorrectly()
    {
        // arrange
        var writer = new CsvWriter();
        writer.AddRow("Old", "Values");

        // act
        writer.SetRow(0, "New", "Values");

        // assert
        Assert.Equal("New", writer.GetValue(0, 0));
        Assert.Equal("Values", writer.GetValue(0, 1));
    }

    [Fact]
    public void SetRow_WithInvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        // arrange
        var writer = new CsvWriter();
        writer.AddRow("Test", "Row");

        // act & assert
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.SetRow(-1, "Invalid"));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.SetRow(1, "Invalid"));
    }

    [Fact]
    public void SetRow_WithNullValues_ThrowsArgumentNullException()
    {
        // arrange
        var writer = new CsvWriter();
        writer.AddRow("Test", "Row");
        string[]? values = null;

        // act & assert
        Assert.Throws<ArgumentNullException>(() => writer.SetRow(0, values!));
    }

    [Fact]
    public void GetValue_ByRowAndColumn_ReturnsCorrectValue()
    {
        // arrange
        var writer = new CsvWriter();
        writer.AddRow("John", "30", "Engineer");

        // act & assert
        Assert.Equal("John", writer.GetValue(0, 0));
        Assert.Equal("30", writer.GetValue(0, 1));
        Assert.Equal("Engineer", writer.GetValue(0, 2));
    }

    [Fact]
    public void GetValue_ByRowAndColumnName_ReturnsCorrectValue()
    {
        // arrange
        var writer = new CsvWriter();
        writer.SetHeaders(new[] { "Name", "Age", "Job" });
        writer.AddRow("John", "30", "Engineer");

        // act & assert
        Assert.Equal("John", writer.GetValue(0, "Name"));
        Assert.Equal("30", writer.GetValue(0, "Age"));
        Assert.Equal("Engineer", writer.GetValue(0, "Job"));
    }

    [Fact]
    public void GetValue_WithInvalidRowIndex_ThrowsArgumentOutOfRangeException()
    {
        // arrange
        var writer = new CsvWriter();
        writer.AddRow("Test", "Row");

        // act & assert
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.GetValue(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.GetValue(1, 0));
    }

    [Fact]
    public void GetValue_WithInvalidColumnIndex_ThrowsArgumentOutOfRangeException()
    {
        // arrange
        var writer = new CsvWriter();
        writer.AddRow("Test", "Row");

        // act & assert
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.GetValue(0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.GetValue(0, 2));
    }

    [Fact]
    public void GetValue_WithNullColumnName_ThrowsArgumentNullException()
    {
        // arrange
        var writer = new CsvWriter();
        writer.SetHeaders(new[] { "Name" });
        writer.AddRow("John");
        string? columnName = null;

        // act & assert
        Assert.Throws<ArgumentNullException>(() => writer.GetValue(0, columnName!));
    }

    [Fact]
    public void GetValue_WithNoHeaders_ThrowsInvalidOperationException()
    {
        // arrange
        var writer = new CsvWriter();
        writer.AddRow("John");

        // act & assert
        Assert.Throws<InvalidOperationException>(() => writer.GetValue(0, "Name"));
    }

    [Fact]
    public void GetValue_WithNonExistentColumnName_ThrowsInvalidOperationException()
    {
        // arrange
        var writer = new CsvWriter();
        writer.SetHeaders(new[] { "Name" });
        writer.AddRow("John");

        // act & assert
        Assert.Throws<InvalidOperationException>(() => writer.GetValue(0, "NonExistent"));
    }

    [Fact]
    public void SetValue_ByRowAndColumn_SetsValueCorrectly()
    {
        // arrange
        var writer = new CsvWriter();
        writer.AddRow("Old", "Value");

        // act
        writer.SetValue(0, 0, "New");

        // assert
        Assert.Equal("New", writer.GetValue(0, 0));
        Assert.Equal("Value", writer.GetValue(0, 1));
    }

    [Fact]
    public void SetValue_ByRowAndColumnName_SetsValueCorrectly()
    {
        // arrange
        var writer = new CsvWriter();
        writer.SetHeaders(new[] { "Name", "Age" });
        writer.AddRow("John", "30");

        // act
        writer.SetValue(0, "Name", "Jane");

        // assert
        Assert.Equal("Jane", writer.GetValue(0, "Name"));
        Assert.Equal("30", writer.GetValue(0, "Age"));
    }

    [Fact]
    public void SetValue_WithNullValue_SetsEmptyString()
    {
        // arrange
        var writer = new CsvWriter();
        writer.AddRow("Old");

        // act
        writer.SetValue(0, 0, null!);

        // assert
        Assert.Equal(string.Empty, writer.GetValue(0, 0));
    }

    [Fact]
    public void ToCsvString_WithHeadersAndData_GeneratesCorrectCsv()
    {
        // arrange
        var writer = new CsvWriter();
        writer.SetHeaders(new[] { "Name", "Age", "City" });
        writer.AddRow("John", "30", "New York");
        writer.AddRow("Jane", "25", "Boston");

        // act
        var result = writer.ToCsvString();

        // assert
        var expected = "Name,Age,City\nJohn,30,New York\nJane,25,Boston";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToCsvString_WithoutHeaders_GeneratesCorrectCsv()
    {
        // arrange
        var writer = new CsvWriter(false);
        writer.AddRow("John", "30", "New York");
        writer.AddRow("Jane", "25", "Boston");

        // act
        var result = writer.ToCsvString();

        // assert
        var expected = "John,30,New York\nJane,25,Boston";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToCsvString_WithCommasInValues_QuotesFieldsCorrectly()
    {
        // arrange
        var writer = new CsvWriter();
        writer.SetHeaders(new[] { "Name", "Description" });
        writer.AddRow("John Doe", "A person with, comma in description");

        // act
        var result = writer.ToCsvString();

        // assert
        var expected = "Name,Description\nJohn Doe,\"A person with, comma in description\"";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToCsvString_WithNewlinesInValues_QuotesFieldsCorrectly()
    {
        // arrange
        var writer = new CsvWriter();
        writer.SetHeaders(new[] { "Name", "Address" });
        writer.AddRow("John Doe", "123 Main St\nAnytown, USA");

        // act
        var result = writer.ToCsvString();

        // assert
        var expected = "Name,Address\nJohn Doe,\"123 Main St\nAnytown, USA\"";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToCsvString_WithQuotesInValues_EscapesQuotesCorrectly()
    {
        // arrange
        var writer = new CsvWriter();
        writer.SetHeaders(new[] { "Name", "Quote" });
        writer.AddRow("John", "He said \"Hello World\"");

        // act
        var result = writer.ToCsvString();

        // assert
        var expected = "Name,Quote\nJohn,\"He said \"\"Hello World\"\"\"";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToCsvString_WithEmptyValues_HandlesCorrectly()
    {
        // arrange
        var writer = new CsvWriter();
        writer.SetHeaders(new[] { "Name", "Middle", "Last" });
        writer.AddRow("John", "", "Doe");
        writer.AddRow("", "Middle", "");

        // act
        var result = writer.ToCsvString();

        // assert
        var expected = "Name,Middle,Last\nJohn,,Doe\n,Middle,";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToCsvString_WithEmptyWriter_ReturnsEmptyString()
    {
        // arrange
        var writer = new CsvWriter(false);

        // act
        var result = writer.ToCsvString();

        // assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void SaveToFile_WithValidPath_WritesFileCorrectly()
    {
        // arrange
        var writer = new CsvWriter();
        writer.SetHeaders(new[] { "Name", "Age" });
        writer.AddRow("John", "30");
        writer.AddRow("Jane", "25");

        var tempFile = Path.GetTempFileName();

        try
        {
            // act
            writer.SaveToFile(tempFile);

            // assert
            Assert.True(File.Exists(tempFile));
            var content = File.ReadAllText(tempFile);
            var expected = "Name,Age\nJohn,30\nJane,25";
            Assert.Equal(expected, content);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void SaveToFile_WithNullPath_ThrowsArgumentNullException()
    {
        // arrange
        var writer = new CsvWriter();
        string? filePath = null;

        // act & assert
        Assert.Throws<ArgumentNullException>(() => writer.SaveToFile(filePath!));
    }

    [Fact]
    public void Integration_CsvReaderToCsvWriter_RoundTripWorksCorrectly()
    {
        // arrange
        var originalCsv = "Product,Price,Category\nLaptop,999.99,Electronics\n\"Widget, Special\",29.99,\"Tools & Hardware\"\n\"Book\nMultiline\",19.99,\"Education\"";
        var reader = new CsvReader(originalCsv);
        var writer = new CsvWriter(reader);

        // act
        var result = writer.ToCsvString();

        // assert - Should preserve the data correctly
        var expectedReader = new CsvReader(result);
        var originalRows = reader.ToList();
        var resultRows = expectedReader.ToList();

        Assert.Equal(originalRows.Count, resultRows.Count);
        
        for (int i = 0; i < originalRows.Count; i++)
        {
            var originalRow = originalRows[i];
            var resultRow = resultRows[i];
            
            Assert.Equal(originalRow.ColumnCount, resultRow.ColumnCount);
            
            for (int j = 0; j < originalRow.ColumnCount; j++)
            {
                Assert.Equal(originalRow[j], resultRow[j]);
            }
        }
    }

    [Fact]
    public void Integration_EditExistingCsv_WorksCorrectly()
    {
        // arrange
        var originalCsv = "Name,Age,City\nJohn,30,New York\nJane,25,Boston";
        var reader = new CsvReader(originalCsv);
        var writer = new CsvWriter(reader);

        // act - Edit existing data
        writer.SetValue(0, "Age", "31");
        writer.SetValue(1, "City", "Chicago");
        writer.AddRow("Bob", "35", "Seattle");

        var result = writer.ToCsvString();

        // assert
        var expected = "Name,Age,City\nJohn,31,New York\nJane,25,Chicago\nBob,35,Seattle";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ColumnCount_WithHeaders_ReturnsHeaderCount()
    {
        // arrange
        var writer = new CsvWriter();
        writer.SetHeaders(new[] { "Col1", "Col2", "Col3" });

        // act & assert
        Assert.Equal(3, writer.ColumnCount);
    }

    [Fact]
    public void ColumnCount_WithDataButNoHeaders_ReturnsFirstRowColumnCount()
    {
        // arrange
        var writer = new CsvWriter(false);
        writer.AddRow("A", "B", "C");
        writer.AddRow("D", "E"); // Different column count

        // act & assert
        Assert.Equal(3, writer.ColumnCount); // Should use first row
    }

    [Fact]
    public void ColumnCount_WithNoDataOrHeaders_ReturnsZero()
    {
        // arrange
        var writer = new CsvWriter();

        // act & assert
        Assert.Equal(0, writer.ColumnCount);
    }
}