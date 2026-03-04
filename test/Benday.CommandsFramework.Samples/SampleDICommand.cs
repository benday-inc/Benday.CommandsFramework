using Benday.CommandsFramework.Samples.Services;

namespace Benday.CommandsFramework.Samples;

[Command(Name = "greet",
    Description = "Sample command demonstrating dependency injection",
    Category = "Samples",
    IsAsync = true)]
public class SampleDICommand : DependencyInjectionCommand
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

    protected override Task OnExecute()
    {
        var name = Arguments.GetStringValue("name");

        // Get the service via DI
        var greetingService = GetRequiredService<IGreetingService>();

        var greeting = greetingService.GetGreeting(name);

        WriteLine(greeting);

        return Task.CompletedTask;
    }
}
