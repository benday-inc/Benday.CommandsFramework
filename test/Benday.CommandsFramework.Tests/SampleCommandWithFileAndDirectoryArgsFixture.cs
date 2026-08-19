using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

/// <summary>
/// End to end coverage for file and directory arguments. Until this sample existed, the
/// only coverage for AddFile() and AddDirectory() was on the argument classes themselves,
/// so nothing exercised them through command validation or the --json schema -- the two
/// paths that cmdui depends on.
/// </summary>
public class SampleCommandWithFileAndDirectoryArgsFixture : IDisposable
{
    private readonly string _TempDirectory;
    private readonly string _ExistingFile;

    public SampleCommandWithFileAndDirectoryArgsFixture()
    {
        _TempDirectory = Path.Combine(
            Path.GetTempPath(), $"filesanddirs-{Guid.NewGuid():N}");

        Directory.CreateDirectory(_TempDirectory);

        _ExistingFile = Path.Combine(_TempDirectory, "input.txt");

        File.WriteAllText(_ExistingFile, "sample");
    }

    public void Dispose()
    {
        if (Directory.Exists(_TempDirectory) == true)
        {
            Directory.Delete(_TempDirectory, true);
        }

        GC.SuppressFinalize(this);
    }

    private StringBuilderTextOutputProvider? _OutputProvider;

    private StringBuilderTextOutputProvider OutputProvider
    {
        get
        {
            _OutputProvider ??= new StringBuilderTextOutputProvider();

            return _OutputProvider;
        }
    }

    private string Run(params string[] args)
    {
        var executionInfo = new ArgumentCollectionFactory().Parse(
            Utilities.GetStringArray(
                new[] { ApplicationConstants.CommandName_CommandWithFileAndDirectoryArgs }
                    .Concat(args).ToArray()));

        var command = new SampleCommandWithFileAndDirectoryArgs(executionInfo, OutputProvider);

        command.Execute();

        return OutputProvider.GetOutput();
    }

    [Fact]
    public void Execute_ExistingFileAndDirectory_Succeeds()
    {
        // act
        var output = Run(
            $"/{SampleCommandWithFileAndDirectoryArgs.ArgumentName_InputFile}:{_ExistingFile}",
            $"/{SampleCommandWithFileAndDirectoryArgs.ArgumentName_OutputDirectory}:{_TempDirectory}");

        // assert
        Assert.Contains("** SUCCESS **", output);
        Assert.Contains(_ExistingFile, output);
        Assert.Contains(_TempDirectory, output);
    }

    [Fact]
    public void Execute_MissingFile_FailsValidation()
    {
        // arrange
        var missingFile = Path.Combine(_TempDirectory, "nope.txt");

        // act
        var output = Run(
            $"/{SampleCommandWithFileAndDirectoryArgs.ArgumentName_InputFile}:{missingFile}",
            $"/{SampleCommandWithFileAndDirectoryArgs.ArgumentName_OutputDirectory}:{_TempDirectory}");

        // assert -- MustExist() is the whole difference between a file argument and a
        // string argument, and this is where it shows up
        Assert.DoesNotContain("** SUCCESS **", output);
        Assert.Contains("** INVALID ARGUMENT **", output);
        Assert.Contains(SampleCommandWithFileAndDirectoryArgs.ArgumentName_InputFile, output);
    }

    [Fact]
    public void Execute_MissingDirectory_FailsValidation()
    {
        // arrange
        var missingDirectory = Path.Combine(_TempDirectory, "nope");

        // act
        var output = Run(
            $"/{SampleCommandWithFileAndDirectoryArgs.ArgumentName_InputFile}:{_ExistingFile}",
            $"/{SampleCommandWithFileAndDirectoryArgs.ArgumentName_OutputDirectory}:{missingDirectory}");

        // assert
        Assert.DoesNotContain("** SUCCESS **", output);
        Assert.Contains("** INVALID ARGUMENT **", output);
        Assert.Contains(SampleCommandWithFileAndDirectoryArgs.ArgumentName_OutputDirectory, output);
    }

    [Fact]
    public void Execute_OptionalPathsThatDoNotExist_Succeeds()
    {
        // arrange -- ExistenceOptional() means the value is a destination, not a source
        var newFile = Path.Combine(_TempDirectory, "output.txt");
        var newDirectory = Path.Combine(_TempDirectory, "temp");

        // act
        var output = Run(
            $"/{SampleCommandWithFileAndDirectoryArgs.ArgumentName_InputFile}:{_ExistingFile}",
            $"/{SampleCommandWithFileAndDirectoryArgs.ArgumentName_OutputDirectory}:{_TempDirectory}",
            $"/{SampleCommandWithFileAndDirectoryArgs.ArgumentName_OptionalFile}:{newFile}",
            $"/{SampleCommandWithFileAndDirectoryArgs.ArgumentName_OptionalDirectory}:{newDirectory}");

        // assert
        Assert.Contains("** SUCCESS **", output);
        Assert.Contains(newFile, output);
        Assert.Contains(newDirectory, output);
    }

    [Fact]
    public void Arguments_AreFileAndDirectoryArguments()
    {
        // arrange
        var executionInfo = new ArgumentCollectionFactory().Parse(
            Utilities.GetStringArray(
                ApplicationConstants.CommandName_CommandWithFileAndDirectoryArgs));

        var command = new SampleCommandWithFileAndDirectoryArgs(executionInfo, OutputProvider);

        // act
        var args = command.GetArguments();

        // assert
        var inputFile = Assert.IsType<FileArgument>(
            args[SampleCommandWithFileAndDirectoryArgs.ArgumentName_InputFile]);
        var outputDirectory = Assert.IsType<DirectoryArgument>(
            args[SampleCommandWithFileAndDirectoryArgs.ArgumentName_OutputDirectory]);

        Assert.True(inputFile.MustExist);
        Assert.True(outputDirectory.MustExist);

        var optionalFile = Assert.IsType<FileArgument>(
            args[SampleCommandWithFileAndDirectoryArgs.ArgumentName_OptionalFile]);
        var optionalDirectory = Assert.IsType<DirectoryArgument>(
            args[SampleCommandWithFileAndDirectoryArgs.ArgumentName_OptionalDirectory]);

        Assert.False(optionalFile.MustExist);
        Assert.False(optionalDirectory.MustExist);
    }
}
