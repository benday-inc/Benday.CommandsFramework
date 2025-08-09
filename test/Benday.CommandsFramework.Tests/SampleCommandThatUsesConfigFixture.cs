using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

public class SampleCommandThatUsesConfigFixture
{
        public SampleCommandThatUsesConfigFixture()
    {
        _SystemUnderTest = null;
        _OutputProvider = null;
    }

    private SampleCommandThatUsesConfig? _SystemUnderTest;

    private SampleCommandThatUsesConfig SystemUnderTest
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
    public void CreateAndRun_Valid_RequiredPositionalAppearsInValues_OnlyRequired()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            ApplicationConstants.CommandName_CommandThatUsesConfig);

        var factory = new ArgumentCollectionFactory();

        var executionInfo = factory.Parse(commandLineArgs);
        executionInfo.Configuration = new FileBasedConfigurationManager("CommandsFrameworkTests-Deletable");

        _SystemUnderTest = new SampleCommandThatUsesConfig(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert        
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.Contains("** SUCCESS **", output);        
    }

}