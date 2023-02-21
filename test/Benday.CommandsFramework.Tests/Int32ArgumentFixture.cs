namespace Benday.CommandsFramework.Tests;

[TestClass]
public class Int32ArgumentFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _SystemUnderTest = null;
    }

    private Int32Argument? _SystemUnderTest;
    private const string EXPECTED_ARG_NAME = "arg123";
    private const int EXPECTED_ARG_VALUE = 123;
    private const string EXPECTED_ARG_DESC = "argvalue123 description";
    private const bool EXPECTED_ARG_ISREQUIRED = true;
    private const bool EXPECTED_ARG_ALLOWEMPTYVALUE = true;
    private const ArgumentDataType EXPECTED_ARG_DATATYPE = ArgumentDataType.Int32;


    private Int32Argument SystemUnderTest
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
        _SystemUnderTest = new Int32Argument(
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
        Assert.AreEqual<int>(EXPECTED_ARG_VALUE, SystemUnderTest.Value, "Value was wrong");
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
        SystemUnderTest.Value = 2345;

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
    public void TrySetValue_False_NotANumber()
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
    public void TrySetValue_True_ValidInteger()
    {
        // arrange
        InitializeWithAllTheArgs();
        var expected = true;
        string input = "4321";
        var expectedValue = 4321;

        // act
        var actual = SystemUnderTest.TrySetValue(input);

        // assert
        Assert.AreEqual<bool>(expected, actual, "Wrong try set value return value");
        Assert.AreEqual<int>(expectedValue, SystemUnderTest.Value, "Value was wrong");
    }
}
