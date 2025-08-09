namespace Benday.CommandsFramework.Tests;

public class DirectoryArgumentFixture
{
    public DirectoryArgumentFixture()
    {
        _SystemUnderTest = null;
    }

    private DirectoryArgument? _SystemUnderTest;
    private const string EXPECTED_ARG_NAME = "arg123";
    private const string EXPECTED_ARG_VALUE = "argvalue123";
    private const string EXPECTED_ARG_DESC = "argvalue123 description";
    private const bool EXPECTED_ARG_ISREQUIRED = true;
    private const bool EXPECTED_ARG_ALLOWEMPTYVALUE = true;
    private const ArgumentDataType EXPECTED_ARG_DATATYPE = ArgumentDataType.String;


    private DirectoryArgument SystemUnderTest
    {
        get
        {
            if (_SystemUnderTest == null)
            {
                InitializeWithNoArgs();
            }

            if (_SystemUnderTest == null)
            {
                throw new InvalidOperationException($"System under test is null");
            }

            return _SystemUnderTest;
        }
    }

    private void InitializeWithNoArgs()
    {
        // _SystemUnderTest = new Argument<string>();
        throw new NotImplementedException();
    }


    [Fact]
    public void IsValid_Required_MustExistIsFalse_RelativePath_DirExists()
    {
        // arrange
        var testDir = GetTestDirectory();

        var dirName = testDir.Name;

        var dirValue =
            Path.Combine(Environment.CurrentDirectory, dirName);

        var arg = new ArgumentCollection().AddDirectory(EXPECTED_ARG_NAME)
           .AsRequired()
           .AllowEmptyValue()
           .WithDescription(EXPECTED_ARG_DESC)
           .ExistenceOptional();

        arg.Value = EXPECTED_ARG_VALUE;

        var temp = arg as DirectoryArgument ?? throw new InvalidOperationException("Wrong type");

        _SystemUnderTest = temp;

        // act
        var actual = SystemUnderTest.Validate();

        // assert        
        Assert.False(SystemUnderTest.MustExist);
        Assert.True(actual);
    }

    [Fact]
    public void IsValid_Required_MustExistIsFalse_AbsolutePath_DirExists()
    {
        // arrange
        var testDir = GetTestDirectory();

        var dirName = testDir.Name;

        var dirValue =
            testDir.FullName;

        var arg = new ArgumentCollection().AddDirectory(EXPECTED_ARG_NAME)
           .AsRequired()
           .AllowEmptyValue()
           .WithDescription(EXPECTED_ARG_DESC)
           .ExistenceOptional();

        arg.Value = EXPECTED_ARG_VALUE;

        var temp = arg as DirectoryArgument ?? throw new InvalidOperationException("Wrong type");

        _SystemUnderTest = temp;

        // act
        var actual = SystemUnderTest.Validate();

        // assert        
        Assert.False(SystemUnderTest.MustExist);
        Assert.True(actual);
    }

    [Fact]
    public void IsValid_Required_MustExistIsFalse_RelativePath_DirDoesNotExist()
    {
        // arrange
        var testDir = new DirectoryInfo(
            Path.Combine(Environment.CurrentDirectory, "bogus-dir"));

        var dirName = testDir.Name;

        var dirValue =
            Path.Combine(Environment.CurrentDirectory, dirName);

        var arg = new ArgumentCollection().AddDirectory(EXPECTED_ARG_NAME)
           .AsRequired()
           .AllowEmptyValue()
           .WithDescription(EXPECTED_ARG_DESC)
           .ExistenceOptional();

        arg.Value = EXPECTED_ARG_VALUE;

        var temp = arg as DirectoryArgument ?? throw new InvalidOperationException("Wrong type");

        _SystemUnderTest = temp;

        // act
        var actual = SystemUnderTest.Validate();

        // assert        
        Assert.False(SystemUnderTest.MustExist);
        Assert.True(actual);
    }

    [Fact]
    public void IsValid_Required_MustExistIsTrue_RelativePath_DirDoesNotExist()
    {
        // arrange
        var testDir = new DirectoryInfo(
            Path.Combine(Environment.CurrentDirectory, "bogus-dir"));

        var dirName = testDir.Name;

        var dirValue =
            Path.Combine(Environment.CurrentDirectory, dirName);

        var arg = new ArgumentCollection().AddDirectory(EXPECTED_ARG_NAME)
           .AsRequired()
           .AllowEmptyValue()
           .WithDescription(EXPECTED_ARG_DESC)
           .MustExist();

        arg.Value = EXPECTED_ARG_VALUE;

        var temp = arg as DirectoryArgument ?? throw new InvalidOperationException("Wrong type");

        _SystemUnderTest = temp;

        // act
        var actual = SystemUnderTest.Validate();

        // assert        
        Assert.True(SystemUnderTest.MustExist);
        Assert.False(actual);
    }

    [Fact]
    public void IsValid_Required_MustExistIsTrue_AbsolutePath_DirDoesNotExist()
    {
        // arrange
        var testDir = new DirectoryInfo(
            Path.Combine(Environment.CurrentDirectory, "bogus-dir"));

        var dirName = testDir.FullName;

        var dirValue =
            Path.Combine(Environment.CurrentDirectory, dirName);

        var arg = new ArgumentCollection().AddDirectory(EXPECTED_ARG_NAME)
           .AsRequired()
           .AllowEmptyValue()
           .WithDescription(EXPECTED_ARG_DESC)
           .MustExist();

        arg.Value = EXPECTED_ARG_VALUE;

        var temp = arg as DirectoryArgument ?? throw new InvalidOperationException("Wrong type");

        _SystemUnderTest = temp;

        // act
        var actual = SystemUnderTest.Validate();

        // assert        
        Assert.True(SystemUnderTest.MustExist);
        Assert.False(actual);
    }

    [Fact]
    public void IsValid_Required_MustExistIsFalse_AbsolutePath_DirDoesNotExist()
    {
        // arrange
        var testDir = new DirectoryInfo(
            Path.Combine(Environment.CurrentDirectory, "bogus-dir"));

        var dirName = testDir.Name;

        var dirValue =
            testDir.FullName;

        var arg = new ArgumentCollection().AddDirectory(EXPECTED_ARG_NAME)
           .AsRequired()
           .AllowEmptyValue()
           .WithDescription(EXPECTED_ARG_DESC)
           .ExistenceOptional();

        arg.Value = EXPECTED_ARG_VALUE;

        var temp = arg as DirectoryArgument ?? throw new InvalidOperationException("Wrong type");

        _SystemUnderTest = temp;

        // act
        var actual = SystemUnderTest.Validate();

        // assert        
        Assert.False(SystemUnderTest.MustExist);
        Assert.True(actual);
    }

    [Fact]
    public void IsValid_Required_MustExistIsTrue_RelativePath_DirNotExist()
    {
        // arrange
        var testDir = GetTestDirectory();

        var dirName = testDir.Name;

        var dirValue =
            Path.Combine(Environment.CurrentDirectory, dirName);

        var arg = new ArgumentCollection().AddDirectory(EXPECTED_ARG_NAME)
           .AsRequired()
           .AllowEmptyValue()
           .WithDescription(EXPECTED_ARG_DESC)
           .MustExist();

        arg.Value = EXPECTED_ARG_VALUE;

        var temp = arg as DirectoryArgument ?? throw new InvalidOperationException("Wrong type");

        _SystemUnderTest = temp;

        // act
        var actual = SystemUnderTest.Validate();

        // assert        
        Assert.True(SystemUnderTest.MustExist);
        Assert.False(actual);
    }

    [Fact]
    public void IsValid_Required_MustExistIsTrue_AbsolutePath_DirDoesExist()
    {
        // arrange
        var testDir = GetTestDirectory();

        var dirName = testDir.FullName;

        var dirValue =
            Path.Combine(Environment.CurrentDirectory, dirName);

        var arg = new ArgumentCollection().AddDirectory(EXPECTED_ARG_NAME)
           .AsRequired()
           .AllowEmptyValue()
           .WithDescription(EXPECTED_ARG_DESC)
           .MustExist();

        arg.Value = EXPECTED_ARG_VALUE;

        var temp = arg as DirectoryArgument ?? throw new InvalidOperationException("Wrong type");

        _SystemUnderTest = temp;

        // act
        var actual = SystemUnderTest.Validate();

        // assert        
        Assert.True(SystemUnderTest.MustExist);
        Assert.False(actual);
    }



    private static DirectoryInfo GetTestDirectory()
    {
        var currentDir = System.IO.Directory.GetCurrentDirectory();

        var unitTestTestDataDir = Path.Combine(currentDir, "unit-test-test-data");

        if (Directory.Exists(unitTestTestDataDir) == true)
        {
            Directory.Delete(unitTestTestDataDir, true);
        }

        if (Directory.Exists(unitTestTestDataDir) == false)
        {
            Directory.CreateDirectory(unitTestTestDataDir);
        }

        return new DirectoryInfo(unitTestTestDataDir);
    }
}
