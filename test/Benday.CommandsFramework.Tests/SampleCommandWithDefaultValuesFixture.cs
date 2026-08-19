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
    public async Task GetHelp()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            ApplicationConstants.CommandName_CommandWithDefaultValues,
            ArgumentFrameworkConstants.ArgumentHelpString
            );

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new SampleCommandWithDefaultValues(executionInfo, OutputProvider);

        await // act
        _SystemUnderTest.ExecuteAsync(TestContext.Current.CancellationToken);

        // assert
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.DoesNotContain("** SUCCESS **", output);
        Assert.DoesNotContain("** INVALID ARGUMENTS **", output);
        Assert.Contains("** USAGE **", output);
    }

    [Fact]
    public async Task GetHelp_ShowsDefaultValues()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            ApplicationConstants.CommandName_CommandWithDefaultValues,
            ArgumentFrameworkConstants.ArgumentHelpString
            );

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new SampleCommandWithDefaultValues(executionInfo, OutputProvider);

        await // act
        _SystemUnderTest.ExecuteAsync(TestContext.Current.CancellationToken);

        // assert
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);

        Assert.Contains($"(default: {new DateTime(2023, 6, 23)})", output);
        Assert.Contains("(default: 123)", output);
        Assert.Contains("(default: True)", output);
        Assert.Contains("(default: wickid awesome)", output);
    }

    [Fact]
    public async Task GetHelp_DefaultValueIsOnItsOwnLineAlignedToDescriptionColumn()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            ApplicationConstants.CommandName_CommandWithDefaultValues,
            ArgumentFrameworkConstants.ArgumentHelpString
            );

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new SampleCommandWithDefaultValues(executionInfo, OutputProvider);

        await // act
        _SystemUnderTest.ExecuteAsync(TestContext.Current.CancellationToken);

        // assert
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);

        var lines = output.Split(Environment.NewLine);

        var descriptionLine = Assert.Single(lines, l => l.Contains("thingy number"));

        var defaultLine = Assert.Single(lines, l => l.Contains("(default: 123)"));

        // the default is on its own line, not appended to the description
        Assert.DoesNotContain("(default:", descriptionLine);

        // ...and it starts in the same column as the description text
        var descriptionColumn = descriptionLine.IndexOf("thingy number", StringComparison.Ordinal);
        var defaultColumn = defaultLine.IndexOf("(default:", StringComparison.Ordinal);

        Assert.Equal(descriptionColumn, defaultColumn);
    }

    [Fact]
    public async Task CreateAndRun_Valid_NoArgsSuppliedUsesDefaults()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            ApplicationConstants.CommandName_CommandWithDefaultValues
            );

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new SampleCommandWithDefaultValues(executionInfo, OutputProvider);

        await // act
        _SystemUnderTest.ExecuteAsync(TestContext.Current.CancellationToken);

        // assert        
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.Contains("** SUCCESS **", output);

        Assert.Contains($"thing-date: {new DateTime(2023, 06, 23)}", output);
        Assert.Contains("thing-number: 123", output);
        Assert.Contains("isThingy: True", output);
        Assert.Contains("bingbong: wickid awesome", output);
    }

    [Fact]
    public async Task CreateAndRun_Valid_UsesSuppliedValuesRatherThanDefaults()
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

        await // act
        _SystemUnderTest.ExecuteAsync(TestContext.Current.CancellationToken);

        // assert        
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Assert.Contains("** SUCCESS **", output);

        Assert.Contains($"thing-date: {new DateTime(2001, 01, 01)}", output);
        Assert.Contains("thing-number: 456", output);
        Assert.Contains("isThingy: False", output);
        Assert.Contains("bingbong: blah", output);
    }
}