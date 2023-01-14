namespace Benday.CommandsFramework.Tests;

[TestClass]
public class ArgumentAsStringFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _SystemUnderTest = null;
    }

    private Argument<string>? _SystemUnderTest;
    private const string EXPECTED_ARG_NAME = "arg123";
    private const string EXPECTED_ARG_VALUE = "argvalue123";
    private const string EXPECTED_ARG_DESC = "argvalue123 description";
    private const bool EXPECTED_ARG_ISREQUIRED = true;
    private const ArgumentDataType EXPECTED_ARG_DATATYPE = ArgumentDataType.String;


    private Argument<string> SystemUnderTest
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
        _SystemUnderTest = new Argument<string>(EXPECTED_ARG_NAME, EXPECTED_ARG_VALUE, EXPECTED_ARG_DESC, EXPECTED_ARG_ISREQUIRED);
    }

    [TestMethod]
    public void Ctor_WithAllValues()
    {
        // arrange

        // act
        InitializeWithAllTheArgs();

        // assert
        Assert.AreEqual<string>(EXPECTED_ARG_DESC, SystemUnderTest.Description, "Description was wrong");
        Assert.AreEqual<string>(EXPECTED_ARG_VALUE, SystemUnderTest.Value, "Value was wrong");
        Assert.AreEqual<string>(EXPECTED_ARG_NAME, SystemUnderTest.Name, "Name was wrong");
        Assert.AreEqual<bool>(EXPECTED_ARG_ISREQUIRED, SystemUnderTest.IsRequired, "IsRequired was wrong");
        Assert.AreEqual<string>(EXPECTED_ARG_DESC, SystemUnderTest.Description, "Description was wrong");
        Assert.AreEqual<ArgumentDataType>(EXPECTED_ARG_DATATYPE, SystemUnderTest.DataType, "DataType was wrong");
    }
}