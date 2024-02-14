using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benday.CommandsFramework.Tests;


[TestClass]
public class FileBasedConfigurationManagerFixture
{
    [TestInitialize]
    public void OnTestInitialize()
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

    [TestMethod]
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
        Assert.IsTrue(configFileExists, $"Expected config file to exist at {expected}");
    }


    [TestMethod]
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
    
        Assert.IsFalse(configFileExists, $"Expected config file to not exist at {expected}");
    }

    [TestMethod]
    public void SetValueCreatesConfigFile()
    {
        // arrange
        var expected = FileBasedConfigurationManager.GetConfigurationFilePath(APPLICATION_NAME);
        DeleteDirectory();

        _SystemUnderTest = new FileBasedConfigurationManager(APPLICATION_NAME);

        // act
        SystemUnderTest.SetValue("testkey", "testvalue");

        // assert
        Assert.IsTrue(SystemUnderTest.ConfigFileExists(),
            $"config file should exist at {expected}");
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

    [TestMethod]
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
        Assert.AreEqual<string>(expected, actual, $"Config filename was wrong");
    }

    [TestMethod]
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
        Assert.AreEqual<string>(expectedDir, actual, $"Config dir path was wrong");
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void GetConfigFilePath_ThrowsOnInvalidAppName()
    {
        // arrange
        // get path to user profile dir
        var applicationName = string.Empty;

        // act
        var actual = FileBasedConfigurationManager.GetConfigurationFilePath(applicationName);

        // assert
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void GetConfigDirPath_ThrowsOnInvalidAppName()
    {
        // arrange
        // get path to user profile dir
        var applicationName = string.Empty;

        // act
        var actual = FileBasedConfigurationManager.GetConfigurationDirectoryPath(applicationName);

        // assert
    }


}