using System;
using Benday.CommandsFramework.DataFormatting;

namespace Benday.CommandsFramework.Tests;

public class TableFormatterFixture
{
        public TableFormatterFixture()
    {
        _SystemUnderTest = null;
    }

    private TableFormatter? _SystemUnderTest;

    private TableFormatter SystemUnderTest
    {
        get
        {
            if (_SystemUnderTest == null)
            {
                _SystemUnderTest = new TableFormatter();
            }

            return _SystemUnderTest;
        }
    }

    [Fact]
    public void MultiColumn_PopulatesColumnWidths()
    {
        // arrange
        SystemUnderTest.AddColumn("Last Name");
        SystemUnderTest.AddColumn("First Name");
        SystemUnderTest.AddColumn("Email Address");

        var data = GetTestData();

        var longestLastName = data.Max(x => x.LastName.Length);
        var longestFirstName = data.Max(x => x.FirstName.Length);
        var longestEmailAddress = data.Max(x => x.EmailAddress.Length);

        // act
        foreach (var item in data)
        {
            SystemUnderTest.AddData(item.LastName, item.FirstName, item.EmailAddress);
        }

        // assert
        Assert.Equal(longestLastName, SystemUnderTest.Columns[0].WidthOfLongestValue);
        Assert.Equal(longestFirstName, SystemUnderTest.Columns[1].WidthOfLongestValue);
        Assert.Equal(longestEmailAddress, SystemUnderTest.Columns[2].WidthOfLongestValue);
    }

    [Fact]
    public void FormatTable()
    {
        // arrange
        SystemUnderTest.AddColumn("Last Name");
        SystemUnderTest.AddColumn("First Name");
        SystemUnderTest.AddColumn("Email Address");

        var data = GetTestData();

        var expectedLineLength = 50;

        var longestLastName = data.Max(x => x.LastName.Length);
        var longestFirstName = data.Max(x => x.FirstName.Length);
        var longestEmailAddress = data.Max(x => x.EmailAddress.Length);
        
        // act
        foreach (var item in data)
        {
            SystemUnderTest.AddData(item.LastName, item.FirstName, item.EmailAddress);
        }

        // assert
        var result = SystemUnderTest.FormatTable();

        var lines = result.Split(Environment.NewLine);

        if (lines.Length == 0)
        {
            Assert.Fail("No lines in result.");
        }
        else if (lines.Length == 1)
        {
            Assert.Fail("Only header row in result.");
        }
        else
        {
            var lastLine = lines[lines.Length - 1];

            if (string.IsNullOrWhiteSpace(lastLine) == true)
            {
                // it's ok for the last line to be empty
                // remove the last line if it's empty
                var temp = lines.ToList();
                temp.RemoveAt(temp.Count - 1);
                lines = temp.ToArray();
            }
        }

        Assert.Equal(data.Count + 1, lines.Length);

        var actualRowLength = SystemUnderTest.Columns.Sum(x => x.Width) + SystemUnderTest.Columns.Count - 1;

        Assert.Equal(expectedLineLength, actualRowLength);

        var expectedHeaderRow = $"{SystemUnderTest.Columns[0].NamePadded} {SystemUnderTest.Columns[1].NamePadded} {SystemUnderTest.Columns[2].NamePadded}";
        Assert.Equal(expectedHeaderRow, lines[0]);
        Assert.Equal(expectedLineLength, lines[0].Length);

        var lineNumber = 0;

        foreach (var line in lines)
        {
            if (lineNumber == 0)
            {
                // skip the header row
                lineNumber++;
                continue;
            }

            Assert.Equal(expectedLineLength, line.Length);
            lineNumber++;
        }



        for (var index = 0; index < data.Count; index++)
        {
            var item = data[index];
            var line = lines[index + 1];

            var paddedLastName = item.LastName.PadRight(SystemUnderTest.Columns[0].Width);
            var paddedFirstName = item.FirstName.PadRight(SystemUnderTest.Columns[1].Width);
            var paddedEmailAddress = item.EmailAddress.PadRight(SystemUnderTest.Columns[2].Width);

            var expectedLine = $"{paddedLastName} {paddedFirstName} {paddedEmailAddress}";

            Assert.Equal(expectedLine, line);
        }
    }

    private List<Person> GetTestData()
    {
        var returnValues = new List<Person>();

        returnValues.Add(new Person()
        {
            LastName = "Smith",
            FirstName = "John",
            EmailAddress = "john.smith@email.com"
        });

        returnValues.Add(new Person()
        {
            LastName = "Jones",
            FirstName = "Sally",
            EmailAddress = "sally.jones@emailthingy.org"
        });

        returnValues.Add(new Person()
        {
            LastName = "Subramanian",
            FirstName = "Rajesh",
            EmailAddress = "rj@stuff.net"
        });

        returnValues.Add(new Person()
        {
            LastName = "Ng",
            FirstName = "Paul",
            EmailAddress = "rj@stuff.net"
        });

        return returnValues;
    }

    private class Person
    {
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
    }
}