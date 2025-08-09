using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

public class SampleCommand1Fixture
{
        public SampleCommand1Fixture()
    {
        _SystemUnderTest = null;
        _OutputProvider = null;
    }

    private SampleCommand1? _SystemUnderTest;

    private SampleCommand1 SystemUnderTest
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
    public void CreateAndRun_ValidArgs()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            "commandname1",
            "/arg1:Hello",
            "/isawesome:true",
            "/count:4321",
            "/dateofthingy:12/25/2022",
            "/verbose"
            );

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new SampleCommand1(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert        
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.Contains("** SUCCESS **", output);
    }

    [Fact]
    public void CreateAndRun_MultipleInvalidArgs()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            "commandname1",
            "/arg1:Hello",
            "/isawesome2:true",
            "/count:4321",
            "/dateofthingy:notADateValue"
            );

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new SampleCommand1(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert        
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.Contains("** INVALID ARGUMENTS **", output);
    }

    [Fact]
    public void CreateAndRun_OneInvalidArg()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            "commandname1",
            "/arg1:Hello",
            "/isawesome2:true",
            "/count:4321",
            "/dateofthingy:12/25/2022",
            "/verbose"
            );

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new SampleCommand1(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert        
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.Contains("** INVALID ARGUMENT **", output);
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

        _SystemUnderTest = new SampleCommand1(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.Contains("** USAGE **", output);
        Assert.Contains("This is the description for command one.", output);

        var lines = output.Split(Environment.NewLine);

        var expectedLineStarts = new string[]
        {
            "/arg1",
            "/isawesome",
            "/count",
            "/dateofthingy",
            "[/verbose"
        };

        foreach (var lineStart in expectedLineStarts)
        {
            var foundLine = lines.Any(x => x.StartsWith(lineStart));

            Assert.True(foundLine);
        }
    }
}
