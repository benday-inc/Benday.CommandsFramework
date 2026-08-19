using Benday.CommandsFramework.Samples;
using Benday.Common.Testing;

namespace Benday.CommandsFramework.Tests;


public class SampleCommandWithConfigArgsFixture : TestClassBase
{
    public SampleCommandWithConfigArgsFixture(
        ITestOutputHelper output) : base(output)
    {
        _SystemUnderTest = null;
        _OutputProvider = null;
    }

    private SampleCommandWithConfigArgs? _SystemUnderTest;

    private SampleCommandWithConfigArgs SystemUnderTest
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
    public async Task CreateAndRun_Valid()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            "api-call");

        var factory = new ArgumentCollectionFactory();

        var config = new InMemoryConfigurationManager(
        "CommandsFrameworkTests-Deletable");

        var executionInfo = factory.Parse(commandLineArgs);
        executionInfo.Configuration = config;

        config.SetValue("base-url", "base url value");
        config.SetValue("api-key", "api key value");

        _SystemUnderTest = new SampleCommandWithConfigArgs(executionInfo, OutputProvider);

        await // act
        _SystemUnderTest.ExecuteAsync(TestContext.Current.CancellationToken);

        // assert        
        var output = OutputProvider.GetOutput();

        WriteLine("Output:");
        WriteLine(output);

        AssertThatString.IsNotNullOrWhiteSpace(output, "output was empty");


        AssertThatString.DoesNotContain(
            output, "** SUCCESS **", "should not succeed");

    }

    [Fact]
    public async Task CreateAndRun_Invalid_WithoutRequiredConfig()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            "api-call");

        var factory = new ArgumentCollectionFactory();

        var config = new InMemoryConfigurationManager(
        "CommandsFrameworkTests-Deletable"); ;

        var executionInfo = factory.Parse(commandLineArgs);
        executionInfo.Configuration = config;

        _SystemUnderTest = new SampleCommandWithConfigArgs(executionInfo, OutputProvider);

        await // act
        _SystemUnderTest.ExecuteAsync(TestContext.Current.CancellationToken);

        // assert        
        var output = OutputProvider.GetOutput();

        WriteLine("Output:");
        WriteLine(output);

        AssertThatString.IsNotNullOrWhiteSpace(output, "output was empty");


        AssertThatString.DoesNotContain(
            output, "** SUCCESS **", "should not succeed");

        AssertThatString.Contains(
            output, "api-key is not valid or missing", "api-key should fail");
        AssertThatString.Contains(
            output, "base-url is not valid or missing", "base-url should fail");
    }

    [Fact]
    public async Task CreateAndRun_Invalid_PartialRequiredConfig_MissingApiKey()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            "api-call");

        var factory = new ArgumentCollectionFactory();

        var config = new InMemoryConfigurationManager(
                "CommandsFrameworkTests-Deletable"); ;

        var executionInfo = factory.Parse(commandLineArgs);
        executionInfo.Configuration = config;

        config.SetValue("base-url", "base url value");

        _SystemUnderTest = new SampleCommandWithConfigArgs(executionInfo, OutputProvider);

        await // act
        _SystemUnderTest.ExecuteAsync(TestContext.Current.CancellationToken);

        // assert        
        var output = OutputProvider.GetOutput();

        WriteLine("Output:");
        WriteLine(output);

        AssertThatString.IsNotNullOrWhiteSpace(output, "output was empty");

        AssertThatString.DoesNotContain(
            output, "** SUCCESS **", "should not succeed");

        AssertThatString.DoesNotContain(
            output, "base-url is not valid or missing", "base-url should not fail");

        AssertThatString.Contains(
            output, "api-key is not valid or missing", "api-key should fail");
    }

    [Fact]
    public async Task CreateAndRun_Invalid_PartialRequiredConfig_MissingBaseUrl()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            "api-call");

        var factory = new ArgumentCollectionFactory();

        var config = new InMemoryConfigurationManager(
                "CommandsFrameworkTests-Deletable"); ;

        var executionInfo = factory.Parse(commandLineArgs);
        executionInfo.Configuration = config;

        config.SetValue("api-key", "api key value");

        _SystemUnderTest = new SampleCommandWithConfigArgs(executionInfo, OutputProvider);

        await // act
        _SystemUnderTest.ExecuteAsync(TestContext.Current.CancellationToken);

        // assert        
        var output = OutputProvider.GetOutput();

        WriteLine("Output:");
        WriteLine(output);

        AssertThatString.IsNotNullOrWhiteSpace(output, "output was empty");

        AssertThatString.DoesNotContain(
            output, "** SUCCESS **", "should not succeed");

        AssertThatString.Contains(
            output, "base-url is not valid or missing", "base-url should fail");

        AssertThatString.DoesNotContain(
            output, "api-key is not valid or missing", "api-key should not fail");
    }

}
