using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

[TestClass]
public class SampleCommandWithNoArgOptionsFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _SystemUnderTest = null;
        _OutputProvider = null;
    }

    private SampleCommandWithNoArgOptions? _SystemUnderTest;

    private SampleCommandWithNoArgOptions SystemUnderTest
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
        Assert.IsTrue(output.Contains("** USAGE **"), "Did not contain expected string");
        
        var lines = output.Split(Environment.NewLine);

        var expectedLineStarts = new string[]
        {
            
        };

        foreach (var lineStart in expectedLineStarts)
        {
            var foundLine = lines.Any(x => x.StartsWith(lineStart));

            Assert.IsTrue(foundLine, $"Did not find line that starts with '{lineStart}'");
        }
    }
}
