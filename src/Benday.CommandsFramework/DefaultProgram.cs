using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Benday.CommandsFramework;

/// <summary>
/// This class provides a default implementation of a program that uses the CommandsFramework. There are options for 
/// supplying the application name, version, and website. Customize these options in order to control how
/// the list of available commands and usage information is displayed.
/// </summary>
public class DefaultProgram : ICommandProgram
{
    public ICommandProgramOptions Options { get; private set; }
    public Assembly ImplementationAssembly { get; }
    public ITextOutputProvider OutputProvider { get; private set; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="options">Display options such as application name, version, and website</param>
    /// <param name="implementationAssembly">Assembly that contains your command implementations</param>
    public DefaultProgram(DefaultProgramOptions options, Assembly implementationAssembly)
    {
        Options = options;
        ImplementationAssembly = implementationAssembly;
        OutputProvider = options.OutputProvider;
    }

    private void WriteLine(string message)
    {
        OutputProvider.WriteLine(message);
    }

    private void Write(string message)
    {
        OutputProvider.Write(message);
    }

    private void WriteLine()
    {
        OutputProvider.WriteLine();
    }

    /// <summary>
    /// Write a line of commentary about the work to the diagnostic channel.
    /// </summary>
    private void WriteStatus(string message)
    {
        OutputProvider.WriteStatus(message);
    }

    /// <summary>
    /// Write an error message to the diagnostic channel. A failure has to stay out of the
    /// command's result, or a failed command piping --json to a file lands its error text
    /// inside the JSON.
    /// </summary>
    private void WriteError(string message)
    {
        OutputProvider.WriteError(message);
    }

    /// <summary>
    /// Runs whatever the command line asked for and reports how it went.
    /// </summary>
    /// <remarks>
    /// This returns the exit code rather than assigning Environment.ExitCode. Setting the
    /// process exit code is a console application's decision, and this class is also what a
    /// long lived host runs commands through -- there, one command's failure has no business
    /// deciding the fate of the process. CommandsApp is the console entry point and is where
    /// the exit code gets applied.
    /// </remarks>
    /// <param name="args">Command line arguments</param>
    /// <param name="cancellationToken">Cancels the command being run</param>
    /// <returns>The exit code the process should use if it is exiting</returns>
    public async Task<int> RunAsync(
        string[] args, CancellationToken cancellationToken = default)
    {
        var util = new CommandAttributeUtility(Options);

        if (args.Length == 0)
        {
            DisplayUsage(util);

            // no command was named, so nothing was run
            return CommandFrameworkConstants.ExitCode_Failure;
        }

        try
        {
            if (args[0] == ArgumentFrameworkConstants.ArgumentJson)
            {
                DumpJson(util);

                return CommandFrameworkConstants.ExitCode_Success;
            }

            if (args[0] == ArgumentFrameworkConstants.ArgumentGui)
            {
                LaunchGui();

                return CommandFrameworkConstants.ExitCode_Success;
            }

            if (args[0] == ArgumentFrameworkConstants.ArgumentHelpString)
            {
                DisplayUsage(util);

                return CommandFrameworkConstants.ExitCode_Success;
            }

            // one lookup. The built-in configuration commands are ordinary registrations in
            // the registry, so there is no assembly to route to and no UsesConfiguration
            // branch here -- that used to be decided three separate times, once here, once
            // again below, and once inside GetCommand().
            var registration = util.GetRegistry(ImplementationAssembly).Find(args[0]);

            if (registration is null)
            {
                throw new KnownException($"Invalid command name '{args[0]}'.");
            }

            var command = util.GetCommand(args, ImplementationAssembly);

            if (command is null)
            {
                DisplayUsage(util);

                return CommandFrameworkConstants.ExitCode_Failure;
            }

            // one base class, so there is nothing to branch on. This used to be about forty
            // lines that told a synchronous command from an asynchronous one by reading a
            // flag off the attribute -- a flag that could disagree with the class it was on.
            if (command is not Command runThis)
            {
                throw new InvalidOperationException(
                    $"Command '{registration.Name}' does not derive from {nameof(Command)}.");
            }

            var result = await runThis.ExecuteAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(result.Message) == false &&
                result.Status == CommandExecutionStatus.Failed)
            {
                WriteError(result.Message);
            }

            return result.ExitCode;
        }
        catch (KnownException ex)
        {
            WriteError(ex.Message);

            return CommandFrameworkConstants.ExitCode_Failure;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            WriteError("Cancelled.");

            return CommandFrameworkConstants.ExitCode_Failure;
        }
    }

    private void LaunchGui()
    {
        // https://www.nuget.org/packages/Benday.CommandsFramework.CmdUi

        if (!IsCmdUiInstalled())
        {
            WriteLine("The 'cmdui' tool is not installed.");
            WriteLine("cmdui provides a web-based UI for this command-line tool.");
            WriteLine();
            WriteLine("Here's the NuGet package URL: https://www.nuget.org/packages/Benday.CommandsFramework.CmdUi");
            WriteLine();
            Write("Would you like to install it now? (Y/n): ");

            var response = Options.InputProvider.ReadLine()?.Trim().ToLowerInvariant();

            if (string.IsNullOrEmpty(response) || response == "y" || response == "yes")
            {
                WriteLine();
                WriteLine("Installing cmdui...");

                if (!InstallCmdUi())
                {
                    throw new KnownException(
                        "Failed to install cmdui. You can install it manually with: dotnet tool install -g Benday.CommandsFramework.CmdUi");
                }

                WriteLine("cmdui installed successfully.");
                WriteLine();
            }
            else
            {
                WriteLine();
                WriteLine("You can install cmdui later with: dotnet tool install -g Benday.CommandsFramework.CmdUi");
                return;
            }
        }

        var toolName = Process.GetCurrentProcess().ProcessName;

        WriteLine($"Launching cmdui for '{toolName}'...");

        var psi = new ProcessStartInfo
        {
            FileName = "cmdui",
            ArgumentList = { toolName },
            UseShellExecute = false
        };

        try
        {
            using var process = Process.Start(psi);

            if (process != null)
            {
                process.WaitForExit();
            }
        }
        catch (Exception ex)
        {
            throw new KnownException(
                $"Failed to launch cmdui. Error: {ex.Message}");
        }
    }

    private bool IsCmdUiInstalled()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "tool list -g",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);

            if (process == null)
            {
                return false;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Check if cmdui appears in the tool list
            return output.Contains("cmdui", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private bool InstallCmdUi()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "tool install -g Benday.CommandsFramework.CmdUi",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi);

            if (process == null)
            {
                return false;
            }

            // Show output to user
            while (!process.StandardOutput.EndOfStream)
            {
                var line = process.StandardOutput.ReadLine();
                if (!string.IsNullOrEmpty(line))
                {
                    WriteLine(line);
                }
            }

            process.WaitForExit();

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private void DumpJson(CommandAttributeUtility util)
    {
        var schema = new CommandSchema
        {
            ApplicationName = Options.ApplicationName,
            ApplicationVersion = Options.Version,
            Commands = util.GetAllCommandUsages(ImplementationAssembly)
        };

        var json = JsonSerializer.Serialize(schema, new JsonSerializerOptions()
        {
            WriteIndented = true
        });

        WriteLine(json);
    }

    /// <summary>
    /// Displays the usage information for the program and all available commands.
    /// </summary>
    /// <param name="util"></param>
    public virtual void DisplayUsage(CommandAttributeUtility util)
    {
        // a value that was never configured prints as a blank line, which is most of the
        // usage header when a tool is bootstrapped with CommandsApp.RunAsync(args) and has
        // no website in its assembly metadata
        if (Options.DisplayUsageOptions.ShowApplicationName &&
            string.IsNullOrWhiteSpace(Options.ApplicationName) == false)
        {
            WriteLine($"{Options.ApplicationName}");
        }

        if (Options.DisplayUsageOptions.ShowWebsite &&
            string.IsNullOrWhiteSpace(Options.Website) == false)
        {
            WriteLine($"{Options.Website}");
        }

        if (Options.DisplayUsageOptions.ShowVersion &&
            string.IsNullOrWhiteSpace(Options.Version) == false)
        {
            WriteLine($"{Options.Version}");
        }

        if (Options.DisplayUsageOptions.NewLineAfterHeaderInfo)
        {
            WriteLine();
        }

        WriteLine($"Available commands:");

        var commands = util.GetAvailableCommandAttributes(ImplementationAssembly);

        if (Options.DisplayUsageOptions.ShowCategories)
        {
            DisplayCommandsWithCategories(commands);
        }
        else
        {
            DisplayCommandsWithoutCategories(commands);
        }

        DisplayCommandAliases(util.GetCommandAliases(ImplementationAssembly));

        DisplayReservedKeywords();
    }

    /// <summary>
    /// Displays the names the framework reserves for itself. They are not commands and they
    /// are not any command's arguments, so nothing else in the usage output mentions them.
    /// </summary>
    public virtual void DisplayReservedKeywords()
    {
        var keywords = ReservedKeywords.ForPrograms;

        if (keywords.Count == 0)
        {
            return;
        }

        var separator = " - ";
        var longestName = keywords.Max(x => x.Name.Length);
        var nameColumnWidth = longestName + separator.Length;
        var consoleWidth = GetConsoleWidth();

        WriteLine();
        WriteLine("Also available:");

        var builder = new StringBuilder();

        foreach (var keyword in keywords)
        {
            builder.Clear();
            builder.Append(LineWrapUtilities.GetValueWithPadding(keyword.Name, longestName));
            builder.Append(separator);
            builder.AppendWrappedValue(keyword.Description, consoleWidth, nameColumnWidth);

            WriteLine(builder.ToString());
        }
    }

    /// <summary>
    /// Displays the aliases that supply argument values. Plain aliases are not listed here
    /// because they are already shown next to the command name they belong to.
    /// </summary>
    /// <param name="aliases">All of the aliases for the available commands</param>
    public virtual void DisplayCommandAliases(List<CommandAliasInfo> aliases)
    {
        var aliasesWithArguments = aliases.Where(x => x.HasArguments).OrderBy(x => x.Alias).ToList();

        if (aliasesWithArguments.Count == 0)
        {
            return;
        }

        WriteLine();
        WriteLine("Command aliases:");

        var longestName = aliasesWithArguments.Max(x => x.Alias.Length);

        var consoleWidth = GetConsoleWidth();
        var separator = " - ";
        int aliasNameColumnWidth = (longestName + separator.Length);

        foreach (var alias in aliasesWithArguments)
        {
            Write(LineWrapUtilities.GetValueWithPadding(alias.Alias, longestName));
            Write(separator);

            var argumentSummary = string.Join(" ",
                alias.Arguments.Select(x =>
                    string.IsNullOrEmpty(x.Value) ? $"/{x.Key}" : $"/{x.Key}:{x.Value}"));

            var description = string.IsNullOrWhiteSpace(alias.Description)
                ? $"{alias.CommandName} {argumentSummary}"
                : $"{alias.Description} ({alias.CommandName} {argumentSummary})";

            WriteLine(
                LineWrapUtilities.WrapValue(aliasNameColumnWidth, consoleWidth, description));
        }
    }

    /// <summary>
    /// Displays the usage information for the program and all available commands displayed alphabetically.
    /// </summary>
    /// <param name="commands">List of commands</param>
    public virtual void DisplayCommandsWithoutCategories(List<CommandAttribute> commands)
    {
        var longestName = commands.Max(x => GetCommandDisplayName(x).Length);

        var consoleWidth = GetConsoleWidth();
        var separator = " - ";
        int commandNameColumnWidth = (longestName + separator.Length);

        foreach (var command in commands.OrderBy(x => x.Name))
        {
            Write(LineWrapUtilities.GetValueWithPadding(GetCommandDisplayName(command), longestName));
            Write(separator);

            WriteLine(
                LineWrapUtilities.WrapValue(commandNameColumnWidth,
                consoleWidth, command.Description));
        }
    }

    /// <summary>
    /// Gets the name to show for a command in the list of available commands. Commands
    /// that have aliases are shown as 'name (alias1, alias2)'.
    /// </summary>
    /// <param name="command">Command attribute</param>
    /// <returns>Display name for the command</returns>
    protected static string GetCommandDisplayName(CommandAttribute command)
    {
        if (command.Aliases.Length == 0)
        {
            return command.Name;
        }

        return $"{command.Name} ({string.Join(", ", command.Aliases)})";
    }

    private int GetConsoleWidth()
    {
        if (Console.IsOutputRedirected == true)
        {
            return 80;
        }
        else
        {
            return Console.WindowWidth;
        }
    }

    /// <summary>
    /// Displays the usage information for the program and all available commands displayed by category.
    /// </summary>
    /// <param name="commands">List of commands</param>
    public virtual void DisplayCommandsWithCategories(List<CommandAttribute> commands)
    {
        var categories = commands.Select(x => x.Category).Distinct().Order();

        var longestName = commands.Max(x => GetCommandDisplayName(x).Length);

        var consoleWidth = GetConsoleWidth();
        var separator = " - ";
        int commandNameColumnWidth = (longestName + separator.Length);

        foreach (var category in categories)
        {
            WriteLine($"* {category} *");
            WriteLine();

            foreach (var command in commands.Where(x => x.Category == category).OrderBy(x => x.Name))
            {
                Write(LineWrapUtilities.GetValueWithPadding(GetCommandDisplayName(command), longestName));
                Write(separator);

                WriteLine(
                    LineWrapUtilities.WrapValue(commandNameColumnWidth,
                    consoleWidth, command.Description));
            }

            WriteLine();
        }
    }
}