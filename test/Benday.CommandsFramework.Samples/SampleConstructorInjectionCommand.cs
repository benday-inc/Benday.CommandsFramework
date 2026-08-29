using Benday.CommandsFramework;
using Benday.CommandsFramework.Samples.Services;

namespace Benday.CommandsFramework.Samples;

/// <summary>
/// Sample command that takes its dependency as a constructor parameter. Commands are created
/// through ActivatorUtilities, so anything registered can be injected -- there is no need to
/// derive from a special base class or to resolve services by hand.
/// </summary>
[Command(Name = ApplicationConstants.CommandName_ConstructorInjection,
    Description = "Sample command that gets its dependency injected into its constructor.")]
public class SampleConstructorInjectionCommand : Command
{
    private readonly IGreetingService _GreetingService;

    public SampleConstructorInjectionCommand(
        CommandExecutionInfo info,
        ITextOutputProvider outputProvider,
        IGreetingService greetingService) : base(info, outputProvider)
    {
        _GreetingService = greetingService;
    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString("name").AsRequired().WithDescription("Name of the person to greet");

        return args;
    }

    /// <summary>
    /// The greeting that was produced, so a caller can read it back.
    /// </summary>
    public string Greeting { get; private set; } = string.Empty;

    protected override Task OnExecute(CancellationToken cancellationToken)
    {
        Greeting = _GreetingService.GetGreeting(Arguments.GetStringValue("name"));

        WriteLine(Greeting);

        return Task.CompletedTask;
    }
}
