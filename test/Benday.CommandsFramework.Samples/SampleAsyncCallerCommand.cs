namespace Benday.CommandsFramework.Samples;

/// <summary>
/// Sample async command that is run by another command.
/// </summary>
[Command(
    Name = ApplicationConstants.CommandName_AsyncGreeting,
    IsAsync = true,
    Description = "Async version of the greeting command")]
public class SampleAsyncGreetingCommand : AsynchronousCommand
{
    public SampleAsyncGreetingCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {

    }

    public string Greeting { get; private set; } = string.Empty;

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString("name").AsRequired().WithDescription("name of the person to greet");

        return args;
    }

    protected override Task OnExecute()
    {
        Greeting = $"Hello, {Arguments.GetStringValue("name")}!";

        WriteLine(Greeting);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Sample async command that reuses another async command in process.
/// </summary>
[Command(
    Name = ApplicationConstants.CommandName_AsyncCallsOtherCommands,
    IsAsync = true,
    Description = "Reuses the async greeting command")]
public class SampleAsyncCallerCommand : AsynchronousCommand
{
    public SampleAsyncCallerCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {

    }

    public string Greeting { get; private set; } = string.Empty;

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString("name").AsRequired().WithDescription("name of the person to greet");

        return args;
    }

    protected override async Task OnExecute()
    {
        var command = await ExecuteCommandAsync<SampleAsyncGreetingCommand>(args =>
        {
            args["name"] = Arguments.GetStringValue("name");
        });

        Greeting = command.Greeting;

        WriteLine($"** SUCCESS ** {Greeting}");
    }
}
