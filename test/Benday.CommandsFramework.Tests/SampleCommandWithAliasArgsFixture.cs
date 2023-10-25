using Benday.CommandsFramework.Samples;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System.Reflection.Metadata;

namespace Benday.CommandsFramework.Tests;

[TestClass]
public class SampleCommandWithAliasArgsFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _SystemUnderTest = null;
        _OutputProvider = null;
    }

    private SampleCommandWithAliasArgs? _SystemUnderTest;

    private SampleCommandWithAliasArgs SystemUnderTest
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
            ApplicationConstants.CommandName_CommandWithAliases,
            ArgumentFrameworkConstants.ArgumentHelpString
            );

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new SampleCommandWithAliasArgs(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert        
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.IsFalse(output.Contains("** SUCCESS **"), "Should not contain expected string");
        Assert.IsFalse(output.Contains("** INVALID ARGUMENTS **"), "Should not contain expected string");
        Assert.IsTrue(output.Contains("** USAGE **"), "Did not contain expected string");

        AssertContains(output, "/Value1");
        AssertContains(output, "[/Value2");
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
        Assert.IsTrue(output.Contains("** SUCCESS **"), "Did not contain expected string");

        AssertContains(output, "Value1: value1value");
    }

    [TestMethod]
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
        Assert.IsTrue(output.Contains("** SUCCESS **"), "Did not contain expected string");

        AssertContains(output, "Value1: value1value");
        AssertContains(output, "Value2: value2value");
    }
}