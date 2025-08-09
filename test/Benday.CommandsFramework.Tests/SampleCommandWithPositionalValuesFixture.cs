using Benday.CommandsFramework.Samples;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System.Reflection.Metadata;

namespace Benday.CommandsFramework.Tests;

public class SampleCommandWithPositionalSourcesFixture
{
        public SampleCommandWithPositionalSourcesFixture()
    {
        _SystemUnderTest = null;
        _OutputProvider = null;
    }

    private SampleCommandWithPositionalSources? _SystemUnderTest;

    private SampleCommandWithPositionalSources SystemUnderTest
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
            ApplicationConstants.CommandName_CommandWithPositionalSources);

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new SampleCommandWithPositionalSources(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert        
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.False(output.Contains("** SUCCESS **"));
        Assert.False(output.Contains("** INVALID ARGUMENTS **"));
        Assert.True(output.Contains("** USAGE **"));

        AssertDoesNotContain(output, "/Value1");
        AssertDoesNotContain(output, "[/Value1");

        AssertContains(output, "{Value1:String}");
        AssertContains(output, "[{Value2:String}]");
    }

    private void AssertDoesNotContain(string actual, string notExpected)
    {
        Assert.False(actual.Contains(notExpected));
    }

    private void AssertContains(string actual, string expected)
    {
        Assert.True(actual.Contains(expected));
    }

    [Fact]
    public void CreateAndRun_Valid_RequiredPositionalAppearsInValues_OnlyRequired()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            ApplicationConstants.CommandName_CommandWithPositionalSources,
            "value 1 value"
            );

        var factory = new ArgumentCollectionFactory();

        var executionInfo = factory.Parse(commandLineArgs);

        _SystemUnderTest = new SampleCommandWithPositionalSources(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert        
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.True(output.Contains("** SUCCESS **"));

        AssertContains(output, "Value1: value 1 value");
    }

    [Fact]
    public void CreateAndRun_Valid_RequiredPositionalAppearsInValues_RequiredAndOptional()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            ApplicationConstants.CommandName_CommandWithPositionalSources,
            "value 1 value",
            "value 2 value"
            );

        var factory = new ArgumentCollectionFactory();

        var executionInfo = factory.Parse(commandLineArgs);

        _SystemUnderTest = new SampleCommandWithPositionalSources(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert        
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.True(output.Contains("** SUCCESS **"));

        AssertContains(output, "Value1: value 1 value");
        AssertContains(output, "Value2: value 2 value");
    }
}
