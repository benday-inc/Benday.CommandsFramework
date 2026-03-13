# Benday.CommandsFramework

A .NET framework for building command-line interface (CLI) utilities. Define named commands with typed, validated arguments using a fluent API, and wire up dependency injection and configuration with minimal boilerplate.

## About

Written by Benjamin Day<br>
Pluralsight Author | Microsoft MVP | Scrum.org Professional Scrum Trainer<br>
https://www.benday.com  
https://www.honestcheetah.com  
info@benday.com

*Got ideas for features you'd like to see? Found a bug?
Let us know by submitting an [issue](https://github.com/benday-inc/Benday.CommandsFramework/issues)*. *Want to contribute? Submit a pull request.*

[Source code](https://github.com/benday-inc/Benday.CommandsFramework)  
[API Documentation](https://benday-inc.github.io/Benday.CommandsFramework/api/Benday.CommandsFramework.html)  
[NuGet Package](https://www.nuget.org/packages/Benday.CommandsFramework/)

## Features

- Named commands with descriptions and categories
- Typed arguments: `String`, `Boolean`, `Int32`, `DateTime`, `File`, `Directory`
- Fluent argument definition API with required/optional, default values, and allowed values
- Automatic argument parsing and validation
- Built-in `--help` usage display
- Dependency injection support
- Configuration from JSON files, environment variables, and custom sources
- Arguments that pull values from configuration via `FromConfig()`
- Async command support
- `--json` schema output for tooling integration
- `gui` command to launch a web UI via [Benday.CommandsFramework.CmdUi](https://www.nuget.org/packages/Benday.CommandsFramework.CmdUi/)

## Table of Contents

- [Installation](#installation)
- [Getting Started](#getting-started)
  - [1. Create a Command](#1-create-a-command)
  - [2. Set Up Program.cs](#2-set-up-programcs)
  - [3. Run It](#3-run-it)
- [Argument Types](#argument-types)
- [Configuration](#configuration)
  - [JSON Files and Environment Variables](#json-files-and-environment-variables)
  - [Custom Configuration Sources](#custom-configuration-sources)
  - [Config-Backed Arguments](#config-backed-arguments)
- [Dependency Injection](#dependency-injection)
- [Async Commands](#async-commands)
- [Data Formatting Utilities](#data-formatting-utilities)
  - [TableFormatter](#tableformatter)
  - [CsvReader](#csvreader)
  - [CsvWriter](#csvwriter)
- [CommandsApp Builder Reference](#commandsapp-builder-reference)
- [Built-in Keywords](#built-in-keywords)
- [About](#about)

## Installation

```bash
dotnet add package Benday.CommandsFramework
```

## Getting Started

### 1. Create a Command

Commands inherit from `SynchronousCommand`, `AsynchronousCommand`, or `DependencyInjectionCommand`. Use the `[Command]` attribute to define the command name and description.

```csharp
using Benday.CommandsFramework;

[Command(Name = "greet",
    Description = "Says hello to someone",
    Category = "Demo")]
public class GreetCommand : SynchronousCommand
{
    public GreetCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider)
        : base(info, outputProvider) { }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString("name").AsRequired().WithDescription("Name of the person to greet");
        args.AddBoolean("loud").AsNotRequired().AllowEmptyValue().WithDescription("Greet loudly");

        return args;
    }

    protected override void OnExecute()
    {
        var name = Arguments.GetStringValue("name");
        var loud = Arguments.GetBooleanValue("loud");

        var message = $"Hello, {name}!";
        WriteLine(loud ? message.ToUpper() : message);
    }
}
```

### 2. Set Up Program.cs

Use the `CommandsApp` builder to configure and run your CLI app. Pass any command type from your assembly to `Create<T>()` — the framework discovers all `[Command]`-attributed classes in that assembly.

```csharp
using Benday.CommandsFramework;

CommandsApp
    .Create<GreetCommand>(args)
    .WithAppInfo("My CLI Tool", "https://www.example.com")
    .WithVersionFromAssembly()
    .Run();
```

### 3. Run It

```bash
dotnet run -- greet /name:World
# Output: Hello, World!

dotnet run -- greet /name:World /loud
# Output: HELLO, WORLD!

dotnet run -- greet --help
# Output: Usage information for the greet command
```

## Argument Types

Define arguments using the fluent API in `GetArguments()`:

```csharp
public override ArgumentCollection GetArguments()
{
    var args = new ArgumentCollection();

    args.AddString("name").AsRequired().WithDescription("Your name");
    args.AddInt32("count").AsRequired().WithDescription("Number of times");
    args.AddBoolean("verbose").AsNotRequired().AllowEmptyValue();
    args.AddDateTime("start-date").AsRequired().WithDescription("Start date");
    args.AddFile("input").AsRequired().WithDescription("Input file path");
    args.AddDirectory("output-dir").AsNotRequired().WithDescription("Output directory");

    // Restrict to specific values
    args.AddString("format").AsRequired().WithAllowedValues("json", "xml", "csv");

    // Set a default value
    args.AddString("env").AsNotRequired().WithDefaultValue("production");

    return args;
}
```

Arguments are passed on the command line using `/name:value` syntax. Boolean flags with `AllowEmptyValue()` can be passed as just `/name` (presence means `true`).

## Configuration

### JSON Files and Environment Variables

```csharp
CommandsApp
    .Create<MyCommand>(args)
    .WithAppSettings()                              // loads appsettings.json + env vars
    .WithConfigFile("appsettings.local.json", optional: true)  // additional JSON file
    .WithEnvironmentVariables()                     // add env vars explicitly
    .Run();
```

### Custom Configuration Sources

Use `ConfigureConfiguration()` to add any configuration source supported by `IConfigurationBuilder`, such as in-memory collections:

```csharp
CommandsApp
    .Create<MyCommand>(args)
    .WithAppSettings()
    .ConfigureConfiguration(config =>
    {
        config.AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string?>("MySection:MyKey", "MyValue")
        });
    })
    .Run();
```

### Config-Backed Arguments

Arguments can pull their values from configuration using `FromConfig()`. Command-line values take precedence over config values.

```csharp
public override ArgumentCollection GetArguments()
{
    var args = new ArgumentCollection();

    args.AddString("api-key")
        .FromConfig()
        .AsRequired()
        .WithDescription("Your API key");

    args.AddString("base-url")
        .FromConfig()
        .AsNotRequired()
        .WithDefaultValue("https://api.example.com")
        .WithDescription("API base URL");

    return args;
}
```

## Dependency Injection

Register services with `ConfigureServices()` and use them in commands that inherit from `DependencyInjectionCommand`:

```csharp
// Program.cs
CommandsApp
    .Create<GreetCommand>(args)
    .WithAppInfo("My Tool", "https://www.example.com")
    .ConfigureServices(services =>
    {
        services.AddSingleton<IGreetingService, GreetingService>();
    })
    .Run();

// Or with access to configuration:
CommandsApp
    .Create<GreetCommand>(args)
    .WithAppSettings()
    .ConfigureServices((services, config) =>
    {
        services.Configure<MyOptions>(config.GetSection("MyOptions"));
        services.AddSingleton<IGreetingService, GreetingService>();
    })
    .Run();
```

```csharp
// Command using DI
[Command(Name = "greet", Description = "Greet with DI", IsAsync = true)]
public class GreetCommand : DependencyInjectionCommand
{
    public GreetCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider)
        : base(info, outputProvider) { }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();
        args.AddString("name").AsRequired().WithDescription("Name to greet");
        return args;
    }

    protected override Task OnExecute()
    {
        var service = GetRequiredService<IGreetingService>();
        WriteLine(service.GetGreeting(Arguments.GetStringValue("name")));
        return Task.CompletedTask;
    }
}
```

## Async Commands

For commands that need async operations, inherit from `AsynchronousCommand`:

```csharp
[Command(Name = "fetch", Description = "Fetch data from API", IsAsync = true)]
public class FetchCommand : AsynchronousCommand
{
    public FetchCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider)
        : base(info, outputProvider) { }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();
        args.AddString("url").AsRequired().WithDescription("URL to fetch");
        return args;
    }

    protected override async Task OnExecute()
    {
        var url = Arguments.GetStringValue("url");
        // async work here
        await Task.CompletedTask;
    }
}
```

## CommandsApp Builder Reference

| Method | Description |
|--------|-------------|
| `Create<TCommand>(args)` | Create builder, discover commands from the assembly containing `TCommand` |
| `Create(args, assembly)` | Create builder with explicit assembly |
| `WithAppInfo(name, website)` | Set application name and website |
| `WithAppInfo(name, version, website)` | Set application name, version, and website |
| `WithVersion(version)` | Set version string |
| `WithVersionFromAssembly()` | Auto-detect version from assembly file version |
| `WithAppSettings(optional)` | Load `appsettings.json` and environment variables |
| `WithConfigFile(filename, optional)` | Load an additional JSON config file |
| `WithEnvironmentVariables()` | Add environment variables to configuration |
| `ConfigureConfiguration(action)` | Direct access to `IConfigurationBuilder` for custom sources |
| `ConfigureServices(action)` | Register services for dependency injection |
| `ConfigureServices(action<services, config>)` | Register services with access to `IConfiguration` |
| `ConfigureOptions(action)` | Configure `DefaultProgramOptions` directly |
| `ConfigureUsageDisplay(action)` | Configure how usage/help is displayed |
| `UsesConfiguration(bool)` | Enable/disable built-in configuration storage |
| `Run()` | Build and run the application |
| `RunAsync()` | Build and run the application asynchronously |

## Data Formatting Utilities

The framework includes utility classes in `Benday.CommandsFramework.DataFormatting` for working with tabular and CSV data inside your commands.

### TableFormatter

Format data as aligned, column-padded tables for console output. Supports optional row filtering.

```csharp
using Benday.CommandsFramework.DataFormatting;

var formatter = new TableFormatter();

formatter.AddColumn("Name");
formatter.AddColumn("Role");
formatter.AddColumn("Location");

formatter.AddData("Alice", "Developer", "Seattle");
formatter.AddData("Bob", "Designer", "Portland");
formatter.AddData("Carol", "Manager", "Denver");

WriteLine(formatter.FormatTable());
```

Output:
```
Name  Role      Location
Alice Developer Seattle
Bob   Designer  Portland
Carol Manager   Denver
```

Use `AddDataWithFilter()` to only include rows where any column value contains a search string (case-insensitive):

```csharp
formatter.AddDataWithFilter("port", "Bob", "Designer", "Portland");   // included
formatter.AddDataWithFilter("port", "Alice", "Developer", "Seattle"); // excluded
```

### CsvReader

Read and iterate over CSV files or strings. Supports header rows, quoted values with embedded commas and newlines, and column access by name or index.

```csharp
using Benday.CommandsFramework.DataFormatting;

// From a file
var reader = CsvReader.FromFile("/path/to/data.csv");

// Or from a string
var reader = new CsvReader("Name,Age,City\nAlice,30,Seattle\nBob,25,Portland");

foreach (var row in reader)
{
    // Access by column name
    var name = row["Name"];
    var age = row["Age"];

    // Or by index
    var city = row[2];

    Console.WriteLine($"{name} is {age} years old and lives in {city}");
}
```

### CsvWriter

Build CSV data in memory, edit existing CSV content, and write to file or string. Handles quoting of values that contain commas, newlines, or quotes.

```csharp
using Benday.CommandsFramework.DataFormatting;

// Create from scratch
var writer = new CsvWriter();
writer.AddColumns("Name", "Age", "City");
writer.AddRow("Alice", "30", "Seattle");
writer.AddRow("Bob", "25", "Portland");

// Save to file
writer.SaveToFile("/path/to/output.csv");

// Or get as string
var csvString = writer.ToCsvString();
```

Edit existing CSV data by loading from a `CsvReader`:

```csharp
var reader = CsvReader.FromFile("/path/to/data.csv");
var writer = new CsvWriter(reader);

// Modify a value
writer.SetValue(0, "City", "Tacoma");

// Add a new row
writer.AddRow("Carol", "35", "Denver");

// Remove a row
writer.RemoveRow(1);

writer.SaveToFile("/path/to/updated.csv");
```

## Built-in Keywords

- `--help` — Display usage information for a command
- `--json` — Output the full command schema as JSON (used by tooling)
- `gui` — Launch the CmdUi web interface for this tool


