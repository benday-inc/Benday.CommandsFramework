using Benday.CommandsFramework.Samples;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System.Reflection.Metadata;

namespace Benday.CommandsFramework.Tests;

[TestClass]
public class SampleCommandWithPositionalSourcesFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _SystemUnderTest = null;
        _OutputProvider = null;
    }

    private SampleCommandWithPositionalSources? _SystemUnderTest;

    private SampleCommandWithPositionalSources SystemUnderTest
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
    public void GetHelp()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            ApplicationConstants.CommandName_CommandWithPositionalSources,
            ArgumentFrameworkConstants.ArgumentHelpString
            );

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new SampleCommandWithPositionalSources(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert        
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.IsFalse(output.Contains("** SUCCESS **"), "Should not contain expected string");
        Assert.IsFalse(output.Contains("** INVALID ARGUMENTS **"), "Should not contain expected string");
        Assert.IsTrue(output.Contains("** USAGE **"), "Did not contain expected string");

        AssertDoesNotContain(output, "/Value1");
        AssertDoesNotContain(output, "[/Value1");

        AssertContains(output, "{Value1:String}");
        AssertContains(output, "[{Value2:String}]");
    }

    private void AssertDoesNotContain(string actual, string notExpected)
    {
        Assert.IsFalse(actual.Contains(notExpected), $"Should not contain string '{notExpected}'. Value was '{actual}'");
    }

    private void AssertContains(string actual, string expected)
    {
        Assert.IsTrue(actual.Contains(expected), $"Expected value to contain string '{expected}'. Value was '{actual}'");
    }

    [TestMethod]
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
        Assert.IsTrue(output.Contains("** SUCCESS **"), "Did not contain expected string");

        AssertContains(output, "Value1: value 1 value");
    }

    [TestMethod]
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
        Assert.IsTrue(output.Contains("** SUCCESS **"), "Did not contain expected string");

        AssertContains(output, "Value1: value 1 value");
        AssertContains(output, "Value2: value 2 value");
    }
}