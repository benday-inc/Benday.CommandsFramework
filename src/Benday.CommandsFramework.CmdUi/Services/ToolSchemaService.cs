using System.Diagnostics;
using System.Text.Json;
using Benday.CommandsFramework.CmdUi.Models;

namespace Benday.CommandsFramework.CmdUi.Services;

public class ToolSchemaService
{
    private readonly Dictionary<string, List<ToolCommandInfo>> _cache = new();

    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(10);

    public async Task<List<ToolCommandInfo>> GetCommandSchemaAsync(string toolName)
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

        var commands = JsonSerializer.Deserialize<List<ToolCommandInfo>>(stdout,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? new List<ToolCommandInfo>();

        _cache[toolName] = commands;
        return commands;
    }
}
