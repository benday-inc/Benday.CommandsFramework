using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benday.CommandsFramework;

public abstract class CommandBase
{
    private readonly CommandExecutionInfo _Info;
    protected readonly ITextOutputProvider _OutputProvider;
    private ArgumentCollection _Arguments;
    private bool _HaveValuesBeenSet = false;

    public CommandBase(CommandExecutionInfo info, ITextOutputProvider outputProvider)
    {
        if (outputProvider is null)
        {
            throw new ArgumentNullException(nameof(outputProvider));
        }

        _Info = info ?? throw new ArgumentNullException(nameof(info));
        _OutputProvider = outputProvider;
        _Arguments = GetAvailableArguments();
    }

    public CommandExecutionInfo ExecutionInfo
    {
        get
        {
            return _Info;
        }
    }

    protected ArgumentCollection Arguments 
    { 
        get
        {
            return _Arguments; 
        }        
    }

    protected virtual ArgumentCollection GetAvailableArguments()
    {
        return new();
    }

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

    protected virtual void DisplayUsage()
    {
        var builder = new StringBuilder();

        builder.AppendLine($"Command name: {ExecutionInfo.CommandName}");

        if (string.IsNullOrWhiteSpace(Description) == false)
        {
            builder.AppendLine(Description);
        }

        builder.AppendLine("** USAGE **");
        builder.AppendLine(ExecutionInfo.CommandName);

        foreach (var key in Arguments.Keys)
        {
            var temp = Arguments[key];
            if (temp.Name == temp.Description || string.IsNullOrEmpty(temp.Description) == true)
            {
                // description has an empty value or the value is the same as the arg name

                if (temp.IsRequired == true)
                {
                    builder.AppendLine($"/{key}:{temp.DataType}");
                }
                else
                {
                    builder.AppendLine($"[/{key}:{temp.DataType}]");
                }
            }
            else
            {
                // description has an actual value

                if (temp.IsRequired == true)
                {
                    builder.AppendLine($"/{key}:{temp.DataType} -- {temp.Description}");
                }
                else
                {
                    builder.AppendLine($"[/{key}:{temp.DataType}] -- {temp.Description}");
                }
            }
        }

        _OutputProvider.WriteLine(builder.ToString());
    }

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

        return returnValue;
    }

    protected virtual void SetValuesFromExecutionInfo()
    {
        if (_HaveValuesBeenSet == false)
        {
            Arguments.SetValues(_Info.Arguments);
            _HaveValuesBeenSet = true;
        }
    }
}
