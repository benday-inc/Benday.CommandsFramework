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

    private FileBasedConfigurationManager? _SystemUnderTest;

    private FileBasedConfigurationManager SystemUnderTest
    {
        get
        {
            if (_SystemUnderTest == null)
            {
                _SystemUnderTest = new FileBasedConfigurationManager();
            }

            return _SystemUnderTest;
        }
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
        var applicationName = "BendayCommandsFrameworkTests-Deletable";

        var expectedDir = System.IO.Path.Combine(userProfileDir, applicationName);
        var expected = System.IO.Path.Combine(expectedDir, "config.json");

        DeleteDirectory(expectedDir);

        // act
        var actual = FileBasedConfigurationManager.GetConfigurationFilePath(applicationName);

        // assert
        Assert.AreEqual<string>(expected, actual, $"Config filename was wrong");
    }

    [TestMethod]
    public void GetConfigDirPath()
    {
        // arrange
        // get path to user profile dir
        var userProfileDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var applicationName = "BendayCommandsFrameworkTests-Deletable";

        var expectedDir = System.IO.Path.Combine(userProfileDir, applicationName);

        DeleteDirectory(expectedDir);

        // act
        var actual = FileBasedConfigurationManager.GetConfigurationDirectoryPath(applicationName);

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