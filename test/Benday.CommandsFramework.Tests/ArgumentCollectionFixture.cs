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
        var expectedArgs = new Dictionary<string, IArgument>();

        expectedArgs.Add("arg1", new StringArgument("arg1", true, "argument 1", true, false));
        expectedArgs.Add("isawesome", new BooleanArgument("isawesome", true, true));
        expectedArgs.Add("count", new Int32Argument("count", true, true));
        expectedArgs.Add("dateofthingy", new DateTimeArgument("dateofthingy", true, true));
        expectedArgs.Add("verbose", new BooleanArgument("verbose", false, true));

        _SystemUnderTest = new(expectedArgs);
        
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

    private void AssertArgumentValue(string expectedKey, string expectedValue)
    {
        Assert.IsTrue(SystemUnderTest.ContainsKey(expectedKey), $"Key named '{expectedKey}' not found");

        var actual = SystemUnderTest[expectedKey];

        var actualAsTyped = actual as StringArgument;

        Assert.IsNotNull(actualAsTyped, $"Could not convert '{expectedKey}' argument to StringArgument");

        Assert.IsTrue(actualAsTyped.Validate(), "Should be valid");

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
