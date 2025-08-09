namespace Benday.CommandsFramework.Tests;

public class BooleanArgumentFixture
{
        public BooleanArgumentFixture()
    {
        _SystemUnderTest = null;
    }

    private BooleanArgument? _SystemUnderTest;
    private const string EXPECTED_ARG_NAME = "arg123";
    private const bool EXPECTED_ARG_VALUE = true;
    private const string EXPECTED_ARG_DESC = "argvalue123 description";
    private const bool EXPECTED_ARG_ISREQUIRED = true;
    private const bool EXPECTED_ARG_ALLOWEMPTYVALUE = true;
    private const ArgumentDataType EXPECTED_ARG_DATATYPE = ArgumentDataType.Boolean;


    private BooleanArgument SystemUnderTest
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

    private void InitializeWithAllTheArgs_AllowEmptyValue()
    {
        //_SystemUnderTest = new BooleanArgument(
        //    EXPECTED_ARG_NAME,
        //    EXPECTED_ARG_VALUE,
        //    EXPECTED_ARG_DESC,
        //    EXPECTED_ARG_ISREQUIRED,
        //    EXPECTED_ARG_ALLOWEMPTYVALUE);

        var arg = new ArgumentCollection()
            .AddBoolean(EXPECTED_ARG_NAME)
            .AsRequired()
            .AllowEmptyValue()
            .WithDescription(EXPECTED_ARG_DESC);

        arg.Value = EXPECTED_ARG_VALUE;

        var temp = arg as BooleanArgument ?? throw new InvalidOperationException("Wrong type");

        _SystemUnderTest = temp;
    }

    private void InitializeNotRequiredAllowEmptyValue()
    {
        var arg = new BooleanArgument(EXPECTED_ARG_NAME).AsNotRequired().AllowEmptyValue();

        var temp = arg as BooleanArgument ?? throw new InvalidOperationException("Wrong type");

        _SystemUnderTest = temp;
    }

    [Fact]
    public void Ctor_WithAllValues()
    {
        // arrange

        // act
        InitializeWithAllTheArgs_AllowEmptyValue();

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
        InitializeWithAllTheArgs_AllowEmptyValue();
        SystemUnderTest.AllowEmptyValue = false;
        var expected = true;
        SystemUnderTest.Value = false;

        // act
        var actual = SystemUnderTest.Validate();

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TrySetValue_AllowsEmptyValue_EmptyValue()
    {
        // this test verifies that TrySetValue handles setting the arg value properly
        // when the arg allows an empty value.  This is basically the case for handling
        // args like "/verbose" that don't have a value.  The existence of the arg 
        // means that the value is true.

        // arrange
        InitializeNotRequiredAllowEmptyValue();

        var expected = true;
        string input = string.Empty;

        // act
        SystemUnderTest.TrySetValue(input);

        // assert
        var actual = SystemUnderTest.ValueAsBoolean;
        Assert.Equal(expected, actual);
        Assert.True(SystemUnderTest.HasValue);
    }

    [Fact]
    public void TrySetValue_AllowsEmptyValue_NoValueIsSet()
    {
        // this test verifies that TrySetValue handles setting the arg value properly
        // when the arg allows an empty value.  This is basically the case for handling
        // args like "/verbose" that don't have a value.  The existence of the arg 
        // means that the value is true.
        // This case checks that the values are correct even if TrySetValue doesn't get 
        // called.  For example, you *could* have an arg of "/verbose" but it wasn't set
        // on the command line

        // arrange
        InitializeNotRequiredAllowEmptyValue();

        Assert.True(SystemUnderTest.AllowEmptyValue);
        Assert.False(SystemUnderTest.IsRequired);
        Assert.False(SystemUnderTest.HasValue);

        var expected = false;
        string input = string.Empty;

        // act

        // for this case, purposefully do not call TrySetValue()
        // pretend the value isn't passed in the command line args
        // var actual = SystemUnderTest.TrySetValue(input);

        // assert
        var actual = SystemUnderTest.ValueAsBoolean;
        Assert.Equal(expected, actual);
        Assert.False(SystemUnderTest.HasValue);
    }

    [Fact]
    public void TrySetValue_AllowsEmptyValue_PassedAValueOfTrue()
    {
        // this test verifies that TrySetValue handles setting the arg value properly
        // when the arg allows an empty value.  This is basically the case for handling
        // args like "/verbose" that don't have a value.  The existence of the arg 
        // means that the value is true.
        // This case checks that setting a value on the arg such as
        // "/verbose:true" is handled correctly.

        // arrange
        InitializeNotRequiredAllowEmptyValue();

        var expected = true;
        string input = true.ToString();

        // act
        SystemUnderTest.TrySetValue(input);

        // assert
        var actual = SystemUnderTest.ValueAsBoolean;
        Assert.Equal(expected, actual);
        Assert.True(SystemUnderTest.HasValue);
    }

    [Fact]
    public void TrySetValue_AllowsEmptyValue_PassedAValueOfFalse()
    {
        // this test verifies that TrySetValue handles setting the arg value properly
        // when the arg allows an empty value.  This is basically the case for handling
        // args like "/verbose" that don't have a value.  The existence of the arg 
        // means that the value is true.
        // This case checks that setting a value on the arg such as
        // "/verbose:false" is handled correctly.

        // arrange
        InitializeNotRequiredAllowEmptyValue();

        var expected = false;
        string input = false.ToString();

        // act
        SystemUnderTest.TrySetValue(input);

        // assert
        var actual = SystemUnderTest.ValueAsBoolean;
        Assert.Equal(expected, actual);
        Assert.True(SystemUnderTest.HasValue);
    }

    [Fact]
    public void TrySetValue_False_NullString()
    {
        // arrange
        InitializeWithAllTheArgs_AllowEmptyValue();
        
        var expected = false;
        string? input = null;

        // act
        var actual = SystemUnderTest.TrySetValue(input!);

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TrySetValue_False_NotABool()
    {
        // arrange
        InitializeWithAllTheArgs_AllowEmptyValue();

        var expected = false;
        string input = "asdf";

        // act
        var actual = SystemUnderTest.TrySetValue(input!);

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TrySetValue_True_ValidBool()
    {
        // arrange
        InitializeWithAllTheArgs_AllowEmptyValue();
        var expected = true;
        string input = true.ToString();
        var expectedValue = true;

        // act
        var actual = SystemUnderTest.TrySetValue(input);

        // assert
        Assert.Equal(expected, actual);
        Assert.Equal(expectedValue, SystemUnderTest.Value);
    }    
}