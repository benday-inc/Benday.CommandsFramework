using Benday.CommandsFramework.Samples.Services;

namespace Benday.CommandsFramework.Samples;

/// <summary>
/// Sample command demonstrating dependency injection through the obsolete base class. Kept
/// on DependencyInjectionCommand on purpose: it is what proves the shim still works for
/// tools that have not moved yet. New commands should derive from Command and take their
/// dependencies as constructor parameters -- see SampleConstructorInjectionCommand.
/// </summary>
[Command(Name = "greet",
    Description = "Sample command demonstrating dependency injection",
    Category = "Samples")]
#pragma warning disable CS0618 // deriving from the obsolete base class is the point here
public class SampleDICommand : DependencyInjectionCommand
#pragma warning restore CS0618
{
    public SampleDICommand(CommandExecutionInfo info, ITextOutputProvider outputProvider)
        : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        arguments.AddString("name")
            .AsRequired()
            .WithDescription("Name of the person to greet");

        return arguments;
    }

    protected override Task OnExecute(CancellationToken cancellationToken)
    {
        var name = Arguments.GetStringValue("name");

        // Get the service via DI
        var greetingService = GetRequiredService<IGreetingService>();

        var greeting = greetingService.GetGreeting(name);

        WriteLine(greeting);

        return Task.CompletedTask;
    }
}
