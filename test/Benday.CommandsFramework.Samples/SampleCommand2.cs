﻿namespace Benday.CommandsFramework.Samples;

[Command(Name = ApplicationConstants.CommandName_Command2)]
public class SampleCommand2 : AsynchronousCommand
{
    public SampleCommand2(CommandExecutionInfo info, ITextOutputProvider outputProvider) : 
        base(info, outputProvider)
    {

    }

    protected override async Task OnExecute()
    {
        var temp = GetStringAsync().GetAwaiter();

        var value = temp.GetResult();

        _OutputProvider.WriteLine("** SUCCESS **");
        _OutputProvider.WriteLine($"isawesome:{value}:");

        await Task.CompletedTask;
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private async Task<string> GetStringAsync()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        return Arguments["isawesome"].Value.ToString();
    }

    protected override ArgumentCollection GetAvailableArguments()
    {
        var expectedArgs = new Dictionary<string, IArgument>();

        expectedArgs.Add("isawesome", new BooleanArgument("isawesome", true, true));
        
        return new(expectedArgs);
    }
}
