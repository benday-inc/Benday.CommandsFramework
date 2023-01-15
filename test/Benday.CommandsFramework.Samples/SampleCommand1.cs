using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benday.CommandsFramework.Samples;

[Command(Name = ApplicationConstants.CommandName_Command1)]
public class SampleCommand1 : CommandBase
{
	public SampleCommand1(CommandExecutionInfo info) : base(info)
	{

	}
}
