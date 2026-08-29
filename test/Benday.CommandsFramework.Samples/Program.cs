using Benday.CommandsFramework;
using Benday.CommandsFramework.Samples;
using Benday.CommandsFramework.Samples.Services;
using Benday.CommandsFramework.Tui;

using Microsoft.Extensions.DependencyInjection;

namespace Benday.CommandsFramework.Samples;

class Program
{
    // returning the exit code from Main is what makes it the process exit code -- the
    // framework hands it back rather than assigning Environment.ExitCode itself
    static async Task<int> Main(string[] args)
    {
        return await CommandsApp
            .Create<SampleCommand1>(args)
            .WithAppInfo("Sample Tool using Commands Framework", "https://www.benday.com")
            .WithVersionFromAssembly()
            // one line, and the tool gains a 'tui' keyword
            .WithTui()
            .ConfigureServices(services =>
            {
                // Register your services for dependency injection
                services.AddSingleton<IGreetingService, GreetingService>();
            })
            .RunAsync();
    }
}