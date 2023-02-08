using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

[TestClass]
public class SampleAsyncCommandFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _SystemUnderTest = null;
        _OutputProvider = null;
    }

    private SampleAsyncCommand? _SystemUnderTest;

    private SampleAsyncCommand SystemUnderTest
    {
        get
        {
            Assert.IsNotNull(_SystemUnderTest);

            return _SystemUnderTest;
        }
    }

    private StringBuilderTextOutputProvider? _OutputProvider;

    private StringBuilderTextOutputProvider OutputProvider
    {
        get
        {
            if (_OutputProvider == null)
            {
                _OutputProvider = new StringBuilderTextOutputProvider();
            }

            return _OutputProvider;
        }
    }


    [TestMethod]
    public async Task CreateAndRun_ValidArgs_IsAwesomeFalse()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            "commandname2",
            "/isawesome:false"
            );

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new SampleAsyncCommand(executionInfo, OutputProvider);

        // act
        await _SystemUnderTest.ExecuteAsync();

        // assert        
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.IsTrue(output.Contains("** SUCCESS **"), "Did not contain expected string");
        Assert.IsTrue(output.Contains("isawesome:False:"), "Did not contain expected result string");
    }

    [TestMethod]
    public async Task CreateAndRun_ValidArgs_IsAwesomeTrue()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            "commandname2",
            "/isawesome:true"
            );

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new SampleAsyncCommand(executionInfo, OutputProvider);

        // act
        await _SystemUnderTest.ExecuteAsync();

        // assert        
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.IsTrue(output.Contains("** SUCCESS **"), "Did not contain expected string");
        Assert.IsTrue(output.Contains("isawesome:True:"), "Did not contain expected result string");
    }

    [TestMethod]
    public async Task GetHelp()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            "commandname2",
            ArgumentFrameworkConstants.ArgumentHelpString
            );

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new SampleAsyncCommand(executionInfo, OutputProvider);

        // act
        await _SystemUnderTest.ExecuteAsync();

        // assert        
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.IsFalse(output.Contains("** SUCCESS **"), "Should not contain expected string");
        Assert.IsFalse(output.Contains("** INVALID ARGUMENTS **"), "Should not contain expected string");
        Assert.IsTrue(output.Contains("** USAGE **"), "Did not contain expected string");

    }
}
