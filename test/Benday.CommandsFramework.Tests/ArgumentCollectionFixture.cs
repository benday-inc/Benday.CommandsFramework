namespace Benday.CommandsFramework.Tests;

public class ArgumentCollectionFixture
{
        public ArgumentCollectionFixture()
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

    [Fact]
    public void StartsAsEmpty()
    {
        // arrange
        var expected = 0;

        // act
        var actual = SystemUnderTest.Count;

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
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
        Assert.Equal(expected, actual);
    }

    [Fact]
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
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Contains_ForItemThatDoesNotExist_ReturnsFalse()
    {
        // arrange
        var expected = false;
        var key = "key123";

        // act
        var actual = SystemUnderTest.ContainsKey(key);

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
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
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Remove_ForItemThatDoesNotExist()
    {
        // arrange
        var expected = false;
        var key = "key123";

        // act
        SystemUnderTest.Remove(key);

        // assert
        var actual = SystemUnderTest.ContainsKey(key);
        Assert.Equal(expected, actual);
    }

    [Fact]
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
        Assert.Equal(expectedCount, SystemUnderTest.Count);

        // wipe out the source dictionary
        fromDictionary.Clear();

        Assert.Equal(expectedCount, SystemUnderTest.Count);

        Assert.True(SystemUnderTest.ContainsKey(key1));
        Assert.True(SystemUnderTest.ContainsKey(key2));
        Assert.True(SystemUnderTest.ContainsKey(key3));
    }

    [Fact]
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

        // act & assert
        Assert.Throws<InvalidOperationException>(() => {
            _SystemUnderTest = new ArgumentCollection(fromDictionary);
        });
    }

    [Fact]
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

    [Fact]
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

    [Fact]
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
        Assert.Equal(expected, actual);
    }

    [Fact]
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
        Assert.Equal(expected, actual);
    }

    [Fact]
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
        Assert.Equal(expected, actual);
    }

    [Fact]
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
        Assert.Equal(expected, actual);
    }

    private void AssertArgumentValue(string expectedKey, string expectedValue)
    {
        Assert.True(SystemUnderTest.ContainsKey(expectedKey));

        var actual = SystemUnderTest[expectedKey];

        var actualAsTyped = actual as StringArgument;

        Assert.NotNull(actualAsTyped);

        Assert.True(actualAsTyped.Validate());

        Assert.Equal(expectedValue, actualAsTyped.Value);
    }

    private void AssertArgumentValue(string expectedKey, int expectedValue)
    {
        Assert.True(SystemUnderTest.ContainsKey(expectedKey));

        var actual = SystemUnderTest[expectedKey];

        var actualAsTyped = actual as Int32Argument;

        Assert.NotNull(actualAsTyped);

        Assert.True(actualAsTyped.Validate());

        Assert.Equal(expectedValue, actualAsTyped.Value);
    }

    private void AssertArgumentValue(string expectedKey, bool expectedValue)
    {
        Assert.True(SystemUnderTest.ContainsKey(expectedKey));

        var actual = SystemUnderTest[expectedKey];

        var actualAsTyped = actual as BooleanArgument;

        Assert.NotNull(actualAsTyped);

        Assert.True(actualAsTyped.Validate());

        Assert.Equal(expectedValue, actualAsTyped.Value);
    }

    private void AssertArgumentValue(string expectedKey, DateTime expectedValue)
    {
        Assert.True(SystemUnderTest.ContainsKey(expectedKey));

        var actual = SystemUnderTest[expectedKey];

        var actualAsTyped = actual as DateTimeArgument;

        Assert.NotNull(actualAsTyped);

        Assert.True(actualAsTyped.Validate());

        Assert.Equal(expectedValue, actualAsTyped.Value);
    }

    
}
