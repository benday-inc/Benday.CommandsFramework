using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Benday.CommandsFramework.CmdUi;
using Benday.CommandsFramework.CmdUi.Services;

// Handle cmdui's own flags before treating args as a tool name
if (args.Length > 0 && args[0] is "--help" or "-h" or "--version")
{
    Console.WriteLine("cmdui - Blazor Server web UI for CommandsFramework CLI tools");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  cmdui <toolname>    Launch UI for a specific tool");
    Console.WriteLine("  cmdui              Discover all installed CommandsFramework tools");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  cmdui slnutil");
    Console.WriteLine("  cmdui azdoutil");
    return;
}

// First non-flag argument is the tool name; ignore anything starting with --
var toolName = args.Length > 0 && !args[0].StartsWith('-') ? args[0] : null;
var port = PortFinder.GetAvailablePort();
var url = $"http://localhost:{port}";

// When installed as a dotnet tool, wwwroot is next to the assembly.
// In dev mode (dotnet run), static web assets are resolved via manifest.
var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
var publishedWebRoot = Path.Combine(assemblyDir, "wwwroot");

WebApplicationBuilder builder;

if (Directory.Exists(publishedWebRoot))
{
    // Published / installed as tool — serve from the physical wwwroot
    builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
        Args = Array.Empty<string>(),
        ContentRootPath = assemblyDir,
        WebRootPath = publishedWebRoot
    });
}
else
{
    // Dev mode — let the SDK resolve static web assets via manifest
    builder = WebApplication.CreateBuilder(Array.Empty<string>());
}

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

app.MapRazorComponents<Benday.CommandsFramework.CmdUi.Components.App>()
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
