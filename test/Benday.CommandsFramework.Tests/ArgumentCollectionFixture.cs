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
}