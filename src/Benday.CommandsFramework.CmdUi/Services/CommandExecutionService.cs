using System.Diagnostics;
using Benday.CommandsFramework.CmdUi.Models;

namespace Benday.CommandsFramework.CmdUi.Services;

public class CommandExecutionService
{
    private readonly AppState _appState;
    private readonly ToolSchemaService _schemaService;

    public CommandExecutionService(AppState appState, ToolSchemaService schemaService)
    {
        _appState = appState;
        _schemaService = schemaService;
    }

    public async Task<CommandExecutionResult> ExecuteCommandAsync(
        string toolName,
        string commandName,
        List<ToolArgumentInfo> arguments,
        Dictionary<string, string> values)
    {
        // which syntax to build depends on the tool, not on cmdui: a tool built against an
        // older framework only understands the slash form. The schema says which, and the
        // schema service has it cached from the probe that produced these arguments.
        var schema = await _schemaService.GetSchemaDocumentAsync(toolName);

        var argList = BuildArgumentList(
            commandName, arguments, values, schema.AcceptsPosixSyntax);
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

    /// <summary>
    /// An argument name as the target tool expects it to be typed.
    /// </summary>
    private static string FormatName(string name, bool posix) =>
        posix == true ? $"--{name}" : $"/{name}";

    /// <summary>
    /// An argument name and value in a single token, so it survives being passed as one
    /// element of ProcessStartInfo.ArgumentList.
    /// </summary>
    private static string FormatNameValue(string name, string value, bool posix) =>
        posix == true ? $"--{name}={value}" : $"/{name}:{value}";

    private static List<string> BuildArgumentList(
        string commandName,
        List<ToolArgumentInfo> arguments,
        Dictionary<string, string> values,
        bool posix)
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
                        result.Add(FormatName(arg.Name, posix));
                    }
                    else
                    {
                        result.Add(FormatNameValue(arg.Name, "true", posix));
                    }
                }
                else if (!arg.AllowEmptyValue && val == "false")
                {
                    result.Add(FormatNameValue(arg.Name, "false", posix));
                }
            }
            else if (!string.IsNullOrEmpty(val))
            {
                result.Add(FormatNameValue(arg.Name, val, posix));
            }
        }

        return result;
    }
}
