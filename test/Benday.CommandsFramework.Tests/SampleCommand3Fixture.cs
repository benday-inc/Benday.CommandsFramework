using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

public class SampleCommand3Fixture
{
        public SampleCommand3Fixture()
    {
        _SystemUnderTest = null;
        _OutputProvider = null;
    }

    private SampleCommand3? _SystemUnderTest;

    private SampleCommand3 SystemUnderTest
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
    public void GetHelp()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            "commandname3");

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new SampleCommand3(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert        
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.False(output.Contains("** SUCCESS **"));
        Assert.False(output.Contains("** INVALID ARGUMENTS **"));
        Assert.True(output.Contains("** USAGE **"));

    }
}
