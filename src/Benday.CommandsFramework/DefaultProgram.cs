using System.Reflection;

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

    public void Run(string[] args)
    {
        var util = new CommandAttributeUtility(Options);

        if (args.Length == 0)
        {
            DisplayUsage(util);
        }
        else
        {
            try
            {
                var names = util.GetAvailableCommandNames(ImplementationAssembly);

                if (names.Contains(args[0]) == false)
                {
                    throw new KnownException(
                            $"Invalid command name '{args[0]}'.");
                }



                CommandBase? command;

                if (Options.UsesConfiguration == false)
                {
                    command = util.GetCommand(args, ImplementationAssembly);
                }
                else
                {
                    if (IsDefaultCommandName(args[0]) == true)
                    {
                        command = util.GetCommand(args, this.GetType().Assembly);
                    }
                    else
                    {
                        command = util.GetCommand(args, ImplementationAssembly);
                    }
                }

                if (command == null)
                {
                    DisplayUsage(util);
                }
                else
                {
                    CommandAttribute? attr;

                    if (Options.UsesConfiguration == false)
                    {
                        attr = util.GetCommandAttributeForCommandName(ImplementationAssembly,
                                                command.ExecutionInfo.CommandName);
                    }
                    else
                    {
                        if (IsDefaultCommandName(command.ExecutionInfo.CommandName) == true)
                        {
                            attr = util.GetCommandAttributeForCommandName(this.GetType().Assembly,
                                                command.ExecutionInfo.CommandName);
                        }
                        else
                        {
                            attr = util.GetCommandAttributeForCommandName(ImplementationAssembly,
                                                command.ExecutionInfo.CommandName);
                        }
                    }

                    if (attr == null)
                    {
                        throw new KnownException(
                            $"Invalid command name '{command.ExecutionInfo.CommandName}'.");
                    }
                    else
                    {
                        if (attr.IsAsync == false)
                        {
                            var runThis = command as ISynchronousCommand;

                            if (runThis == null)
                            {
                                throw new InvalidOperationException($"Could not convert type to {typeof(ISynchronousCommand)}.");
                            }
                            else
                            {
                                runThis.Execute();
                            }
                        }
                        else
                        {
                            var runThis = command as IAsyncCommand;

                            if (runThis == null)
                            {
                                throw new InvalidOperationException($"Could not convert type to {typeof(IAsyncCommand)}.");
                            }
                            else
                            {
                                var temp = runThis.ExecuteAsync().GetAwaiter();

                                temp.GetResult();
                            }
                        }
                    }
                }
            }
            catch (KnownException ex)
            {
                WriteLine(ex.Message);
                Environment.ExitCode = 1;
            }
            catch
            {
                Environment.ExitCode = 1;
                throw;
            }
        }
    }
    private bool IsDefaultCommandName(string commandName)
    {
        var commandNames = new string[]
        {
            CommandFrameworkConstants.CommandName_GetConfig,
            CommandFrameworkConstants.CommandName_SetConfig,
            CommandFrameworkConstants.CommandName_RemoveConfig
        };

        return commandNames.Contains(commandName);
    }

    /// <summary>
    /// Displays the usage information for the program and all available commands.
    /// </summary>
    /// <param name="util"></param>
    public virtual void DisplayUsage(CommandAttributeUtility util)
    {
        if (Options.DisplayUsageOptions.ShowApplicationName)
        {
            WriteLine($"{Options.ApplicationName}");
        }

        if (Options.DisplayUsageOptions.ShowWebsite)
        {
            WriteLine($"{Options.Website}");
        }

        if (Options.DisplayUsageOptions.ShowVersion)
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

        Environment.ExitCode = 1;
    }

    /// <summary>
    /// Displays the usage information for the program and all available commands displayed alphabetically.
    /// </summary>
    /// <param name="commands">List of commands</param>
    public virtual void DisplayCommandsWithoutCategories(List<CommandAttribute> commands)
    {
        var longestName = commands.Max(x => x.Name.Length);

        var consoleWidth = GetConsoleWidth();
        var separator = " - ";
        int commandNameColumnWidth = (longestName + separator.Length);

        foreach (var command in commands.OrderBy(x => x.Name))
        {
            Write(LineWrapUtilities.GetValueWithPadding(command.Name, longestName));
            Write(separator);

            WriteLine(
                LineWrapUtilities.WrapValue(commandNameColumnWidth,
                consoleWidth, command.Description));
        }
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

        var longestName = commands.Max(x => x.Name.Length);
        
        var consoleWidth = GetConsoleWidth();
        var separator = " - ";
        int commandNameColumnWidth = (longestName + separator.Length);

        foreach (var category in categories)
        {
            WriteLine($"* {category} *");
            WriteLine();

            foreach (var command in commands.Where(x => x.Category == category).OrderBy(x => x.Name))
            {
                Write(LineWrapUtilities.GetValueWithPadding(command.Name, longestName));
                Write(separator);

                WriteLine(
                    LineWrapUtilities.WrapValue(commandNameColumnWidth,
                    consoleWidth, command.Description));
            }

            WriteLine();
        }
    }
}