using System.Diagnostics;
using System.Text.Json;
using Benday.CommandsFramework.CmdUi.Models;

namespace Benday.CommandsFramework.CmdUi.Services;

public class ToolSchemaService
{
    private readonly Dictionary<string, ToolSchemaDocument> _cache = new();

    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(10);

    private static readonly JsonSerializerOptions SchemaSerializerOptions =
        new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Highest schema version this build of cmdui understands.
    /// </summary>
    public const int HighestSupportedSchemaVersion = 2;

    public async Task<List<ToolCommandInfo>> GetCommandSchemaAsync(string toolName)
    {
        var document = await GetSchemaDocumentAsync(toolName);

        return document.Commands;
    }

    public async Task<ToolSchemaDocument> GetSchemaDocumentAsync(string toolName)
    {
        if (_cache.TryGetValue(toolName, out var cached))
        {
            return cached;
        }

        var psi = new ProcessStartInfo
        {
            FileName = toolName,
            Arguments = "--json",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        using var cts = new CancellationTokenSource(ProbeTimeout);

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cts.Token);
        var stderrTask = process.StandardError.ReadToEndAsync(cts.Token);

        try
        {
            await Task.WhenAll(stdoutTask, stderrTask);
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(); } catch { }
            throw new InvalidOperationException(
                $"Timed out waiting for '{toolName} --json' after {ProbeTimeout.TotalSeconds}s.");
        }

        var stdout = await stdoutTask;

        if (process.ExitCode != 0)
        {
            var stderr = await stderrTask;
            throw new InvalidOperationException(
                $"Failed to get schema from '{toolName} --json'. Exit code: {process.ExitCode}. Error: {stderr}");
        }

        var document = ParseSchema(stdout);

        _cache[toolName] = document;
        return document;
    }

    /// <summary>
    /// Reads a tool's --json output in whichever shape it arrived in.
    /// </summary>
    /// <remarks>
    /// The 4.x schema is a bare array of commands and the 5.x schema is an object with the
    /// commands inside it, so the root JSON token says which one this is. That is the whole
    /// discriminator -- there is no negotiation and nothing to ask the tool.
    /// </remarks>
    /// <param name="json">Raw --json output</param>
    /// <returns>The parsed schema</returns>
    /// <exception cref="InvalidOperationException">Thrown when the output is not a schema at
    /// all, or is a newer schema version than this build understands.</exception>
    public static ToolSchemaDocument ParseSchema(string json)
    {
        using var parsed = JsonDocument.Parse(json);

        if (parsed.RootElement.ValueKind == JsonValueKind.Array)
        {
            // pre-5.0 tool: a bare array, with nothing identifying it
            return new ToolSchemaDocument
            {
                SchemaVersion = 1,
                Commands = parsed.RootElement.Deserialize<List<ToolCommandInfo>>(
                    SchemaSerializerOptions) ?? new List<ToolCommandInfo>()
            };
        }

        if (parsed.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                $"Expected the schema to be an object or an array but it was " +
                $"{parsed.RootElement.ValueKind}.");
        }

        var document = parsed.RootElement.Deserialize<ToolSchemaDocument>(
            SchemaSerializerOptions) ?? new ToolSchemaDocument();

        if (document.SchemaVersion > HighestSupportedSchemaVersion)
        {
            throw new InvalidOperationException(
                $"This tool reports schema version {document.SchemaVersion}, and this " +
                $"version of cmdui only understands up to {HighestSupportedSchemaVersion}. " +
                "Update cmdui with: dotnet tool update -g Benday.CommandsFramework.CmdUi");
        }

        return document;
    }
}
