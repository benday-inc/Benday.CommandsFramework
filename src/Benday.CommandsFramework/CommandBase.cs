using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benday.CommandsFramework;
public abstract class CommandBase
{
    private readonly CommandExecutionInfo _Info;
    private ArgumentCollection _Arguments;
    private bool _HaveValuesBeenSet = false;

    public CommandBase(CommandExecutionInfo info)
    {
        _Info = info;
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
            if (_Arguments == null)
            {
                _Arguments = GetAvailableArguments();
            }

            return _Arguments; 
        }        
    }

    protected virtual ArgumentCollection GetAvailableArguments()
    {
        return new();
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
