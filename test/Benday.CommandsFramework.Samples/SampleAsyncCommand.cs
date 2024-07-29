namespace Benday.CommandsFramework.Samples;

[Command(Name = ApplicationConstants.CommandName_Command2)]
public class SampleAsyncCommand : AsynchronousCommand
{
    public SampleAsyncCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) : 
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

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddBoolean("isawesome").AsRequired().AllowEmptyValue();

        return args;
    }
}
