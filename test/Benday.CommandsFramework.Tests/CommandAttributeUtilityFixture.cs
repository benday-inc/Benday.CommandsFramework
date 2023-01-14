using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

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
                _SystemUnderTest = new CommandAttributeUtility();
            }

            return _SystemUnderTest;
        }
    }

    [TestMethod]
    public void GetAvailableCommandNames()
    {
        // arrange
        var expectedCount = 3;
        var sampleAssembly = typeof(Benday.CommandsFramework.Samples.SampleCommand1).Assembly;

        // act
        var actual = SystemUnderTest.GetAvailableCommandNames(sampleAssembly);

        // assert
        Assert.IsNotNull(actual, "Result was null");
        Assert.AreNotEqual<int>(0, actual.Count, "Result count was zero");
        Assert.AreEqual<int>(expectedCount, actual.Count, "result count wrong");

        actual.ForEach(x => { Console.WriteLine($"{x}"); });
    }
}