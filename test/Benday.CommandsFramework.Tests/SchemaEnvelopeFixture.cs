using System.Text.Json;

using Benday.CommandsFramework.CmdUi.Models;
using Benday.CommandsFramework.CmdUi.Services;
using Benday.CommandsFramework.Samples;

namespace Benday.CommandsFramework.Tests;

/// <summary>
/// Tests the --json envelope and the compatibility rule that goes with it. cmdui is a global
/// tool that probes whatever tools happen to be installed, so one copy has to read both the
/// 4.x schema and the 5.x schema on the same machine. The 4.x form is a bare array and the
/// 5.x form is an object, which is the entire discriminator -- no negotiation, and nothing
/// that shipped against 4.x has to change.
/// </summary>
public class SchemaEnvelopeFixture
{
    private static string GetSchemaJson()
    {
        var options = new DefaultProgramOptions
        {
            ApplicationName = "Test Sample Application",
            Version = "v1.2.3",
            ConfigurationFolderName = "TestSampleApplication-Deleteable",
            OutputProvider = new StringBuilderTextOutputProvider(),
            UsesConfiguration = false
        };

        var output = (StringBuilderTextOutputProvider)options.OutputProvider;

        var program = new DefaultProgram(options, typeof(SampleCommand1).Assembly);

        var originalExitCode = Environment.ExitCode;

        try
        {
            program.Run([ArgumentFrameworkConstants.ArgumentJson]);
        }
        finally
        {
            Environment.ExitCode = originalExitCode;
        }

        return output.GetResultOutput();
    }

    [Fact]
    public void Schema_RootIsAnObjectWithAVersion()
    {
        // act
        using var document = JsonDocument.Parse(GetSchemaJson());

        // assert
        Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
        Assert.Equal(
            CommandFrameworkConstants.CurrentSchemaVersion,
            document.RootElement.GetProperty("SchemaVersion").GetInt32());
        Assert.Equal(JsonValueKind.Array, document.RootElement.GetProperty("Commands").ValueKind);
    }

    [Fact]
    public void Schema_CarriesTheApplicationNameAndVersion()
    {
        // act
        using var document = JsonDocument.Parse(GetSchemaJson());

        // assert
        Assert.Equal(
            "Test Sample Application",
            document.RootElement.GetProperty("ApplicationName").GetString());
        Assert.Equal("v1.2.3", document.RootElement.GetProperty("ApplicationVersion").GetString());
    }

    [Fact]
    public void Schema_StillContainsEveryCommand()
    {
        // act
        using var document = JsonDocument.Parse(GetSchemaJson());

        var names = document.RootElement.GetProperty("Commands")
            .EnumerateArray()
            .Select(x => x.GetProperty("Name").GetString())
            .ToList();

        // assert
        Assert.Contains(ApplicationConstants.CommandName_Command1, names);
        Assert.Contains(ApplicationConstants.CommandName_CommandWithFileAndDirectoryArgs, names);
    }

    [Fact]
    public void Schema_IsWriteOnly()
    {
        // The schema types serialize; they do not deserialize. CommandInfo's setters are
        // internal and Arguments is a collection of an interface, so a consumer that reaches
        // for JsonSerializer.Deserialize<CommandSchema> gets objects with everything blank
        // rather than an error. This test exists so that the trap is documented and so that
        // the day someone makes the types round-trippable, it fails and gets deleted.
        var schema = JsonSerializer.Deserialize<CommandSchema>(GetSchemaJson());

        Assert.NotNull(schema);
        Assert.NotEmpty(schema.Commands);
        Assert.All(schema.Commands, x => Assert.Equal(string.Empty, x.Name));

        // read the schema the way cmdui does instead -- through mirror types
        var viaCmdUi = ToolSchemaService.ParseSchema(GetSchemaJson());

        Assert.All(viaCmdUi.Commands, x => Assert.NotEmpty(x.Name));
    }

    [Fact]
    public void CmdUi_ReadsTheNewSchema()
    {
        // act
        var document = ToolSchemaService.ParseSchema(GetSchemaJson());

        // assert
        Assert.Equal(CommandFrameworkConstants.CurrentSchemaVersion, document.SchemaVersion);
        Assert.Equal("Test Sample Application", document.ApplicationName);
        Assert.NotEmpty(document.Commands);
    }

    [Fact]
    public void CmdUi_ReadsTheOldBareArraySchema()
    {
        // arrange -- exactly what a tool built against 4.x writes
        var legacyJson = """
            [
              {
                "Name": "greet",
                "Description": "Says hello",
                "Category": "",
                "IsAsync": false,
                "Aliases": [],
                "CommandAliases": [],
                "Arguments": [
                  {
                    "AllowEmptyValue": false,
                    "DataType": "String",
                    "Description": "Name to greet",
                    "FriendlyName": "name",
                    "HasValue": false,
                    "IsRequired": true,
                    "Name": "name",
                    "Alias": "",
                    "HasAlias": false,
                    "IsPositionalSource": false,
                    "IsFromConfig": false,
                    "Value": "",
                    "DefaultValue": "",
                    "HasDefaultValue": false,
                    "AllowedValues": []
                  }
                ]
              }
            ]
            """;

        // act
        var document = ToolSchemaService.ParseSchema(legacyJson);

        // assert -- the version is inferred from the shape, since the old schema never said
        Assert.Equal(1, document.SchemaVersion);
        Assert.Single(document.Commands);
        Assert.Equal("greet", document.Commands[0].Name);
        Assert.Single(document.Commands[0].Arguments);
        Assert.Equal("name", document.Commands[0].Arguments[0].Name);
    }

    [Fact]
    public void CmdUi_SaysSoWhenTheSchemaIsNewerThanItUnderstands()
    {
        // arrange
        var futureJson = $$"""
            { "SchemaVersion": {{ToolSchemaService.HighestSupportedSchemaVersion + 1}}, "Commands": [] }
            """;

        // act & assert -- guessing at an unknown schema is worse than saying so
        var actual = Assert.Throws<InvalidOperationException>(
            () => ToolSchemaService.ParseSchema(futureJson));

        Assert.Contains("dotnet tool update", actual.Message);
    }

    [Fact]
    public void CmdUi_RejectsOutputThatIsNotASchema()
    {
        // act & assert
        Assert.Throws<InvalidOperationException>(() => ToolSchemaService.ParseSchema("\"nope\""));
    }
}
