using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

public class SampleCommandWithNoArgOptionsFixture
{
        public SampleCommandWithNoArgOptionsFixture()
    {
        _SystemUnderTest = null;
        _OutputProvider = null;
    }

    private SampleCommandWithNoArgOptions? _SystemUnderTest;

    private SampleCommandWithNoArgOptions SystemUnderTest
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
    public void CreateAndRun_DisplayUsage()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            "commandname1",
            ArgumentFrameworkConstants.ArgumentHelpString
            );

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new SampleCommandWithNoArgOptions(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.Contains("** USAGE **", output);
        
        var lines = output.Split(Environment.NewLine);

        var expectedLineStarts = new string[]
        {
            
        };

        foreach (var lineStart in expectedLineStarts)
        {
            var foundLine = lines.Any(x => x.StartsWith(lineStart));

            Assert.True(foundLine);
        }
    }
}
