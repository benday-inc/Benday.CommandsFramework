using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

[TestClass]
public class SampleCommand1Fixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _SystemUnderTest = null;
    }

    private SampleCommand1? _SystemUnderTest;

    private SampleCommand1 SystemUnderTest
    {
        get
        {
            Assert.IsNotNull(_SystemUnderTest);

            return _SystemUnderTest;
        }
    }

    [TestMethod]
    public void CreateAndRun_ValidArgs()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            "commandname1",
            "/arg1:Hello",
            "/isawesome:true",
            "/count:4321",
            "/dateofthingy:12/25/2022",
            "/verbose"
            );

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new SampleCommand1(executionInfo);

        // act
        _SystemUnderTest.Execute();

        // assert        
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void CreateAndRun_InvalidArgs()
    {
        // arrange
        var commandLineArgs = Utilities.GetStringArray(
            "commandname1",
            "/arg1:Hello",
            "/isawesome2:true",
            "/count:4321",
            "/dateofthingy:notADateValue"
            );

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new SampleCommand1(executionInfo);

        // act
        _SystemUnderTest.Execute();

        // assert        
    }


}
