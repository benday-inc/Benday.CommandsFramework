using System;
using System.IO;
using System.Linq;
using Benday.CommandsFramework.DataFormatting;

namespace Benday.CommandsFramework.Tests;

public class CsvReaderFixture
{
    public CsvReaderFixture()
    {
        _SystemUnderTest = null;
    }

    private CsvReader? _SystemUnderTest;

    private CsvReader SystemUnderTest
    {
        get
        {
            if (_SystemUnderTest == null)
            {
                _SystemUnderTest = new CsvReader("Name,Age,City\nJohn,30,New York\nJane,25,Boston");
            }

            return _SystemUnderTest;
        }
    }

    [Fact]
    public void Constructor_WithValidCsvContent_InitializesCorrectly()
    {
        // arrange
        var csvContent = "col1,col2\nval1,val2";

        // act
        var result = new CsvReader(csvContent);

        // assert
        Assert.NotNull(result);
        Assert.True(result.HasHeaderRow); // Default should be true
    }

    [Fact]
    public void Constructor_WithNullContent_ThrowsArgumentNullException()
    {
        // arrange
        string? csvContent = null;

        // act & assert
        Assert.Throws<ArgumentNullException>(() => new CsvReader(csvContent!));
    }

    [Fact]
    public void HasHeaderRow_DefaultValue_IsTrue()
    {
        // arrange
        var csvContent = "col1,col2\nval1,val2";

        // act
        var reader = new CsvReader(csvContent);

        // assert
        Assert.True(reader.HasHeaderRow);
    }

    [Fact]
    public void HasHeaderRow_CanBeSetToFalse()
    {
        // arrange
        var csvContent = "col1,col2\nval1,val2";
        var reader = new CsvReader(csvContent);

        // act
        reader.HasHeaderRow = false;

        // assert
        Assert.False(reader.HasHeaderRow);
    }

    [Fact]
    public void FromFile_ExistingFile_ReturnsReader()
    {
        // arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "Name,Age\nJohn,30\nJane,25");

        try
        {
            // act
            var reader = CsvReader.FromFile(tempFile);

            // assert
            Assert.NotNull(reader);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void FromFile_NullPath_ThrowsArgumentNullException()
    {
        // arrange
        string? filePath = null;

        // act & assert
        Assert.Throws<ArgumentNullException>(() => CsvReader.FromFile(filePath!));
    }

    [Fact]
    public void FromFile_NonExistentFile_ThrowsFileNotFoundException()
    {
        // arrange
        var nonExistentPath = "non_existent_file.csv";

        // act & assert
        Assert.Throws<FileNotFoundException>(() => CsvReader.FromFile(nonExistentPath));
    }

    [Fact]
    public void GetColumnNames_WithHeaderRow_ReturnsColumnNames()
    {
        // arrange
        var csvContent = "First Name,Last Name,Email\nJohn,Doe,john@example.com";
        var reader = new CsvReader(csvContent);

        // act
        var result = reader.GetColumnNames();

        // assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Length);
        Assert.Equal("First Name", result[0]);
        Assert.Equal("Last Name", result[1]);
        Assert.Equal("Email", result[2]);
    }

    [Fact]
    public void GetColumnNames_WithoutHeaderRow_ReturnsNull()
    {
        // arrange
        var csvContent = "John,Doe,john@example.com\nJane,Smith,jane@example.com";
        var reader = new CsvReader(csvContent) { HasHeaderRow = false };

        // act
        var result = reader.GetColumnNames();

        // assert
        Assert.Null(result);
    }

    [Fact]
    public void GetColumnNames_EmptyContent_ReturnsNull()
    {
        // arrange
        var reader = new CsvReader("");

        // act
        var result = reader.GetColumnNames();

        // assert
        Assert.Null(result);
    }

    [Fact]
    public void GetEnumerator_WithHeaderRow_ReturnsDataRowsOnly()
    {
        // arrange
        var csvContent = "Name,Age,City\nJohn,30,New York\nJane,25,Boston";
        var reader = new CsvReader(csvContent);

        // act
        var rows = reader.ToList();

        // assert
        Assert.Equal(2, rows.Count); // Should not include header row
        Assert.Equal("John", rows[0]["Name"]);
        Assert.Equal("30", rows[0]["Age"]);
        Assert.Equal("New York", rows[0]["City"]);
        Assert.Equal("Jane", rows[1]["Name"]);
        Assert.Equal("25", rows[1]["Age"]);
        Assert.Equal("Boston", rows[1]["City"]);
    }

    [Fact]
    public void GetEnumerator_WithoutHeaderRow_ReturnsAllRows()
    {
        // arrange
        var csvContent = "John,30,New York\nJane,25,Boston";
        var reader = new CsvReader(csvContent) { HasHeaderRow = false };

        // act
        var rows = reader.ToList();

        // assert
        Assert.Equal(2, rows.Count);
        Assert.Equal("John", rows[0][0]);
        Assert.Equal("30", rows[0][1]);
        Assert.Equal("New York", rows[0][2]);
        Assert.Equal("Jane", rows[1][0]);
        Assert.Equal("25", rows[1][1]);
        Assert.Equal("Boston", rows[1][2]);
    }

    [Fact]
    public void GetEnumerator_EmptyContent_ReturnsNoRows()
    {
        // arrange
        var reader = new CsvReader("");

        // act
        var rows = reader.ToList();

        // assert
        Assert.Empty(rows);
    }

    [Fact]
    public void GetEnumerator_OnlyHeaderRow_ReturnsNoDataRows()
    {
        // arrange
        var reader = new CsvReader("Name,Age,City");

        // act
        var rows = reader.ToList();

        // assert
        Assert.Empty(rows);
    }

    [Fact]
    public void GetEnumerator_WithQuotedValues_ParsesCorrectly()
    {
        // arrange
        var csvContent = "Name,Description\n\"John Doe\",\"A person with, comma in description\"\n\"Jane Smith\",\"Another \"\"quoted\"\" value\"";
        var reader = new CsvReader(csvContent);

        // act
        var rows = reader.ToList();

        // assert
        Assert.Equal(2, rows.Count);
        Assert.Equal("John Doe", rows[0]["Name"]);
        Assert.Equal("A person with, comma in description", rows[0]["Description"]);
        Assert.Equal("Jane Smith", rows[1]["Name"]);
        Assert.Equal("Another \"quoted\" value", rows[1]["Description"]);
    }

    [Fact]
    public void GetEnumerator_WithDifferentLineEndings_ParsesCorrectly()
    {
        // arrange & act & assert
        
        // Windows line endings (\r\n)
        var windowsCsv = "Name,Age\r\nJohn,30\r\nJane,25";
        var windowsReader = new CsvReader(windowsCsv);
        var windowsRows = windowsReader.ToList();
        Assert.Equal(2, windowsRows.Count);

        // Unix line endings (\n)
        var unixCsv = "Name,Age\nJohn,30\nJane,25";
        var unixReader = new CsvReader(unixCsv);
        var unixRows = unixReader.ToList();
        Assert.Equal(2, unixRows.Count);

        // Mac line endings (\r)
        var macCsv = "Name,Age\rJohn,30\rJane,25";
        var macReader = new CsvReader(macCsv);
        var macRows = macReader.ToList();
        Assert.Equal(2, macRows.Count);
    }

    [Fact]
    public void GetEnumerator_WithEmptyLines_SkipsEmptyLines()
    {
        // arrange
        var csvContent = "Name,Age\nJohn,30\n\nJane,25\n";
        var reader = new CsvReader(csvContent);

        // act
        var rows = reader.ToList();

        // assert
        Assert.Equal(2, rows.Count);
        Assert.Equal("John", rows[0]["Name"]);
        Assert.Equal("Jane", rows[1]["Name"]);
    }

    [Fact]
    public void GetEnumerator_WithWhitespaceInColumnNames_TrimsColumnNames()
    {
        // arrange
        var csvContent = " Name , Age , City \nJohn,30,New York";
        var reader = new CsvReader(csvContent);

        // act
        var rows = reader.ToList();

        // assert
        Assert.Single(rows);
        Assert.Equal("John", rows[0]["Name"]);
        Assert.Equal("30", rows[0]["Age"]);
        Assert.Equal("New York", rows[0]["City"]);
    }

    [Fact]
    public void GetEnumerator_CanIterateMultipleTimes()
    {
        // arrange
        var csvContent = "Name,Age\nJohn,30\nJane,25";
        var reader = new CsvReader(csvContent);

        // act
        var firstIteration = reader.ToList();
        var secondIteration = reader.ToList();

        // assert
        Assert.Equal(2, firstIteration.Count);
        Assert.Equal(2, secondIteration.Count);
        Assert.Equal(firstIteration[0]["Name"], secondIteration[0]["Name"]);
        Assert.Equal(firstIteration[1]["Name"], secondIteration[1]["Name"]);
    }

    [Fact]
    public void Integration_FileToEnumeration_WorksCorrectly()
    {
        // arrange
        var tempFile = Path.GetTempFileName();
        var csvContent = "Product,Price,Category\nLaptop,999.99,Electronics\nBook,19.99,Education\n\"Widget, Special\",29.99,\"Tools & Hardware\"";
        File.WriteAllText(tempFile, csvContent);

        try
        {
            // act
            var reader = CsvReader.FromFile(tempFile);
            var products = reader.ToList();

            // assert
            Assert.Equal(3, products.Count);
            
            Assert.Equal("Laptop", products[0]["Product"]);
            Assert.Equal("999.99", products[0]["Price"]);
            Assert.Equal("Electronics", products[0]["Category"]);

            Assert.Equal("Book", products[1]["Product"]);
            Assert.Equal("19.99", products[1]["Price"]);
            Assert.Equal("Education", products[1]["Category"]);

            Assert.Equal("Widget, Special", products[2]["Product"]);
            Assert.Equal("29.99", products[2]["Price"]);
            Assert.Equal("Tools & Hardware", products[2]["Category"]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void GetEnumerator_WithNewlinesInQuotedValues_ParsesCorrectly()
    {
        // arrange
        var csvContent = "Name,Description,Notes\n\"John Doe\",\"A person with\nnewline in description\",\"Simple note\"\n\"Jane Smith\",\"Another person\",\"Note with\nmultiple\nlines\"";
        var reader = new CsvReader(csvContent);

        // act
        var rows = reader.ToList();

        // assert
        Assert.Equal(2, rows.Count);
        
        // First row
        Assert.Equal("John Doe", rows[0]["Name"]);
        Assert.Equal("A person with\nnewline in description", rows[0]["Description"]);
        Assert.Equal("Simple note", rows[0]["Notes"]);
        
        // Second row
        Assert.Equal("Jane Smith", rows[1]["Name"]);
        Assert.Equal("Another person", rows[1]["Description"]);
        Assert.Equal("Note with\nmultiple\nlines", rows[1]["Notes"]);
    }

    [Fact]
    public void GetEnumerator_WithNewlinesAndCommasInQuotedValues_ParsesCorrectly()
    {
        // arrange
        var csvContent = "Product,Description\n\"Widget A\",\"Description with, comma and\nnewline together\"\n\"Widget B\",\"Another description\nwith newlines, commas, and\nmore content\"";
        var reader = new CsvReader(csvContent);

        // act
        var rows = reader.ToList();

        // assert
        Assert.Equal(2, rows.Count);
        
        // First row
        Assert.Equal("Widget A", rows[0]["Product"]);
        Assert.Equal("Description with, comma and\nnewline together", rows[0]["Description"]);
        
        // Second row
        Assert.Equal("Widget B", rows[1]["Product"]);
        Assert.Equal("Another description\nwith newlines, commas, and\nmore content", rows[1]["Description"]);
    }

    [Fact]
    public void GetEnumerator_WithCarriageReturnAndNewlineInQuotedValues_ParsesCorrectly()
    {
        // arrange
        var csvContent = "Name,Address\n\"John Doe\",\"123 Main St\r\nAnytown, ST 12345\"\n\"Jane Smith\",\"456 Oak Ave\r\nOther City, ST 67890\"";
        var reader = new CsvReader(csvContent);

        // act
        var rows = reader.ToList();

        // assert
        Assert.Equal(2, rows.Count);
        
        Assert.Equal("John Doe", rows[0]["Name"]);
        Assert.Equal("123 Main St\r\nAnytown, ST 12345", rows[0]["Address"]);
        
        Assert.Equal("Jane Smith", rows[1]["Name"]);
        Assert.Equal("456 Oak Ave\r\nOther City, ST 67890", rows[1]["Address"]);
    }

    [Fact]
    public void GetEnumerator_WithMixedLineEndingsInQuotedValues_ParsesCorrectly()
    {
        // arrange
        var csvContent = "Name,Comments\n\"User A\",\"Line 1\nLine 2\rLine 3\r\nLine 4\"\n\"User B\",\"Single line comment\"";
        var reader = new CsvReader(csvContent);

        // act
        var rows = reader.ToList();

        // assert
        Assert.Equal(2, rows.Count);
        
        Assert.Equal("User A", rows[0]["Name"]);
        Assert.Equal("Line 1\nLine 2\rLine 3\r\nLine 4", rows[0]["Comments"]);
        
        Assert.Equal("User B", rows[1]["Name"]);
        Assert.Equal("Single line comment", rows[1]["Comments"]);
    }

    [Fact]
    public void GetEnumerator_WithEmptyQuotedFieldsAndNewlines_ParsesCorrectly()
    {
        // arrange
        var csvContent = "Name,Description,Notes\n\"John\",\"\",\"Has empty description\"\n\"Jane\",\"Description\nwith newline\",\"\"";
        var reader = new CsvReader(csvContent);

        // act
        var rows = reader.ToList();

        // assert
        Assert.Equal(2, rows.Count);
        
        Assert.Equal("John", rows[0]["Name"]);
        Assert.Equal("", rows[0]["Description"]);
        Assert.Equal("Has empty description", rows[0]["Notes"]);
        
        Assert.Equal("Jane", rows[1]["Name"]);
        Assert.Equal("Description\nwith newline", rows[1]["Description"]);
        Assert.Equal("", rows[1]["Notes"]);
    }
}