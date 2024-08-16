using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Benday.CommandsFramework.Tests;

[TestClass]
public class DateTimeArgumentFixture
{
    [TestInitialize]
    public void OnTestInitialize()
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

    [TestMethod]
    public void Ctor_WithAllValues()
    {
        // arrange

        // act
        InitializeWithAllTheArgs();

        // assert
        Assert.AreEqual<string>(EXPECTED_ARG_DESC, SystemUnderTest.Description, "Description was wrong");
        Assert.AreEqual<DateTime>(EXPECTED_ARG_VALUE, SystemUnderTest.Value, "Value was wrong");
        Assert.AreEqual<string>(EXPECTED_ARG_NAME, SystemUnderTest.Name, "Name was wrong");
        Assert.AreEqual<bool>(EXPECTED_ARG_ISREQUIRED, SystemUnderTest.IsRequired, "IsRequired was wrong");
        Assert.AreEqual<string>(EXPECTED_ARG_DESC, SystemUnderTest.Description, "Description was wrong");
        Assert.AreEqual<ArgumentDataType>(EXPECTED_ARG_DATATYPE, SystemUnderTest.DataType, "DataType was wrong");
        Assert.AreEqual<bool>(EXPECTED_ARG_ALLOWEMPTYVALUE, SystemUnderTest.AllowEmptyValue, "AllowEmptyValue was wrong");
    }

    [TestMethod]
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
        Assert.AreEqual<bool>(expected, actual, "Validation value is wrong");
    }

    [TestMethod]
    public void TrySetValue_False_NullString()
    {
        // arrange
        InitializeWithAllTheArgs();

        var expected = false;
        string input = null;

        // act
        var actual = SystemUnderTest.TrySetValue(input!);

        // assert
        Assert.AreEqual<bool>(expected, actual, "Wrong try set value return value");
    }

    [TestMethod]
    public void TrySetValue_False_NotADateTime()
    {
        // arrange
        InitializeWithAllTheArgs();

        var expected = false;
        string input = "asdf";

        // act
        var actual = SystemUnderTest.TrySetValue(input!);

        // assert
        Assert.AreEqual<bool>(expected, actual, "Wrong try set value return value");
    }

    [TestMethod]
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
        Assert.AreEqual<bool>(expected, actual, "Wrong try set value return value");
        Assert.AreEqual<DateTime>(expectedValue, SystemUnderTest.Value, "Value was wrong");
    }

    [DataTestMethod]
    [DataRow("12/1/2022", true, "12/1/2022 12:00:00 AM", DisplayName = "month day year")]
    [DataRow("12/31/2022 18:21:14", true, "12/31/2022 6:21:14 PM", DisplayName = "month day year hour min sec in 24h")]
    [DataRow("12/1/2022 6:21:14 PM", true, "12/1/2022 6:21:14 PM", DisplayName = "month day year hour min sec in PM")]
    [DataRow("12/1/2022 6:21:14 AM", true, "12/1/2022 6:21:14 AM", DisplayName = "month day year hour min sec in AM")]
    [DataRow("20240816T1750349136Z", true, "8/16/2024 5:50:34 PM", DisplayName = "Get-Date -Format FileDateTimeUniversal")]
    [DataRow("2024-08-16T17:29:39Z", true, "8/16/2024 1:29:39 PM", DisplayName = "universal")]
    [DataRow("asdf", false, "", DisplayName = "junk")]
    [DataRow(null, false, "", DisplayName = "null")]
    [DataRow("", false, "", DisplayName = "empty string")]
    public void TrySetValueAndVerifyValue(string input, bool expectedOutcome, string expectedDateString)
    {
        // arrange
        var arg = new DateTimeArgument(EXPECTED_ARG_NAME)
            .WithDescription(EXPECTED_ARG_DESC)
            .AsRequired();

        var temp = arg as DateTimeArgument ?? throw new InvalidOperationException("Wrong type");

        _SystemUnderTest = temp;

        // act
        var actual = SystemUnderTest.TrySetValue(input);

        // assert
        Assert.AreEqual<bool>(expectedOutcome, actual, "Wrong try set value return value");

        if (expectedOutcome == true)
        {
            var actualDateString = SystemUnderTest.Value.ToString("M/d/yyyy h:mm:ss tt");

            Assert.AreEqual<string>(expectedDateString, actualDateString, "Value was wrong");
        }
    }

    [TestMethod]
    public void GetTimeZone()
    {
        var localZone = TimeZoneInfo.Local;

        Console.WriteLine(localZone);
    }


}
