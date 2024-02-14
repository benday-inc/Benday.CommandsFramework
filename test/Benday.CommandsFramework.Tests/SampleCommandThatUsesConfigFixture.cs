using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

[TestClass]
public class SampleCommandThatUsesConfigFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _SystemUnderTest = null;
        _OutputProvider = null;
    }

    private SampleCommandThatUsesConfig? _SystemUnderTest;

    private SampleCommandThatUsesConfig SystemUnderTest
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
    public void CreateAndRun_Valid_RequiredPositionalAppearsInValues_OnlyRequired()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            ApplicationConstants.CommandName_CommandThatUsesConfig,
            "value 1 value"
            );

        var factory = new ArgumentCollectionFactory();

        var executionInfo = factory.Parse(commandLineArgs);
        executionInfo.Configuration = new FileBasedConfigurationManager("CommandsFrameworkTests-Deletable");

        _SystemUnderTest = new SampleCommandThatUsesConfig(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert        
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.IsTrue(output.Contains("** SUCCESS **"), "Did not contain expected string");        
    }

}