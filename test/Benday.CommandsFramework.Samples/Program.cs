using Benday.CommandsFramework;
using Benday.CommandsFramework.Samples;
using Benday.CommandsFramework.Samples.Services;

using Microsoft.Extensions.DependencyInjection;

namespace Benday.CommandsFramework.Samples;

class Program
{
    static void Main(string[] args)
    {
        // New simplified approach with CommandsApp builder
        CommandsApp
            .Create<SampleCommand1>(args)
            .WithAppInfo("Sample Tool using Commands Framework", "https://www.benday.com")
            .WithVersionFromAssembly()
            .ConfigureServices(services =>
            {
                // Register your services for dependency injection
                services.AddSingleton<IGreetingService, GreetingService>();
            })
            .Run();
    }
}