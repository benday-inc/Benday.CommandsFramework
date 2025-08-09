using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benday.CommandsFramework.Tests;


public class FileBasedConfigurationManagerFixture
{
        public FileBasedConfigurationManagerFixture()
    {
        _SystemUnderTest = null;
    }

    private const string APPLICATION_NAME = "BendayCommandsFrameworkTests-Deletable";

    private FileBasedConfigurationManager? _SystemUnderTest;

    private FileBasedConfigurationManager SystemUnderTest
    {
        get
        {
            if (_SystemUnderTest == null)
            {
                Assert.Fail("sut not initialized");
            }

            return _SystemUnderTest;
        }
    }

    [Fact]
    public void ConfigFileExists_True()
    {
        // arrange
        var applicationName = APPLICATION_NAME;
        var expected = FileBasedConfigurationManager.GetConfigurationFilePath(applicationName);

        // if file doesn't exist, create it
        if (System.IO.File.Exists(expected) == false)
        {
            // create dir if it doesn't exist
            var dir = FileBasedConfigurationManager.GetConfigurationDirectoryPath(applicationName);
            if (System.IO.Directory.Exists(dir) == false)
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            System.IO.File.WriteAllText(expected, "test");
        }

        _SystemUnderTest = new FileBasedConfigurationManager(applicationName);

        // act
        var configFileExists = SystemUnderTest.ConfigFileExists();

        // assert
        Assert.True(configFileExists);
    }


    [Fact]
    public void ConfigFileExists_False()
    {
        // arrange
        var applicationName = "BendayCommandsFrameworkTests-Deletable";
        var expected = FileBasedConfigurationManager.GetConfigurationFilePath(applicationName);
        DeleteDirectory();

        _SystemUnderTest = new FileBasedConfigurationManager(applicationName);

        // act
        var configFileExists = SystemUnderTest.ConfigFileExists();

        // assert
    
        Assert.False(configFileExists);
    }

    [Fact]
    public void SetValueCreatesConfigFile()
    {
        // arrange
        DeleteDirectory();

        _SystemUnderTest = new FileBasedConfigurationManager(APPLICATION_NAME);

        // act
        SystemUnderTest.SetValue("testkey", "testvalue");

        // assert
        Assert.True(SystemUnderTest.ConfigFileExists());
    }

    [Fact]
    public void SetValue_NewValue_ReadItBack()
    {
        // arrange
        DeleteDirectory();

        _SystemUnderTest = new FileBasedConfigurationManager(APPLICATION_NAME);
        var expectedKey = "testkey";
        var expectedValue = "testvalue";

        // act
        SystemUnderTest.SetValue(expectedKey, expectedValue);

        // assert
        var reloaded = new FileBasedConfigurationManager(APPLICATION_NAME);
        
        var actual = reloaded.GetValue(expectedKey);

        Assert.Equal(expectedValue, actual);
    }

    [Fact]
    public void SetValue_ExistingValue_ReadItBack()
    {
        // arrange
        DeleteDirectory();

        _SystemUnderTest = new FileBasedConfigurationManager(APPLICATION_NAME);
        var expectedKey = "testkey";
        var expectedValueBefore = "testvalue-before";
        var expectedValueAfter = "testvalue-after";

        SystemUnderTest.SetValue(expectedKey, expectedValueBefore);

        // act
        SystemUnderTest.SetValue(expectedKey, expectedValueAfter);

        // assert
        var reloaded = new FileBasedConfigurationManager(APPLICATION_NAME);

        var actual = reloaded.GetValue(expectedKey);

        Assert.Equal(expectedValueAfter, actual);
    }

    [Fact]
    public void RemoveValue_ExistingValue_DeletesIt()
    {
        // arrange
        DeleteDirectory();

        _SystemUnderTest = new FileBasedConfigurationManager(APPLICATION_NAME);
        var expectedKey = "testkey";
        var expectedValue = "testvalue";

        SystemUnderTest.SetValue(expectedKey, expectedValue);

        // act
        SystemUnderTest.RemoveValue(expectedKey);

        // assert
        var reloaded = new FileBasedConfigurationManager(APPLICATION_NAME);

        var actual = reloaded.HasValue(expectedKey);

        Assert.False(actual);
    }

    [Fact]
    public void RemoveValue_NonExistingValue_DoesNothing()
    {
        // arrange
        DeleteDirectory();

        _SystemUnderTest = new FileBasedConfigurationManager(APPLICATION_NAME);
        var expectedKey = "testkey";
        
        // act
        SystemUnderTest.RemoveValue(expectedKey);

        // assert
        var reloaded = new FileBasedConfigurationManager(APPLICATION_NAME);

        var actual = reloaded.HasValue(expectedKey);

        Assert.False(actual);
    }

    [Fact]
    public void GetValue_ThatDoesNotExist_ReturnsStringEmpty()
    {
        // arrange
        DeleteDirectory();

        _SystemUnderTest = new FileBasedConfigurationManager(APPLICATION_NAME);
        var expectedKey = "testkey";
        var expectedValue = string.Empty;

        // act
        var actual = SystemUnderTest.GetValue(expectedKey);

        // assert
        Assert.Equal(expectedValue, actual);
    }

    [Fact]
    public void GetValues_ReturnsAllValues()
    {
        // arrange
        DeleteDirectory();

        _SystemUnderTest = new FileBasedConfigurationManager(APPLICATION_NAME);
        
        SystemUnderTest.SetValue("testkey1", "testvalue1");
        SystemUnderTest.SetValue("testkey2", "testvalue2");
        SystemUnderTest.SetValue("testkey3", "testvalue3"); 

        // act
        var actual = SystemUnderTest.GetValues();

        // assert
        Assert.Equal(3, actual.Count);
    }

    [Fact]
    public void HasValue_ValueExists_True()
    {
        // arrange
        DeleteDirectory();

        _SystemUnderTest = new FileBasedConfigurationManager(APPLICATION_NAME);
        var expectedKey = "testkey";
        var expectedValue = "testvalue";

        SystemUnderTest.SetValue(expectedKey, expectedValue);

        // act
        var actual = SystemUnderTest.HasValue(expectedKey);

        // assert
        Assert.True(actual);
    }

    [Fact]
    public void HasValue_ValueDoesNotExist_False()
    {
        // arrange
        DeleteDirectory();

        _SystemUnderTest = new FileBasedConfigurationManager(APPLICATION_NAME);
        var expectedKey = "testkey";
        
        // act
        var actual = SystemUnderTest.HasValue(expectedKey);

        // assert
        Assert.False(actual);
    }

    private void DeleteDirectory()
    {
        DeleteDirectory(FileBasedConfigurationManager.GetConfigurationDirectoryPath(APPLICATION_NAME));
    }

    private void DeleteDirectory(string expectedDir)
    {
        // if directory exists, delete it and its contents
        if (System.IO.Directory.Exists(expectedDir) == true)
        {
            System.IO.Directory.Delete(expectedDir, true);
        }

        // if directory still exists, throw exception
        if (System.IO.Directory.Exists(expectedDir) == true)
        {
            Assert.Fail($"Directory '{expectedDir}' still exists after delete attempt.");
        }
    }

    [Fact]
    public void GetConfigFilePath()
    {
        // arrange
        // get path to user profile dir
        var userProfileDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        
        var expectedDir = System.IO.Path.Combine(userProfileDir, APPLICATION_NAME);
        var expected = System.IO.Path.Combine(expectedDir, "config.json");

        DeleteDirectory(expectedDir);

        // act
        var actual = FileBasedConfigurationManager.GetConfigurationFilePath(APPLICATION_NAME);

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetConfigDirPath()
    {
        // arrange
        // get path to user profile dir
        var userProfileDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        
        var expectedDir = System.IO.Path.Combine(userProfileDir, APPLICATION_NAME);

        DeleteDirectory(expectedDir);

        // act
        var actual = FileBasedConfigurationManager.GetConfigurationDirectoryPath(APPLICATION_NAME);

        // assert
        Assert.Equal(expectedDir, actual);
    }

    [Fact]
    public void GetConfigFilePath_ThrowsOnInvalidAppName()
    {
        // arrange
        // get path to user profile dir
        var applicationName = string.Empty;

        // act & assert
        Assert.Throws<ArgumentException>(() => {
            var actual = FileBasedConfigurationManager.GetConfigurationFilePath(applicationName);
        });
    }

    [Fact]
    public void GetConfigDirPath_ThrowsOnInvalidAppName()
    {
        // arrange
        // get path to user profile dir
        var applicationName = string.Empty;

        // act & assert
        Assert.Throws<ArgumentException>(() => {
            var actual = FileBasedConfigurationManager.GetConfigurationDirectoryPath(applicationName);
        });
    }


}