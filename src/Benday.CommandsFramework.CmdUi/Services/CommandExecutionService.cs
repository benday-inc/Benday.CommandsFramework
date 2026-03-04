using System.Diagnostics;
using Benday.CommandsFramework.CmdUi.Models;

namespace Benday.CommandsFramework.CmdUi.Services;

public class CommandExecutionService
{
    private readonly AppState _appState;

    public CommandExecutionService(AppState appState)
    {
        _appState = appState;
    }

    public async Task<CommandExecutionResult> ExecuteCommandAsync(
        string toolName,
        string commandName,
        List<ToolArgumentInfo> arguments,
        Dictionary<string, string> values)
    {
        var argList = BuildArgumentList(commandName, arguments, values);
        var commandLine = $"{toolName} {string.Join(" ", argList)}";

        var psi = new ProcessStartInfo
        {
            FileName = toolName,
            WorkingDirectory = _appState.WorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in argList)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        await Task.WhenAll(stdoutTask, stderrTask);
        await process.WaitForExitAsync();

        return new CommandExecutionResult
        {
            StandardOutput = await stdoutTask,
            StandardError = await stderrTask,
            ExitCode = process.ExitCode,
            CommandLine = commandLine,
            ExecutedAt = DateTime.Now
        };
    }

    private static List<string> BuildArgumentList(
        string commandName,
        List<ToolArgumentInfo> arguments,
        Dictionary<string, string> values)
    {
        var result = new List<string> { commandName };

        // Positional arguments first, sorted by alias (POSITION_1, POSITION_2, etc.)
        var positionalArgs = arguments
            .Where(a => a.IsPositionalSource)
            .OrderBy(a => a.Alias)
            .ToList();

        foreach (var arg in positionalArgs)
        {
            if (values.TryGetValue(arg.Name, out var val) && !string.IsNullOrEmpty(val))
            {
                result.Add(val);
            }
        }

        // Named arguments
        foreach (var arg in arguments.Where(a => !a.IsPositionalSource))
        {
            if (!values.TryGetValue(arg.Name, out var val))
            {
                continue;
            }

            if (arg.DataType == "Boolean")
            {
                if (val == "true")
                {
                    if (arg.AllowEmptyValue)
                    {
                        // Flag-style: presence means true
                        result.Add($"/{arg.Name}");
                    }
                    else
                    {
                        result.Add($"/{arg.Name}:true");
                    }
                }
                else if (!arg.AllowEmptyValue && val == "false")
                {
                    result.Add($"/{arg.Name}:false");
                }
            }
            else if (!string.IsNullOrEmpty(val))
            {
                result.Add($"/{arg.Name}:{val}");
            }
        }

        return result;
    }
}
