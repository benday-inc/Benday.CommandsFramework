namespace Benday.CommandsFramework.Tests;

[TestClass]
public class StringArgumentFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _SystemUnderTest = null;
    }

    private StringArgument? _SystemUnderTest;
    private const string EXPECTED_ARG_NAME = "arg123";
    private const string EXPECTED_ARG_VALUE = "argvalue123";
    private const string EXPECTED_ARG_DESC = "argvalue123 description";
    private const bool EXPECTED_ARG_ISREQUIRED = true;
    private const bool EXPECTED_ARG_ALLOWEMPTYVALUE = true;
    private const ArgumentDataType EXPECTED_ARG_DATATYPE = ArgumentDataType.String;


    private StringArgument SystemUnderTest
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

    private void InitializeWithAllTheArgsExceptAlias()
    {
        _SystemUnderTest = new StringArgument(
            EXPECTED_ARG_NAME,
            EXPECTED_ARG_VALUE,
            EXPECTED_ARG_DESC,
            EXPECTED_ARG_ISREQUIRED,
            EXPECTED_ARG_ALLOWEMPTYVALUE);
    }

    private void InitializeWithAllTheArgsExceptValue()
    {
        _SystemUnderTest = new StringArgument(
            EXPECTED_ARG_NAME,
            noValue: true,
            EXPECTED_ARG_DESC,
            EXPECTED_ARG_ISREQUIRED,
            EXPECTED_ARG_ALLOWEMPTYVALUE);
    }

    [TestMethod]
    public void Ctor_WithAllValues()
    {
        // arrange

        // act
        InitializeWithAllTheArgsExceptAlias();

        // assert
        Assert.AreEqual<string>(EXPECTED_ARG_DESC, SystemUnderTest.Description, "Description was wrong");
        Assert.AreEqual<string>(EXPECTED_ARG_VALUE, SystemUnderTest.Value, "Value was wrong");
        Assert.AreEqual<string>(EXPECTED_ARG_NAME, SystemUnderTest.Name, "Name was wrong");
        Assert.AreEqual<bool>(EXPECTED_ARG_ISREQUIRED, SystemUnderTest.IsRequired, "IsRequired was wrong");
        Assert.AreEqual<ArgumentDataType>(EXPECTED_ARG_DATATYPE, SystemUnderTest.DataType, "DataType was wrong");
        Assert.AreEqual<bool>(EXPECTED_ARG_ALLOWEMPTYVALUE, SystemUnderTest.AllowEmptyValue, "AllowEmptyValue was wrong");
    }

    [TestMethod]
    public void Ctor_WithJustName()
    {
        // arrange

        // act
        _SystemUnderTest = new StringArgument(EXPECTED_ARG_NAME);

        // assert
        Assert.AreEqual<string>(EXPECTED_ARG_NAME, SystemUnderTest.Description, "Description was wrong");
        Assert.AreEqual<string>(string.Empty, SystemUnderTest.Value, "Value was wrong");
        Assert.AreEqual<string>(EXPECTED_ARG_NAME, SystemUnderTest.Name, "Name was wrong");
        Assert.AreEqual<bool>(EXPECTED_ARG_ISREQUIRED, SystemUnderTest.IsRequired, "IsRequired was wrong");
        Assert.AreEqual<ArgumentDataType>(EXPECTED_ARG_DATATYPE, SystemUnderTest.DataType, "DataType was wrong");
        Assert.AreEqual<bool>(EXPECTED_ARG_ALLOWEMPTYVALUE, SystemUnderTest.AllowEmptyValue, "AllowEmptyValue was wrong");
        Assert.IsFalse(SystemUnderTest.HasValue, "HasValue was wrong");
    }

    public void Ctor_WithNameAndValue()
    {
        // arrange

        // act
        _SystemUnderTest = new StringArgument(EXPECTED_ARG_NAME, EXPECTED_ARG_VALUE);

        // assert
        Assert.AreEqual<string>(EXPECTED_ARG_NAME, SystemUnderTest.Description, "Description was wrong");
        Assert.AreEqual<string>(EXPECTED_ARG_VALUE, SystemUnderTest.Value, "Value was wrong");
        Assert.AreEqual<string>(EXPECTED_ARG_NAME, SystemUnderTest.Name, "Name was wrong");
        Assert.AreEqual<bool>(EXPECTED_ARG_ISREQUIRED, SystemUnderTest.IsRequired, "IsRequired was wrong");
        Assert.AreEqual<ArgumentDataType>(EXPECTED_ARG_DATATYPE, SystemUnderTest.DataType, "DataType was wrong");
        Assert.AreEqual<bool>(EXPECTED_ARG_ALLOWEMPTYVALUE, SystemUnderTest.AllowEmptyValue, "AllowEmptyValue was wrong");
        Assert.IsTrue(SystemUnderTest.HasValue, "HasValue was wrong");
    }


    [TestMethod]
    public void IsValid_Required_ValidValue_True()
    {
        // arrange
        InitializeWithAllTheArgsExceptAlias();
        SystemUnderTest.AllowEmptyValue = false;
        var expected = true;
        SystemUnderTest.Value = "valid value";

        // act
        var actual = SystemUnderTest.Validate();

        // assert
        Assert.AreEqual<bool>(expected, actual, "Validation value is wrong");        
    }

    [TestMethod]
    public void IsValid_Required_AllowEmptyValueFalse_EmptyString_False()
    {
        // arrange
        InitializeWithAllTheArgsExceptAlias();
        SystemUnderTest.AllowEmptyValue = false;
        var expected = false;
        SystemUnderTest.Value = string.Empty;

        // act
        var actual = SystemUnderTest.Validate();

        // assert
        Assert.AreEqual<bool>(expected, actual, "Validation value is wrong");
    }

    [TestMethod]
    public void IsValid_Required_AllowEmptyValueTrue_EmptyString_True()
    {
        // arrange
        InitializeWithAllTheArgsExceptAlias();
        SystemUnderTest.AllowEmptyValue = true;
        var expected = true;
        SystemUnderTest.Value = string.Empty;

        // act
        var actual = SystemUnderTest.Validate();

        // assert
        Assert.AreEqual<bool>(expected, actual, "Validation value is wrong");
    }

    [TestMethod]    
    public void HasAlias_False_WhenAliasIsNotSet()
    {
        // arrange
        InitializeWithAllTheArgsExceptAlias();
        var expected = false;
        
        // act
        var actual = SystemUnderTest.HasAlias;

        // assert
        Assert.AreEqual<bool>(expected, actual, "HasAlias value is wrong");
    }

    [TestMethod]
    public void HasAlias_False_WhenAliasIsWhitespaceString()
    {
        // arrange
        InitializeWithAllTheArgsExceptAlias();

        SystemUnderTest.Alias = "      ";

        var expected = false;

        // act
        var actual = SystemUnderTest.HasAlias;

        // assert
        Assert.AreEqual<bool>(expected, actual, "HasAlias value is wrong");
    }

    [TestMethod]
    public void HasAlias_False_WhenAliasIsSetToNamePropertyValue()
    {
        // arrange
        InitializeWithAllTheArgsExceptAlias();

        SystemUnderTest.Alias = SystemUnderTest.Name;

        var expected = false;

        // act
        var actual = SystemUnderTest.HasAlias;

        // assert
        Assert.AreEqual<bool>(expected, actual, "HasAlias value is wrong");
    }

    [TestMethod]
    public void HasAlias_True_WhenSetToValue()
    {
        // arrange
        InitializeWithAllTheArgsExceptAlias();

        SystemUnderTest.Alias = "ALIAS123";

        var expected = true;

        // act
        var actual = SystemUnderTest.HasAlias;

        // assert
        Assert.AreEqual<bool>(expected, actual, "HasAlias value is wrong");
    }

    [TestMethod]
    public void IsValid_NoValueSet_Required_AllowEmptyValueFalse_False()
    {
        // arrange
        InitializeWithAllTheArgsExceptValue();
        SystemUnderTest.AllowEmptyValue = false;
        var expected = false;
        var expectedHasValue = false;

        // act
        var actual = SystemUnderTest.Validate();

        // assert
        Assert.AreEqual<bool>(expectedHasValue, SystemUnderTest.HasValue, "HasValue value is wrong");
        Assert.AreEqual<bool>(expected, actual, "Validation value is wrong");
    }

    [TestMethod]
    public void IsValid_NoValueSet_Required_AllowEmptyValueTrue_True()
    {
        // arrange
        InitializeWithAllTheArgsExceptValue();
        SystemUnderTest.AllowEmptyValue = true;
        var expected = true;
        var expectedHasValue = false;

        // act
        var actual = SystemUnderTest.Validate();

        // assert
        Assert.AreEqual<bool>(expectedHasValue, SystemUnderTest.HasValue, "HasValue value is wrong");
        Assert.AreEqual<bool>(expected, actual, "Validation value is wrong");
    }

    [TestMethod]
    public void TrySetValue_False()
    {
        // arrange
        InitializeWithAllTheArgsExceptValue();
        var expected = false;
        string input = null;

        // act
        var actual = SystemUnderTest.TrySetValue(input!);

        // assert
        Assert.AreEqual<bool>(expected, actual, "Wrong try set value return value");
    }

    [TestMethod]
    public void TrySetValue_True_EmptyString()
    {
        // arrange
        InitializeWithAllTheArgsExceptValue();
        var expected = true;
        string input = string.Empty;

        // act
        var actual = SystemUnderTest.TrySetValue(input);

        // assert
        Assert.AreEqual<bool>(expected, actual, "Wrong try set value return value");
    }

    [TestMethod]
    public void TrySetValue_True_NonEmptyString()
    {
        // arrange
        InitializeWithAllTheArgsExceptValue();
        var expected = true;
        string input = "yada yada yada";

        // act
        var actual = SystemUnderTest.TrySetValue(input);

        // assert
        Assert.AreEqual<bool>(expected, actual, "Wrong try set value return value");
    }
}
