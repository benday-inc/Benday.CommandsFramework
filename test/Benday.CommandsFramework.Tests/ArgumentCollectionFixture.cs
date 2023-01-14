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
}