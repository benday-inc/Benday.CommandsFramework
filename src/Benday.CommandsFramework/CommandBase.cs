using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benday.CommandsFramework;
public abstract class CommandBase
{
    private readonly CommandExecutionInfo _Info;

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

    protected virtual List<IArgument> GetAvailableArguments()
    {
        return new List<IArgument>();
    }
}
