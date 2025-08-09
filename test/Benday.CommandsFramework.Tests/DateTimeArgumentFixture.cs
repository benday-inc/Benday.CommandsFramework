using System.Globalization;
namespace Benday.CommandsFramework.Tests;

public class DateTimeArgumentFixture
{
        public DateTimeArgumentFixture()
    {
        _SystemUnderTest = null;
    }

    private DateTimeArgument? _SystemUnderTest;
    private const string EXPECTED_ARG_NAME = "arg123";
#pragma warning disable IDE1006 // Naming Styles
    private readonly DateTime EXPECTED_ARG_VALUE = DateTime.MaxValue;
#pragma warning restore IDE1006 // Naming Styles
    private const string EXPECTED_ARG_DESC = "argvalue123 description";
    private const bool EXPECTED_ARG_ISREQUIRED = true;
    private const bool EXPECTED_ARG_ALLOWEMPTYVALUE = true;
    private const ArgumentDataType EXPECTED_ARG_DATATYPE = ArgumentDataType.DateTime;


    private DateTimeArgument SystemUnderTest
    {
        get
        {
            if (_SystemUnderTest == null)
            {
                InitializeWithNoArgs();
            }

            if (_SystemUnderTest == null)
            {
                throw new InvalidOperationException($"System under test is null");
            }

            return _SystemUnderTest;
        }
    }

    private void InitializeWithNoArgs()
    {
        // _SystemUnderTest = new Argument<string>();
        throw new NotImplementedException();
    }

    private void InitializeWithAllTheArgs()
    {
        var arg = new DateTimeArgument(EXPECTED_ARG_NAME)
            .WithDescription(EXPECTED_ARG_DESC)
            .AsRequired()
            .AllowEmptyValue();

        arg.Value = EXPECTED_ARG_VALUE;

        var temp = arg as DateTimeArgument ?? throw new InvalidOperationException("Wrong type");

        _SystemUnderTest = temp;
    }

    [Fact]
    public void Ctor_WithAllValues()
    {
        // arrange

        // act
        InitializeWithAllTheArgs();

        // assert
        Assert.Equal(EXPECTED_ARG_DESC, SystemUnderTest.Description);
        Assert.Equal(EXPECTED_ARG_VALUE, SystemUnderTest.Value);
        Assert.Equal(EXPECTED_ARG_NAME, SystemUnderTest.Name);
        Assert.Equal(EXPECTED_ARG_ISREQUIRED, SystemUnderTest.IsRequired);
        Assert.Equal(EXPECTED_ARG_DESC, SystemUnderTest.Description);
        Assert.Equal(EXPECTED_ARG_DATATYPE, SystemUnderTest.DataType);
        Assert.Equal(EXPECTED_ARG_ALLOWEMPTYVALUE, SystemUnderTest.AllowEmptyValue);
    }

    [Fact]
    public void IsValid_Required_ValidValue_True()
    {
        // arrange
        InitializeWithAllTheArgs();
        SystemUnderTest.AllowEmptyValue = false;
        var expected = true;
        SystemUnderTest.Value = DateTime.Now;

        // act
        var actual = SystemUnderTest.Validate();

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TrySetValue_False_NullString()
    {
        // arrange
        InitializeWithAllTheArgs();

        var expected = false;
        string? input = null;

        // act
        var actual = SystemUnderTest.TrySetValue(input!);

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TrySetValue_False_NotADateTime()
    {
        // arrange
        InitializeWithAllTheArgs();

        var expected = false;
        string input = "asdf";

        // act
        var actual = SystemUnderTest.TrySetValue(input!);

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TrySetValue_True_ValidDateTime()
    {
        // arrange
        InitializeWithAllTheArgs();
        var expected = true;
        string input = "12/1/2022";
        var expectedValue = new DateTime(2022, 12, 1);

        // act
        var actual = SystemUnderTest.TrySetValue(input);

        // assert
        Assert.Equal(expected, actual);
        Assert.Equal(expectedValue, SystemUnderTest.Value);
    }

    [Theory]
    [InlineData("12/1/2022", true, "12/1/2022 12:00:00 AM")]
    [InlineData("12/31/2022 18:21:14", true, "12/31/2022 6:21:14 PM")]
    [InlineData("12/1/2022 6:21:14 PM", true, "12/1/2022 6:21:14 PM")]
    [InlineData("12/1/2022 6:21:14 AM", true, "12/1/2022 6:21:14 AM")]
    [InlineData("20240816T1750349136Z", true, "8/16/2024 5:50:34 PM")]
    [InlineData("20240816Z", true, "8/16/2024 12:00:00 AM")]
    [InlineData("2024-08-16T17:29:39Z", true, "8/16/2024 5:29:39 PM")]
    [InlineData("asdf", false, "")]
    [InlineData(null, false, "")]
    [InlineData("", false, "")]
    public void TrySetValueAndVerifyValue(string input, bool expectedOutcome, string expectedDateString)
    {
        // arrange

        CultureInfo enUSCulture = new CultureInfo("en-US");
        
        var arg = new DateTimeArgument(EXPECTED_ARG_NAME);             
        arg.IsRequired = true;
        arg.CultureInfo = enUSCulture;
        
        var temp = arg as DateTimeArgument ?? throw new InvalidOperationException("Wrong type");

        _SystemUnderTest = temp;

        // act
        var actual = SystemUnderTest.TrySetValue(input);

        // assert
        Assert.Equal(expectedOutcome, actual);

        if (expectedOutcome == true)
        {
            var actualDateString = SystemUnderTest.Value.ToString("M/d/yyyy h:mm:ss tt");

            Assert.Equal(expectedDateString, actualDateString);
        }
    }

    [Fact]
    public void GetTimeZone()
    {
        var localZone = TimeZoneInfo.Local;

        Console.WriteLine(localZone);
    }


}
