using Benday.CommandsFramework.Samples;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System.Reflection.Metadata;

namespace Benday.CommandsFramework.Tests;

public class SampleCommandWithAliasArgsFixture
{
        public SampleCommandWithAliasArgsFixture()
    {
        _SystemUnderTest = null;
        _OutputProvider = null;
    }

    private SampleCommandWithAliasArgs? _SystemUnderTest;

    private SampleCommandWithAliasArgs SystemUnderTest
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
            ApplicationConstants.CommandName_CommandWithAliases);

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new SampleCommandWithAliasArgs(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert        
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.False(output.Contains("** SUCCESS **"));
        Assert.False(output.Contains("** INVALID ARGUMENTS **"));
        Assert.True(output.Contains("** USAGE **"));

        AssertContains(output, "/Value1");
        AssertContains(output, "[/Value2");
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
    public void CreateAndRun_Valid_RequiredAliasAppearsInValues_OnlyRequired()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            ApplicationConstants.CommandName_CommandWithAliases,
            "/Alias1:value1value"
            );

        var factory = new ArgumentCollectionFactory();

        var executionInfo = factory.Parse(commandLineArgs);

        _SystemUnderTest = new SampleCommandWithAliasArgs(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert        
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.True(output.Contains("** SUCCESS **"));

        AssertContains(output, "Value1: value1value");
    }

    [Fact]
    public void CreateAndRun_Valid_RequiredAliasAppearsInValues_RequiredAndOptional()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            ApplicationConstants.CommandName_CommandWithAliases,
            "/Alias1:value1value",
            "/Alias2:value2value"
            );

        var factory = new ArgumentCollectionFactory();

        var executionInfo = factory.Parse(commandLineArgs);

        _SystemUnderTest = new SampleCommandWithAliasArgs(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert        
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.True(output.Contains("** SUCCESS **"));

        AssertContains(output, "Value1: value1value");
        AssertContains(output, "Value2: value2value");
    }
}