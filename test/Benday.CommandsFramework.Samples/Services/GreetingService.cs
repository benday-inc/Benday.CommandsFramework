namespace Benday.CommandsFramework.Samples.Services;

public class GreetingService : IGreetingService
{
    public string GetGreeting(string name)
    {
        return $"Hello, {name}! Welcome to CommandsFramework.";
    }
}
