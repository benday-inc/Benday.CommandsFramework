using Benday.CommandsFramework.Samples;
using System.Reflection.Metadata;

namespace Benday.CommandsFramework.Tests;

[TestClass]
public class SampleCommandWithDefaultValuesFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _SystemUnderTest = null;
        _OutputProvider = null;
    }

    private SampleCommandWithDefaultValues? _SystemUnderTest;

    private SampleCommandWithDefaultValues SystemUnderTest
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

        _SystemUnderTest = new SampleCommandWithDefaultValues(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert        
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.IsFalse(output.Contains("** SUCCESS **"), "Should not contain expected string");
        Assert.IsFalse(output.Contains("** INVALID ARGUMENTS **"), "Should not contain expected string");
        Assert.IsTrue(output.Contains("** USAGE **"), "Did not contain expected string");
    }

    [TestMethod]
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

        _SystemUnderTest = new SampleCommandWithDefaultValues(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert        
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.IsTrue(output.Contains("** SUCCESS **"), "Did not contain expected string");

        Assert.IsTrue(output.Contains("thing-date: 1/1/2001"), "Did not contain expected string for thing-date");
        Assert.IsTrue(output.Contains("thing-number: 456"), "Did not contain expected string for thing-number");
        Assert.IsTrue(output.Contains("isThingy: False"), "Did not contain expected string for isThingy");
        Assert.IsTrue(output.Contains("bingbong: blah"), "Did not contain expected string bingbong");
    }
}