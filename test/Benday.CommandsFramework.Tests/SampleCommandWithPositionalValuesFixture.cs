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
            ApplicationConstants.CommandName_CommandWithDefaultValues,
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
    public void CreateAndRun_Valid_NoArgsSuppliedUsesDefaults()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            ApplicationConstants.CommandName_CommandWithDefaultValues
            );

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new SampleCommandWithPositionalSources(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert        
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.IsTrue(output.Contains("** SUCCESS **"), "Did not contain expected string");

        Assert.IsTrue(output.Contains($"thing-date: {new DateTime(2023, 06, 23)}"), "Did not contain expected string for thing-date");
        Assert.IsTrue(output.Contains("thing-number: 123"), "Did not contain expected string for thing-number");
        Assert.IsTrue(output.Contains("isThingy: True"), "Did not contain expected string for isThingy");
        Assert.IsTrue(output.Contains("bingbong: wickid awesome"), "Did not contain expected string for bingbong");
    }

    [TestMethod]
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

        _SystemUnderTest = new SampleCommandWithPositionalSources(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert        
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.IsTrue(output.Contains("** SUCCESS **"), "Did not contain expected string");

        Assert.IsTrue(output.Contains($"thing-date: {new DateTime(2001, 01, 01)}"), "Did not contain expected string for thing-date");
        Assert.IsTrue(output.Contains("thing-number: 456"), "Did not contain expected string for thing-number");
        Assert.IsTrue(output.Contains("isThingy: False"), "Did not contain expected string for isThingy");
        Assert.IsTrue(output.Contains("bingbong: blah"), "Did not contain expected string bingbong");
    }
}