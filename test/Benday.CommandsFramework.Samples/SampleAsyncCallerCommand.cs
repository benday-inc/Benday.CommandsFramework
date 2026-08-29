namespace Benday.CommandsFramework.Samples;

/// <summary>
/// Sample async command that is run by another command.
/// </summary>
[Command(
    Name = ApplicationConstants.CommandName_AsyncGreeting,
    Description = "Async version of the greeting command")]
public class SampleAsyncGreetingCommand : Command
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

    protected override Task OnExecute(CancellationToken cancellationToken)
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
    Description = "Reuses the async greeting command")]
public class SampleAsyncCallerCommand : Command
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

    protected override async Task OnExecute(CancellationToken cancellationToken)
    {
        var command = await ExecuteCommandAsync<SampleAsyncGreetingCommand>(
            args => args.Set("name", Arguments.GetStringValue("name")),
            cancellationToken: cancellationToken);

        Greeting = command.Greeting;

        WriteLine($"** SUCCESS ** {Greeting}");
    }
}
