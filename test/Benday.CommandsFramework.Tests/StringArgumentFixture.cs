namespace Benday.CommandsFramework.Tests;

public class StringArgumentFixture
{
        public StringArgumentFixture()
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
        var arg = new ArgumentCollection().AddString(EXPECTED_ARG_NAME)
           .AsRequired()
           .AllowEmptyValue()
           .WithDescription(EXPECTED_ARG_DESC);

        arg.Value = EXPECTED_ARG_VALUE;

        var temp = arg as StringArgument ?? throw new InvalidOperationException("Wrong type");

        _SystemUnderTest = temp;
    }

    private void InitializeWithAllTheArgsExceptValue()
    {
        //_SystemUnderTest = new StringArgument(
        //    EXPECTED_ARG_NAME,
        //    noValue: true,
        //    EXPECTED_ARG_DESC,
        //    EXPECTED_ARG_ISREQUIRED,
        //    EXPECTED_ARG_ALLOWEMPTYVALUE);


        var arg = new ArgumentCollection().AddString(EXPECTED_ARG_NAME)
            .AsRequired()
            .AllowEmptyValue()
            .WithDescription(EXPECTED_ARG_DESC);

        var temp = arg as StringArgument ?? throw new InvalidOperationException("Wrong type");

        _SystemUnderTest = temp;
    }

    [Fact]
    public void Ctor_WithAllValues()
    {
        // arrange

        // act
        InitializeWithAllTheArgsExceptAlias();

        // assert
        Assert.Equal(EXPECTED_ARG_DESC, SystemUnderTest.Description);
        Assert.Equal(EXPECTED_ARG_VALUE, SystemUnderTest.Value);
        Assert.Equal(EXPECTED_ARG_NAME, SystemUnderTest.Name);
        Assert.Equal(EXPECTED_ARG_ISREQUIRED, SystemUnderTest.IsRequired);
        Assert.Equal(EXPECTED_ARG_DATATYPE, SystemUnderTest.DataType);
        Assert.Equal(EXPECTED_ARG_ALLOWEMPTYVALUE, SystemUnderTest.AllowEmptyValue);
    }

    [Fact]
    public void Ctor_WithJustName()
    {
        // arrange

        // act
        _SystemUnderTest = new StringArgument(EXPECTED_ARG_NAME);

        // assert
        Assert.Equal(EXPECTED_ARG_NAME, SystemUnderTest.Description);
        Assert.Equal(string.Empty, SystemUnderTest.Value);
        Assert.Equal(EXPECTED_ARG_NAME, SystemUnderTest.Name);
        Assert.Equal(EXPECTED_ARG_ISREQUIRED, SystemUnderTest.IsRequired);
        Assert.Equal(EXPECTED_ARG_DATATYPE, SystemUnderTest.DataType);
        Assert.Equal(false, SystemUnderTest.AllowEmptyValue);
        Assert.False(SystemUnderTest.HasValue);
    }

    public void Ctor_WithNameAndValue()
    {
        // arrange

        // act
        var arg = new ArgumentCollection().AddString(EXPECTED_ARG_NAME);

        arg.Value = EXPECTED_ARG_VALUE;

        _SystemUnderTest = arg;

        // assert
        Assert.Equal(EXPECTED_ARG_NAME, SystemUnderTest.Description);
        Assert.Equal(EXPECTED_ARG_VALUE, SystemUnderTest.Value);
        Assert.Equal(EXPECTED_ARG_NAME, SystemUnderTest.Name);
        Assert.Equal(EXPECTED_ARG_ISREQUIRED, SystemUnderTest.IsRequired);
        Assert.Equal(EXPECTED_ARG_DATATYPE, SystemUnderTest.DataType);
        Assert.Equal(EXPECTED_ARG_ALLOWEMPTYVALUE, SystemUnderTest.AllowEmptyValue);
        Assert.True(SystemUnderTest.HasValue);
    }


    [Fact]
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
        Assert.Equal(expected, actual);        
    }

    [Fact]
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
        Assert.Equal(expected, actual);
    }

    [Fact]
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
        Assert.Equal(expected, actual);
    }

    [Fact]    
    public void HasAlias_False_WhenAliasIsNotSet()
    {
        // arrange
        InitializeWithAllTheArgsExceptAlias();
        var expected = false;
        
        // act
        var actual = SystemUnderTest.HasAlias;

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void HasAlias_False_WhenAliasIsWhitespaceString()
    {
        // arrange
        InitializeWithAllTheArgsExceptAlias();

        SystemUnderTest.Alias = "      ";

        var expected = false;

        // act
        var actual = SystemUnderTest.HasAlias;

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void HasAlias_False_WhenAliasIsSetToNamePropertyValue()
    {
        // arrange
        InitializeWithAllTheArgsExceptAlias();

        SystemUnderTest.Alias = SystemUnderTest.Name;

        var expected = false;

        // act
        var actual = SystemUnderTest.HasAlias;

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void HasAlias_True_WhenSetToValue()
    {
        // arrange
        InitializeWithAllTheArgsExceptAlias();

        SystemUnderTest.Alias = "ALIAS123";

        var expected = true;

        // act
        var actual = SystemUnderTest.HasAlias;

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SetAliasViaFluentMethod_StringAlias()
    {
        // arrange
        InitializeWithAllTheArgsExceptAlias();
        var expectedAlias = "ALIAS123";

        var expectedHasAlias = true;

        // act
        SystemUnderTest.WithAlias(expectedAlias);

        // assert
        var actualHasAlias = SystemUnderTest.HasAlias;
        Assert.Equal(expectedHasAlias, actualHasAlias);
        Assert.Equal(expectedAlias, SystemUnderTest.Alias);
    }

    [Fact]
    public void SetPositionalSourceViaFluentMethod()
    {
        // arrange
        InitializeWithAllTheArgsExceptAlias();
        var expectedAlias = "POSITION_1";

        var expectedHasAlias = true;
        var expectedIsPositionalSource = true;

        // act
        SystemUnderTest.FromPositionalArgument(1);

        // assert
        var actualHasAlias = SystemUnderTest.HasAlias;
        
        Assert.Equal(expectedHasAlias, actualHasAlias);
        Assert.Equal(expectedIsPositionalSource, SystemUnderTest.IsPositionalSource);
        Assert.Equal(expectedAlias, SystemUnderTest.Alias);
    }

    [Fact]
    public void SetPositionalSourceViaFluentMethod_ThrowsExceptionForLessThan1()
    {
        // arrange
        InitializeWithAllTheArgsExceptAlias();
        
        // act & assert
        Assert.Throws<ArgumentOutOfRangeException>(() => {
            SystemUnderTest.FromPositionalArgument(0);
        });
    }

    [Fact]
    public void SetPositionalSourceViaFluentMethod_ThrowsExceptionForLessThan0()
    {
        // arrange
        InitializeWithAllTheArgsExceptAlias();

        // act & assert
        Assert.Throws<ArgumentOutOfRangeException>(() => {
            SystemUnderTest.FromPositionalArgument(-1);
        });
    }

    [Fact]
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
        Assert.Equal(expectedHasValue, SystemUnderTest.HasValue);
        Assert.Equal(expected, actual);
    }

    [Fact]
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
        Assert.Equal(expectedHasValue, SystemUnderTest.HasValue);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TrySetValue_False()
    {
        // arrange
        InitializeWithAllTheArgsExceptValue();
        var expected = false;
        string? input = null;

        // act
        var actual = SystemUnderTest.TrySetValue(input!);

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TrySetValue_True_EmptyString()
    {
        // arrange
        InitializeWithAllTheArgsExceptValue();
        var expected = true;
        string input = string.Empty;

        // act
        var actual = SystemUnderTest.TrySetValue(input);

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TrySetValue_True_NonEmptyString()
    {
        // arrange
        InitializeWithAllTheArgsExceptValue();
        var expected = true;
        string input = "yada yada yada";

        // act
        var actual = SystemUnderTest.TrySetValue(input);

        // assert
        Assert.Equal(expected, actual);
    }
}
