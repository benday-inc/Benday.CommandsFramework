using Benday.CommandsFramework.Samples;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Text;

namespace Benday.CommandsFramework.Tests;

[TestClass]
public class CommandAttributeUtilityFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _SystemUnderTest = null;
    }

    private CommandAttributeUtility? _SystemUnderTest;

    private CommandAttributeUtility SystemUnderTest
    {
        get
        {
            if (_SystemUnderTest == null)
            {
                _SystemUnderTest = new CommandAttributeUtility(CommandProgramOptionsInstance);
            }

            return _SystemUnderTest;
        }
    }

    private ICommandProgramOptions? _CommandProgramOptionsInstance;
    private ICommandProgramOptions CommandProgramOptionsInstance
    {
        get
        {
            if (_CommandProgramOptionsInstance == null)
            {
                var options = new DefaultProgramOptions();

                options.ApplicationName = "Test Sample Application";
                options.Version = "1.0.0";  
                options.Website = "https://www.benday.com";
                options.ConfigurationFolderName = "TestSampleApplication";

                _CommandProgramOptionsInstance = options;
            }

            return _CommandProgramOptionsInstance;
        }
    }

    [TestMethod]
    public void GetAvailableCommandNames()
    {
        // arrange
        var expectedCount = 6;
        var sampleAssembly = typeof(Benday.CommandsFramework.Samples.SampleCommand1).Assembly;

        // act
        var actual = SystemUnderTest.GetAvailableCommandNames(sampleAssembly);

        // assert
        Assert.IsNotNull(actual, "Result was null");
        Assert.AreNotEqual<int>(0, actual.Count, "Result count was zero");
        Assert.AreEqual<int>(expectedCount, actual.Count, "result count wrong");

        actual.ForEach(x => { Console.WriteLine($"{x}"); });
    }

    [TestMethod]
    public void GetInstance_Command1()
    {
        // arrange
        var expectedCommandName = ApplicationConstants.CommandName_Command1;
        var expectedKey1 = "arg1";
        var expectedVal1 = "value1";
        var expectedKey2 = "arg2";
        var expectedVal2 = "value with spaces";
        var expectedKey3 = "arg3";
        var expectedVal3 = string.Empty;

        string[] args = { expectedCommandName,
            $"/{expectedKey1}:{expectedVal1}",
            $"/{expectedKey2}:\"{expectedVal2}\"",
            $"/{expectedKey3}"
        };

        var sampleAssembly = typeof(Benday.CommandsFramework.Samples.SampleCommand1).Assembly;

        // act
        var actual = SystemUnderTest.GetCommand(args, sampleAssembly);

        // assert
        Assert.IsNotNull(actual, "Result was null");
        Assert.IsNotNull(actual.ExecutionInfo, "Execution info was null");
        Assert.AreEqual<string>(expectedCommandName, actual.ExecutionInfo.CommandName, "Command name was wrong");
        Assert.AreEqual<int>(3, actual.ExecutionInfo.Arguments.Count, "Arg count was wrong");
        Assert.AreSame(CommandProgramOptionsInstance, actual.ExecutionInfo.Options, "Options was wrong");
    }

    [TestMethod]
    public void GetInstance_Command2()
    {
        // arrange
        var expectedCommandName = ApplicationConstants.CommandName_Command2;
        var expectedKey1 = "arg1";
        var expectedVal1 = "value1";
        var expectedKey2 = "arg2";
        var expectedVal2 = "value with spaces";
        var expectedKey3 = "arg3";
        var expectedVal3 = string.Empty;

        string[] args = { expectedCommandName,
            $"/{expectedKey1}:{expectedVal1}",
            $"/{expectedKey2}:\"{expectedVal2}\"",
            $"/{expectedKey3}"
        };

        var sampleAssembly = typeof(Benday.CommandsFramework.Samples.SampleCommand1).Assembly;

        // act
        var actual = SystemUnderTest.GetCommand(args, sampleAssembly);

        // assert
        Assert.IsNotNull(actual, "Result was null");
        Assert.IsNotNull(actual.ExecutionInfo, "Execution info was null");
        Assert.AreEqual<string>(expectedCommandName, actual.ExecutionInfo.CommandName, "Command name was wrong");
        Assert.AreEqual<int>(3, actual.ExecutionInfo.Arguments.Count, "Arg count was wrong");
        Assert.AreSame(CommandProgramOptionsInstance, actual.ExecutionInfo.Options, "Options was wrong");
    }

    [TestMethod]
    public void GetInstance_Command3()
    {
        // arrange
        var expectedCommandName = ApplicationConstants.CommandName_Command3;
        var expectedKey1 = "arg1";
        var expectedVal1 = "value1";
        var expectedKey2 = "arg2";
        var expectedVal2 = "value with spaces";
        var expectedKey3 = "arg3";
        var expectedVal3 = string.Empty;

        string[] args = { expectedCommandName,
            $"/{expectedKey1}:{expectedVal1}",
            $"/{expectedKey2}:\"{expectedVal2}\"",
            $"/{expectedKey3}"
        };

        var sampleAssembly = typeof(Benday.CommandsFramework.Samples.SampleCommand1).Assembly;

        // act
        var actual = SystemUnderTest.GetCommand(args, sampleAssembly);

        // assert
        Assert.IsNotNull(actual, "Result was null");
        Assert.IsNotNull(actual.ExecutionInfo, "Execution info was null");
        Assert.AreEqual<string>(expectedCommandName, actual.ExecutionInfo.CommandName, "Command name was wrong");
        Assert.AreEqual<int>(3, actual.ExecutionInfo.Arguments.Count, "Arg count was wrong");
        Assert.AreSame(CommandProgramOptionsInstance, actual.ExecutionInfo.Options, "Options was wrong");
    }

    [TestMethod]
    [ExpectedException(typeof(MissingArgumentException))]
    public void GetInstance_BogusCommandName_ThrowsException()
    {
        // arrange
        var expectedCommandName = "boguscommandname";
        var expectedKey1 = "arg1";
        var expectedVal1 = "value1";
        var expectedKey2 = "arg2";
        var expectedVal2 = "value with spaces";
        var expectedKey3 = "arg3";
        var expectedVal3 = string.Empty;

        string[] args = { expectedCommandName,
            $"/{expectedKey1}:{expectedVal1}",
            $"/{expectedKey2}:\"{expectedVal2}\"",
            $"/{expectedKey3}"
        };

        var sampleAssembly = typeof(Benday.CommandsFramework.Samples.SampleCommand1).Assembly;

        // act
        var actual = SystemUnderTest.GetCommand(args, sampleAssembly);

        // assert
        Assert.IsNotNull(actual, "Result was null");
    }

    [TestMethod]
    public void GetAllCommandUsages()
    {
        // arrange
        var sampleAssembly = typeof(Benday.CommandsFramework.Samples.SampleCommand1).Assembly;
        
        // act
        var actual = SystemUnderTest.GetAllCommandUsages(sampleAssembly);

        // assert
        Assert.AreNotEqual<int>(0, actual.Count, "Usages collection wasn't populated");

        foreach (var item in actual)
        {
            Assert.IsFalse(string.IsNullOrEmpty(item.Name), "Name wasn't populated");
            Assert.IsNotNull(item.Description, $"Description wasn't populated for '{item.Name}'");

            foreach (var arg in item.Arguments)
            {
                Assert.IsFalse(string.IsNullOrEmpty(arg.Name), "arg.Name wasn't populated");
                Assert.IsNotNull(arg.Description, "arg.Description wasn't populated");
            }
        }
    }
}
