using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Benday.CommandsFramework.Tests;

[TestClass]
public class ArgumentCollectionFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _SystemUnderTest = null;
    }

    private ArgumentCollection? _SystemUnderTest;

    private ArgumentCollection SystemUnderTest
    {
        get
        {
            if (_SystemUnderTest == null)
            {
                _SystemUnderTest = new ArgumentCollection();
            }

            return _SystemUnderTest;
        }
    }

    [TestMethod]
    public void StartsAsEmpty()
    {
        // arrange
        var expected = 0;

        // act
        var actual = SystemUnderTest.Count;

        // assert
        Assert.AreEqual<int>(expected, actual, "Item count was wrong.");
    }

    [TestMethod]
    public void Add()
    {
        // arrange
        var expected = 1;
        var key = "key123";
        var value = "value123";

        // act
        SystemUnderTest.Add(key, value);

        // assert
        var actual = SystemUnderTest.Count; 
        Assert.AreEqual<int>(expected, actual, "Item count was wrong.");
    }

    [TestMethod]
    public void Contains_ForItemThatExists_ReturnsTrue()
    {
        // arrange
        var expected = true;
        var key = "key123";
        var value = "value123";
        SystemUnderTest.Add(key, value);

        // act
        var actual = SystemUnderTest.ContainsKey(key);

        // assert
        Assert.AreEqual<bool>(expected, actual, "Contains return value was wrong.");
    }

    [TestMethod]
    public void Contains_ForItemThatDoesNotExist_ReturnsFalse()
    {
        // arrange
        var expected = false;
        var key = "key123";

        // act
        var actual = SystemUnderTest.ContainsKey(key);

        // assert
        Assert.AreEqual<bool>(expected, actual, "Contains return value was wrong.");
    }

    [TestMethod]
    public void Remove_ForItemThatExists()
    {
        // arrange
        var expected = false;
        var key = "key123";
        var value = "value123";
        SystemUnderTest.Add(key, value);

        // act
        SystemUnderTest.Remove(key);

        // assert
        var actual = SystemUnderTest.ContainsKey(key);
        Assert.AreEqual<bool>(expected, actual, "Contains return value was wrong.");
    }

    [TestMethod]
    public void Remove_ForItemThatDoesNotExist()
    {
        // arrange
        var expected = false;
        var key = "key123";

        // act
        SystemUnderTest.Remove(key);

        // assert
        var actual = SystemUnderTest.ContainsKey(key);
        Assert.AreEqual<bool>(expected, actual, "Contains return value was wrong.");
    }

    [TestMethod]
    public void Ctor_UsingExistingDictionary_CopiesItems()
    {
        // arrange
        var key1 = "key123";
        var key2 = "key234";
        var key3 = "key345";

        var value1 = "val1";
        var value2 = "val2";
        var value3 = "val3";

        var fromDictionary = new Dictionary<string, string>();

        fromDictionary.Add(key1, value1);
        fromDictionary.Add(key2, value2);
        fromDictionary.Add(key3, value3);

        var expectedCount = fromDictionary.Count;

        // act
        _SystemUnderTest = new ArgumentCollection(fromDictionary);

        // assert
        Assert.AreEqual<int>(expectedCount, SystemUnderTest.Count, "Wrong count");

        // wipe out the source dictionary
        fromDictionary.Clear();

        Assert.AreEqual<int>(expectedCount, SystemUnderTest.Count, "Wrong count...values aren't being copies and there's a byref problem");

        Assert.IsTrue(SystemUnderTest.ContainsKey(key1), "Key1 should exist");
        Assert.IsTrue(SystemUnderTest.ContainsKey(key2), "Key2 should exist");
        Assert.IsTrue(SystemUnderTest.ContainsKey(key3), "Key3 should exist");
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Ctor_UsingExistingDictionary_ThrowsExceptionOnNullValueInDictionary()
    {
        // arrange
        var key1 = "key123";
        var key2 = "key234";
        var key3 = "key345";

        var value1 = "val1";
        string? value2 = null;
        var value3 = "val3";

        var fromDictionary = new Dictionary<string, string>();

        fromDictionary.Add(key1, value1);
        fromDictionary.Add(key2, value2!);
        fromDictionary.Add(key3, value3);

        var expectedCount = fromDictionary.Count;

        // act
        _SystemUnderTest = new ArgumentCollection(fromDictionary);

        // assert
       
    }

    [TestMethod]
    public void SetValues()
    {
        // arrange
        var expectedArgs = new ArgumentCollection();

        expectedArgs.AddString("arg1").AsRequired().WithDescription("argument 1").AsRequired();
        expectedArgs.AddBoolean("isawesome").WithDescription("is awesome?").AsRequired().AllowEmptyValue();
        expectedArgs.AddInt32("count").WithDescription("count of things").AsRequired().AllowEmptyValue();
        expectedArgs.AddDateTime("dateofthingy").WithDescription("date of thingy").AsRequired();
        expectedArgs.AddBoolean("verbose").AsNotRequired().AllowEmptyValue();

        _SystemUnderTest = expectedArgs;
        
        var commandLineArgs = Utilities.GetStringArray(
            "commandname1",
            "/arg1:Hello",
            "/isawesome:true",
            "/count:4321",
            "/dateofthingy:12/25/2022",
            "/verbose"
            );

        var valueArgs = new ArgumentCollectionFactory().Parse(commandLineArgs);

        // act
        SystemUnderTest.SetValues(valueArgs.Arguments);

        // assert
        AssertArgumentValue("arg1", "Hello");
        AssertArgumentValue("isawesome", true);
        AssertArgumentValue("count", 4321);
        AssertArgumentValue("dateofthingy", new DateTime(2022, 12, 25));
        AssertArgumentValue("verbose", true);
    }

    [TestMethod]
    public void SetValues_PositionalArgs()
    {
        // arrange
        var expectedArgs = new ArgumentCollection();
        
        expectedArgs.AddString("arg1").WithDescription("argument 1").AsRequired().FromPositionalArgument(1);

        _SystemUnderTest = expectedArgs;

        var commandLineArgs = Utilities.GetStringArray(
            "commandname1",
            "/isawesome:true",
            "/count:4321",
            "/dateofthingy:12/25/2022",
            "/verbose",
            "this-is-arg1"
            );

        var valueArgs = new ArgumentCollectionFactory().Parse(commandLineArgs);

        // act
        SystemUnderTest.SetValues(valueArgs.Arguments);

        // assert
        AssertArgumentValue("arg1", "this-is-arg1");
    }

    [TestMethod]
    public void ExtensionMethods_GetBooleanValue_ArgDoesNotAllowEmptyValue_True()
    {
        // arrange
        var expectedArgs = new ArgumentCollection();

        expectedArgs.AddBoolean("isawesome").WithDescription("is awesome?").AsRequired().AllowEmptyValue();

        _SystemUnderTest = expectedArgs;

        var expected = true;

        var commandLineArgs = Utilities.GetStringArray(
            "commandname1",
            $"/isawesome:{expected}"
            );

        var valueArgs = new ArgumentCollectionFactory().Parse(commandLineArgs);
        SystemUnderTest.SetValues(valueArgs.Arguments);

        // act
        var actual = SystemUnderTest.GetBooleanValue("isawesome");

        // assert
        Assert.AreEqual<bool>(expected, actual, "Wrong value");
    }

    [TestMethod]
    public void ExtensionMethods_GetBooleanValue_ArgDoesNotAllowEmptyValue_False()
    {
        // arrange
        var expectedArgs = new ArgumentCollection();

        expectedArgs.AddBoolean("isawesome").WithDescription("is awesome?").AsRequired().AllowEmptyValue();

        _SystemUnderTest = expectedArgs;

        var expected = false;

        var commandLineArgs = Utilities.GetStringArray(
            "commandname1",
            $"/isawesome:{expected}"
            );

        var valueArgs = new ArgumentCollectionFactory().Parse(commandLineArgs);
        SystemUnderTest.SetValues(valueArgs.Arguments);

        // act
        var actual = SystemUnderTest.GetBooleanValue("isawesome");

        // assert
        Assert.AreEqual<bool>(expected, actual, "Wrong value");
    }

    [TestMethod]
    public void ExtensionMethods_GetBooleanValue_ArgAllowsEmptyValue_True()
    {
        // arrange
        var expectedArgs = new ArgumentCollection();

        expectedArgs.AddBoolean("verbose").AsNotRequired().AllowEmptyValue();

        _SystemUnderTest = expectedArgs;

        var expected = true;

        var commandLineArgs = Utilities.GetStringArray(
            "commandname1",
            "/verbose"
            );

        var valueArgs = new ArgumentCollectionFactory().Parse(commandLineArgs);

        SystemUnderTest.SetValues(valueArgs.Arguments);

        // act
        var actual = SystemUnderTest.GetBooleanValue("verbose");

        // assert
        Assert.AreEqual<bool>(expected, actual, "Wrong value");
    }

    [TestMethod]
    public void ExtensionMethods_GetBooleanValue_ArgAllowsEmptyValue_False()
    {
        // arrange
        var expectedArgs = new ArgumentCollection();

        expectedArgs.AddBoolean("verbose").AsNotRequired().AllowEmptyValue();

        _SystemUnderTest = expectedArgs;

        var expected = false;

        // Note: /verbose is not in this list of args
        var commandLineArgs = Utilities.GetStringArray(
            "commandname1");

        var valueArgs = new ArgumentCollectionFactory().Parse(commandLineArgs);

        SystemUnderTest.SetValues(valueArgs.Arguments);

        // act
        var actual = SystemUnderTest.GetBooleanValue("verbose");

        // assert
        Assert.AreEqual<bool>(expected, actual, "Wrong value");
    }

    private void AssertArgumentValue(string expectedKey, string expectedValue)
    {
        Assert.IsTrue(SystemUnderTest.ContainsKey(expectedKey), $"Key named '{expectedKey}' not found");

        var actual = SystemUnderTest[expectedKey];

        var actualAsTyped = actual as StringArgument;

        Assert.IsNotNull(actualAsTyped, $"Could not convert '{expectedKey}' argument to StringArgument");

        Assert.IsTrue(actualAsTyped.Validate(), $"Arg named '{expectedKey}' should be valid");

        Assert.AreEqual<string>(expectedValue, actualAsTyped.Value, $"Value for key named '{expectedKey}' was wrong");
    }

    private void AssertArgumentValue(string expectedKey, int expectedValue)
    {
        Assert.IsTrue(SystemUnderTest.ContainsKey(expectedKey), $"Key named '{expectedKey}' not found");

        var actual = SystemUnderTest[expectedKey];

        var actualAsTyped = actual as Int32Argument;

        Assert.IsNotNull(actualAsTyped, $"Could not convert '{expectedKey}' argument to StringArgument");

        Assert.IsTrue(actualAsTyped.Validate(), "Should be valid");

        Assert.AreEqual<int>(expectedValue, actualAsTyped.Value, $"Value for key named '{expectedKey}' was wrong");
    }

    private void AssertArgumentValue(string expectedKey, bool expectedValue)
    {
        Assert.IsTrue(SystemUnderTest.ContainsKey(expectedKey), $"Key named '{expectedKey}' not found");

        var actual = SystemUnderTest[expectedKey];

        var actualAsTyped = actual as BooleanArgument;

        Assert.IsNotNull(actualAsTyped, $"Could not convert '{expectedKey}' argument to StringArgument");

        Assert.IsTrue(actualAsTyped.Validate(), "Should be valid");

        Assert.AreEqual<bool>(expectedValue, actualAsTyped.Value, $"Value for key named '{expectedKey}' was wrong");
    }

    private void AssertArgumentValue(string expectedKey, DateTime expectedValue)
    {
        Assert.IsTrue(SystemUnderTest.ContainsKey(expectedKey), $"Key named '{expectedKey}' not found");

        var actual = SystemUnderTest[expectedKey];

        var actualAsTyped = actual as DateTimeArgument;

        Assert.IsNotNull(actualAsTyped, $"Could not convert '{expectedKey}' argument to StringArgument");

        Assert.IsTrue(actualAsTyped.Validate(), "Should be valid");

        Assert.AreEqual<DateTime>(expectedValue, actualAsTyped.Value, $"Value for key named '{expectedKey}' was wrong");
    }

    
}
