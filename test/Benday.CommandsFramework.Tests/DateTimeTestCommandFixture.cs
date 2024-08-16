using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

[TestClass]
public class DateTimeTestCommandFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _SystemUnderTest = null;
        _OutputProvider = null;
    }

    private DateTimeTestCommand? _SystemUnderTest;

    private DateTimeTestCommand SystemUnderTest
    {
        get
        {
            Assert.IsNotNull(_SystemUnderTest);

            return _SystemUnderTest;
        }
    }

    private StringBuilderTextOutputProvider? _OutputProvider;

    private StringBuilderTextOutputProvider OutputProvider
    {
        get
        {
            if (_OutputProvider == null)
            {
                _OutputProvider = new StringBuilderTextOutputProvider();
            }

            return _OutputProvider;
        }
    }


    [TestMethod]
    public void CreateAndRun_ValidArgs_MonthDayYear()
    {
        // arrange
        var inputDateString = "12/24/2022";
        var expectedDateString = "20221224T0500000000Z";

        var commandLineArgs = Utilities.GetStringArray(
            "datetimetest",
            $"/date:{inputDateString}"
            );

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new DateTimeTestCommand(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert        
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.IsTrue(output.Contains("** SUCCESS **"), "Did not contain expected string");
        Assert.IsTrue(output.Contains(expectedDateString), $"Did not contain expected string: {expectedDateString}");
    }


    [TestMethod]
    public void CreateAndRun_ValidArgs_MonthDayYearHourMin_24h()
    {
        // arrange
        var inputDateString = "12/24/2022 14:30";
        var expectedDateString = "20221224T1930000000Z";

        var commandLineArgs = Utilities.GetStringArray(
            "datetimetest",
            $"/date:{inputDateString}"
            );

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new DateTimeTestCommand(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert        
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.IsTrue(output.Contains("** SUCCESS **"), "Did not contain expected string");
        Assert.IsTrue(output.Contains(expectedDateString), $"Did not contain expected string: {expectedDateString}");
    }

    [TestMethod]
    public void CreateAndRun_ValidArgs_MonthDayYearHourMinSec_24h()
    {
        // arrange
        var inputDateString = "12/24/2022 14:30:30";
        var expectedDateString = "20221224T1930300000Z";

        var commandLineArgs = Utilities.GetStringArray(
            "datetimetest",
            $"/date:{inputDateString}"
            );

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new DateTimeTestCommand(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert        
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.IsTrue(output.Contains("** SUCCESS **"), "Did not contain expected string");
        Assert.IsTrue(output.Contains(expectedDateString), $"Did not contain expected string: {expectedDateString}");
    }

    [TestMethod]
    public void CreateAndRun_ValidArgs_FileDateTimeUniversal()
    {
        // arrange

        var inputDateString = "20240816T1515295960Z";
        var expectedDateString = "20240816T1515295960Z";

        var commandLineArgs = Utilities.GetStringArray(
            "datetimetest",
            $"/date:{inputDateString}"
            );

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new DateTimeTestCommand(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert        
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.IsTrue(output.Contains("** SUCCESS **"), "Did not contain expected string");
        Assert.IsTrue(output.Contains(expectedDateString), $"Did not contain expected string: {expectedDateString}");
    }

    [TestMethod]
    public void CreateAndRun_InvalidDate()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            "datetimetest",
            "/date:notADateValue"
            );

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new DateTimeTestCommand(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert        
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.IsTrue(output.Contains("** INVALID ARGUMENT **"), "Did not contain expected string");
    }    
}
