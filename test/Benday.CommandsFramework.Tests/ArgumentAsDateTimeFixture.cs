namespace Benday.CommandsFramework.Tests;

[TestClass]
public class ArgumentAsDateTimeFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _SystemUnderTest = null;
    }

    private Argument<DateTime>? _SystemUnderTest;
    private const string EXPECTED_ARG_NAME = "arg123";
#pragma warning disable IDE1006 // Naming Styles
    private readonly DateTime EXPECTED_ARG_VALUE = DateTime.MaxValue;
#pragma warning restore IDE1006 // Naming Styles
    private const string EXPECTED_ARG_DESC = "argvalue123 description";
    private const bool EXPECTED_ARG_ISREQUIRED = true;
    private const bool EXPECTED_ARG_ALLOWEMPTYVALUE = true;
    private const ArgumentDataType EXPECTED_ARG_DATATYPE = ArgumentDataType.DateTime;


    private Argument<DateTime> SystemUnderTest
    {
        get
        {
            if (_SystemUnderTest == null)
            {
                InitializeWithNoArgs();
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
        _SystemUnderTest = new Argument<DateTime>(
            EXPECTED_ARG_NAME,
            EXPECTED_ARG_VALUE,
            EXPECTED_ARG_DESC,
            EXPECTED_ARG_ISREQUIRED,
            EXPECTED_ARG_ALLOWEMPTYVALUE);
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
}