using System.Diagnostics;
using System.Runtime.InteropServices;
using Benday.CommandsFramework.CmdUI;
using Benday.CommandsFramework.CmdUI.Services;

var toolName = args.Length > 0 ? args[0] : null;
var port = PortFinder.GetAvailablePort();
var url = $"http://localhost:{port}";

var builder = WebApplication.CreateBuilder(Array.Empty<string>());

builder.WebHost.UseUrls(url);
builder.Logging.SetMinimumLevel(LogLevel.Warning);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<ToolSchemaService>();
builder.Services.AddSingleton<CommandExecutionService>();
builder.Services.AddSingleton<ToolDiscoveryService>();
builder.Services.AddSingleton(new AppState { ToolName = toolName });

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<Benday.CommandsFramework.CmdUI.Components.App>()
    .AddInteractiveServerRenderMode();

_ = Task.Run(async () =>
{
    await Task.Delay(1500);
    OpenBrowser(url);
});

Console.WriteLine($"cmdui running at {url}");
if (toolName != null)
{
    Console.WriteLine($"Tool: {toolName}");
}
else
{
    Console.WriteLine("Discovery mode: scanning installed dotnet tools...");
}
Console.WriteLine("Press Ctrl+C to stop.");

await app.RunAsync();

static void OpenBrowser(string url)
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    {
        Process.Start("open", url);
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
        Process.Start("xdg-open", url);
    }
}
