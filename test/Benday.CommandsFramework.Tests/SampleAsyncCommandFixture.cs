using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

public class SampleAsyncCommandFixture
{
        public SampleAsyncCommandFixture()
    {
        _SystemUnderTest = null;
        _OutputProvider = null;
    }

    private SampleAsyncCommand? _SystemUnderTest;

    private SampleAsyncCommand SystemUnderTest
    {
        get
        {
            Assert.NotNull(_SystemUnderTest);

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


    [Fact]
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
        Assert.Contains("** SUCCESS **", output);
        Assert.Contains("isawesome:False:", output);
    }

    [Fact]
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
        Assert.Contains("** SUCCESS **", output);
        Assert.Contains("isawesome:True:", output);
    }

    [Fact]
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
        Assert.DoesNotContain("** SUCCESS **", output);
        Assert.DoesNotContain("** INVALID ARGUMENTS **", output);
        Assert.Contains("** USAGE **", output);

    }
}
