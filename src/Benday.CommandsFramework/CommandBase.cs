using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benday.CommandsFramework;

/// <summary>
/// Base class for all command implementations
/// </summary>
public abstract class CommandBase
{
    private readonly CommandExecutionInfo _Info;
    protected readonly ITextOutputProvider _OutputProvider;
    private ArgumentCollection _Arguments;
    private bool _HaveValuesBeenSet = false;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="info">Execution information for the requested command</param>
    /// <param name="outputProvider">Output provider</param>
    /// <exception cref="ArgumentNullException"></exception>
    public CommandBase(CommandExecutionInfo info, ITextOutputProvider outputProvider)
    {
        if (outputProvider is null)
        {
            throw new ArgumentNullException(nameof(outputProvider));
        }

        _Info = info ?? throw new ArgumentNullException(nameof(info));
        _OutputProvider = outputProvider;
        _Arguments = GetArguments();
    }

    /// <summary>
    /// Property for accessing the raw execution info for the command
    /// </summary>
    public CommandExecutionInfo ExecutionInfo
    {
        get
        {
            return _Info;
        }
    }

    /// <summary>
    /// Arguments and values for the command
    /// </summary>
    protected ArgumentCollection Arguments 
    { 
        get
        {
            return _Arguments; 
        }        
    }

    /// <summary>
    /// Get the arguments for the command execution
    /// </summary>
    /// <returns></returns>
    public virtual ArgumentCollection GetArguments()
    {
        return new();
    }

    /// <summary>
    /// Human readable description of this command
    /// </summary>
    public string Description
    {
        get
        {
            var attribute = 
                Attribute.GetCustomAttribute(GetType(), typeof(CommandAttribute)) as CommandAttribute;

            if (attribute != null)
            {
                return attribute.Description;
            }
            else
            {
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// Write a message to the output provider
    /// </summary>
    /// <param name="text">Message to write</param>
    protected virtual void WriteLine(string text)
    {
        _OutputProvider.WriteLine(text);
    }

    /// <summary>
    /// Write a new line to the output provider
    /// </summary>    
    protected virtual void WriteLine()
    {
        _OutputProvider.WriteLine();
    }

    /// <summary>
    /// Displays the command usage description
    /// </summary>
    protected virtual void DisplayUsage()
    {
        var builder = new StringBuilder();

        DisplayUsage(builder);

        _OutputProvider.WriteLine(builder.ToString());
    }

    private string GetKeyString(IArgument arg)
    {
        if (arg.IsPositionalSource == true)
        {
            if (arg.IsRequired == true)
            {
                return $"{{{arg.Name}:{arg.DataType}}}";
            }
            else
            {
                return $"[{{{arg.Name}:{arg.DataType}}}]";
            }
        }
        else
        {
            if (arg.IsRequired == true)
            {
                return $"/{arg.Name}:{arg.DataType}";
            }
            else
            {
                return $"[/{arg.Name}:{arg.DataType}]";
            }
        }
        
    }

    /// <summary>
    /// Adds the command usage description to the provided string builder
    /// </summary>
    /// <param name="builder">StringBuilder instance</param>
    protected void DisplayUsage(StringBuilder builder)
    {
        builder.AppendLine($"Command name: {ExecutionInfo.CommandName}");

        if (string.IsNullOrWhiteSpace(Description) == false)
        {
            builder.AppendLine(Description);
        }

        builder.AppendLine("** USAGE **");
        builder.AppendLine(ExecutionInfo.CommandName);

        int longestNameLength;

        if (Arguments.Count < 1)
        {
            longestNameLength = 0;
        }
        else
        {
            longestNameLength = Arguments.Keys.Max(x =>
                    {
                        return GetKeyString(Arguments[x]).Length;
                    });
        }

        int consoleWidth; 
        
        if (Console.IsOutputRedirected == true)
        {
            consoleWidth = 60;
        }
        else
        {
            consoleWidth = Console.WindowWidth;
        }        
        
        var separator = " - ";
        int argNameColumnWidth = (longestNameLength + separator.Length);

        var positionalArgs = new List<IArgument>();
        
        foreach (var key in Arguments.Keys)
        {
            var arg = Arguments[key];
            
            if (arg.IsPositionalSource == true)
            {
                positionalArgs.Add(arg);
                continue;
            }
            else if (arg.Name == arg.Description || string.IsNullOrEmpty(arg.Description) == true)
            {
                // description has an empty value or the value is the same as the arg name
                builder.AppendWithPadding(GetKeyString(arg), longestNameLength);
                builder.AppendLine();
            }
            else
            {
                // description has an actual value

                var paddedKeyString = LineWrapUtilities.GetValueWithPadding(
                    GetKeyString(arg), longestNameLength);

                builder.Append(paddedKeyString);
                builder.Append(separator);
                builder.AppendWrappedValue(arg.Description,
                    consoleWidth, argNameColumnWidth);

                builder.AppendLine();
            }
        }

        if (positionalArgs.Count > 0)
        {
            var argsSortedByPosition = positionalArgs.OrderBy(a => a.Alias);

            foreach (var arg in argsSortedByPosition)
            {
                if (arg.Name == arg.Description || string.IsNullOrEmpty(arg.Description) == true)
                {
                    // description has an empty value or the value is the same as the arg name
                    builder.AppendWithPadding(GetKeyString(arg), longestNameLength);
                }
                else
                {
                    // description has an actual value

                    var paddedKeyString = LineWrapUtilities.GetValueWithPadding(
                        GetKeyString(arg), longestNameLength);

                    builder.Append(paddedKeyString);
                    builder.Append(separator);
                    builder.AppendWrappedValue(arg.Description,
                        consoleWidth, argNameColumnWidth);

                    builder.AppendLine();
                }
            }
        }
    }

    /// <summary>
    /// Creates and displays the validation summary when there are failed argument validations
    /// </summary>
    /// <param name="invalidArguments">Collection of invalid arguments</param>
    protected virtual void DisplayValidationSummary(List<IArgument> invalidArguments)
    {
        if (invalidArguments.Count == 1)
        {
            _OutputProvider.WriteLine("** INVALID ARGUMENT **");
        }
        else if (invalidArguments.Count > 1)
        {
            _OutputProvider.WriteLine("** INVALID ARGUMENTS **");
        }

        foreach (var item in invalidArguments)
        {
            _OutputProvider.WriteLine($"{item.Name} is not valid or missing");
        }
    }

    /// <summary>
    /// Validate the arguments provided using the required argument definition for the
    /// command.
    /// </summary>
    /// <returns>List of invalid arguments</returns>
    protected virtual List<IArgument> Validate()
    {
        var returnValue = new List<IArgument>();

        SetValuesFromExecutionInfo();

        foreach (var key in Arguments.Keys)
        {
            var temp = Arguments[key];

            if (temp != null)
            {
                var result = temp.Validate();

                if (result == false)
                    returnValue.Add(temp);
            }
        }

        if (returnValue.Count > 0)
        {
            Environment.ExitCode = 1;
        }

        return returnValue;
    }

    /// <summary>
    /// Reads the arguments from the execution info and 
    /// sets the values on to the argument definitions for the command.
    /// </summary>
    protected virtual void SetValuesFromExecutionInfo()
    {
        if (_HaveValuesBeenSet == false)
        {
            Arguments.SetValues(_Info.Arguments);
            _HaveValuesBeenSet = true;
        }
    }
}
