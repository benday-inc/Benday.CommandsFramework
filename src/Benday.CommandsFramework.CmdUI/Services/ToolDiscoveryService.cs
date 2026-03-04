using System.Diagnostics;
using Benday.CommandsFramework.CmdUI.Models;

namespace Benday.CommandsFramework.CmdUI.Services;

public class ToolDiscoveryService
{
    private readonly ToolSchemaService _schemaService;

    public ToolDiscoveryService(ToolSchemaService schemaService)
    {
        _schemaService = schemaService;
    }

    public async Task<List<ToolInfo>> DiscoverToolsAsync()
    {
        var tools = new List<ToolInfo>();

        var toolNames = await GetInstalledToolsAsync();

        foreach (var (name, version) in toolNames)
        {
            var tool = new ToolInfo
            {
                ToolName = name,
                Version = version
            };

            try
            {
                tool.Commands = await _schemaService.GetCommandSchemaAsync(name);
                tool.SupportsJson = true;
            }
            catch
            {
                tool.SupportsJson = false;
            }

            tools.Add(tool);
        }

        return tools;
    }

    private static async Task<List<(string Name, string Version)>> GetInstalledToolsAsync()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "tool list --global",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdout = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        var results = new List<(string, string)>();
        var lines = stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Skip header lines (Package Id, separator line)
        foreach (var line in lines.Skip(2))
        {
            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
            {
                // Column 0 = package id, column 1 = version, column 2 = commands
                var commandName = parts[2].Trim();
                var version = parts[1].Trim();
                results.Add((commandName, version));
            }
        }

        return results;
    }
}
