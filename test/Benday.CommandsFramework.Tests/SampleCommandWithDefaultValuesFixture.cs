using Benday.CommandsFramework.Samples;
using System.Reflection.Metadata;

namespace Benday.CommandsFramework.Tests;

public class SampleCommandWithDefaultValuesFixture
{
        public SampleCommandWithDefaultValuesFixture()
    {
        _SystemUnderTest = null;
        _OutputProvider = null;
    }

    private SampleCommandWithDefaultValues? _SystemUnderTest;

    private SampleCommandWithDefaultValues SystemUnderTest
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
            ApplicationConstants.CommandName_CommandWithDefaultValues,
            ArgumentFrameworkConstants.ArgumentHelpString
            );

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new SampleCommandWithDefaultValues(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert        
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.False(output.Contains("** SUCCESS **"));
        Assert.False(output.Contains("** INVALID ARGUMENTS **"));
        Assert.True(output.Contains("** USAGE **"));
    }

    [Fact]
    public void CreateAndRun_Valid_NoArgsSuppliedUsesDefaults()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            ApplicationConstants.CommandName_CommandWithDefaultValues
            );

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new SampleCommandWithDefaultValues(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert        
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.True(output.Contains("** SUCCESS **"));

        Assert.True(output.Contains($"thing-date: {new DateTime(2023, 06, 23)}"));
        Assert.True(output.Contains("thing-number: 123"));
        Assert.True(output.Contains("isThingy: True"));
        Assert.True(output.Contains("bingbong: wickid awesome"));
    }

    [Fact]
    public void CreateAndRun_Valid_UsesSuppliedValuesRatherThanDefaults()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            ApplicationConstants.CommandName_CommandWithDefaultValues,
            "/thing-date:1/1/2001",
            "/thing-number:456",
            "/isThingy:false",
            "/bingbong:blah"
            );

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new SampleCommandWithDefaultValues(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert        
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.True(output.Contains("** SUCCESS **"));

        Assert.True(output.Contains($"thing-date: {new DateTime(2001, 01, 01)}"));
        Assert.True(output.Contains("thing-number: 456"));
        Assert.True(output.Contains("isThingy: False"));
        Assert.True(output.Contains("bingbong: blah"));
    }
}