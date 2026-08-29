using System.Text.Json;

using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

/// <summary>
/// Tests that a file or directory argument is distinguishable from a plain string
/// argument in the --json schema. Before PathType and MustExist existed on IArgument,
/// AddFile("target").MustExist() and AddString("target") serialized to byte-identical
/// JSON while validating differently on the same input, so cmdui and anything else
/// reading the schema had no way to tell them apart.
/// </summary>
public class ArgumentPathTypeSchemaFixture
{
    private const string MissingPath = "no-such-thing-12345";

    private static string Serialize(IArgument argument)
    {
        return JsonSerializer.Serialize(argument, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private static IArgument GetArgument(ArgumentCollection args, string name)
    {
        return args[name];
    }

    [Fact]
    public void FileArgument_DoesNotSerializeTheSameAsAStringArgument()
    {
        // arrange
        var files = new ArgumentCollection();
        files.AddFile("target").MustExist().AsRequired();
        files["target"].TrySetValue(MissingPath);

        var strings = new ArgumentCollection();
        strings.AddString("target").AsRequired();
        strings["target"].TrySetValue(MissingPath);

        // act
        var fileJson = Serialize(GetArgument(files, "target"));
        var stringJson = Serialize(GetArgument(strings, "target"));

        // assert -- they validate differently on the same value, so they cannot look
        // the same in the schema
        Assert.False(files["target"].Validate());
        Assert.True(strings["target"].Validate());
        Assert.NotEqual(stringJson, fileJson);
    }

    [Fact]
    public void PathType_ReportedThroughIArgument()
    {
        // arrange
        var args = new ArgumentCollection();
        args.AddFile("file");
        args.AddDirectory("directory");
        args.AddString("text");
        args.AddInt32("number");
        args.AddBoolean("flag");
        args.AddDateTime("asof");

        // assert
        Assert.Equal(ArgumentPathType.File, GetArgument(args, "file").PathType);
        Assert.Equal(ArgumentPathType.Directory, GetArgument(args, "directory").PathType);
        Assert.Equal(ArgumentPathType.None, GetArgument(args, "text").PathType);
        Assert.Equal(ArgumentPathType.None, GetArgument(args, "number").PathType);
        Assert.Equal(ArgumentPathType.None, GetArgument(args, "flag").PathType);
        Assert.Equal(ArgumentPathType.None, GetArgument(args, "asof").PathType);
    }

    [Fact]
    public void MustExist_ReportedThroughIArgument()
    {
        // arrange
        var args = new ArgumentCollection();
        args.AddFile("requiredfile").MustExist();
        args.AddFile("anyfile").ExistenceOptional();
        args.AddDirectory("requireddir").MustExist();
        args.AddDirectory("anydir").ExistenceOptional();
        args.AddString("text");

        // assert
        Assert.True(GetArgument(args, "requiredfile").MustExist);
        Assert.False(GetArgument(args, "anyfile").MustExist);
        Assert.True(GetArgument(args, "requireddir").MustExist);
        Assert.False(GetArgument(args, "anydir").MustExist);
        Assert.False(GetArgument(args, "text").MustExist);
    }

    [Fact]
    public void DataType_IsUnchangedForFileAndDirectoryArguments()
    {
        // arrange -- the distinction is carried by PathType rather than by widening
        // ArgumentDataType, so nothing that switches on DataType has to change
        var args = new ArgumentCollection();
        args.AddFile("file");
        args.AddDirectory("directory");

        // assert
        Assert.Equal(ArgumentDataType.String, GetArgument(args, "file").DataType);
        Assert.Equal(ArgumentDataType.String, GetArgument(args, "directory").DataType);
    }

    [Fact]
    public void SchemaForTheSampleCommand_CarriesPathTypeAndMustExist()
    {
        // arrange -- this is the whole --json path, the one cmdui reads
        var options = new DefaultProgramOptions
        {
            ApplicationName = "Test Sample Application",
            ConfigurationFolderName = "TestSampleApplication-Deleteable",
            UsesConfiguration = false
        };

        var utility = new CommandAttributeUtility(options);

        var usages = utility.GetAllCommandUsages(
            typeof(SampleCommandWithFileAndDirectoryArgs).Assembly);

        var json = JsonSerializer.Serialize(usages, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        using var document = JsonDocument.Parse(json);

        var command = document.RootElement.EnumerateArray().Single(
            x => x.GetProperty("Name").GetString() ==
                ApplicationConstants.CommandName_CommandWithFileAndDirectoryArgs);

        var arguments = command.GetProperty("Arguments")
            .EnumerateArray()
            .ToDictionary(x => x.GetProperty("Name").GetString()!);

        // assert
        var inputFile = arguments[SampleCommandWithFileAndDirectoryArgs.ArgumentName_InputFile];

        Assert.Equal("File", inputFile.GetProperty("PathType").GetString());
        Assert.True(inputFile.GetProperty("MustExist").GetBoolean());

        var optionalFile = arguments[SampleCommandWithFileAndDirectoryArgs.ArgumentName_OptionalFile];

        Assert.Equal("File", optionalFile.GetProperty("PathType").GetString());
        Assert.False(optionalFile.GetProperty("MustExist").GetBoolean());

        var outputDirectory = arguments[SampleCommandWithFileAndDirectoryArgs.ArgumentName_OutputDirectory];

        Assert.Equal("Directory", outputDirectory.GetProperty("PathType").GetString());
        Assert.True(outputDirectory.GetProperty("MustExist").GetBoolean());

        var optionalDirectory = arguments[SampleCommandWithFileAndDirectoryArgs.ArgumentName_OptionalDirectory];

        Assert.Equal("Directory", optionalDirectory.GetProperty("PathType").GetString());
        Assert.False(optionalDirectory.GetProperty("MustExist").GetBoolean());
    }

    [Fact]
    public void SchemaForANonPathArgument_ReportsNone()
    {
        // arrange
        var args = new ArgumentCollection();
        args.AddString("text");

        // act
        var json = Serialize(GetArgument(args, "text"));

        using var document = JsonDocument.Parse(json);

        // assert
        Assert.Equal("None", document.RootElement.GetProperty("PathType").GetString());
        Assert.False(document.RootElement.GetProperty("MustExist").GetBoolean());
    }
}
