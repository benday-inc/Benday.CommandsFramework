using System.Diagnostics;
using System.Text.Json;
using Benday.CommandsFramework.CmdUI.Models;

namespace Benday.CommandsFramework.CmdUI.Services;

public class ToolSchemaService
{
    private readonly Dictionary<string, List<ToolCommandInfo>> _cache = new();

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

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        await Task.WhenAll(stdoutTask, stderrTask);
        await process.WaitForExitAsync();

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
