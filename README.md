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
- Built-in `--help` usage display that reports each argument's default value
- Command name aliases, including aliases that supply preset argument values
- Reuse command logic by running one command from inside another
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
  - [Positional Arguments](#positional-arguments)
  - [Argument Aliases](#argument-aliases)
  - [Friendly Names](#friendly-names)
  - [File and Directory Existence](#file-and-directory-existence)
- [Default Values](#default-values)
- [Command Aliases](#command-aliases)
  - [Short Names](#short-names)
  - [Aliases That Supply Argument Values](#aliases-that-supply-argument-values)
- [Reusing Command Logic](#reusing-command-logic)
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

Commands inherit from `Command` (or `DependencyInjectionCommand` when they need dependency injection). Use the `[Command]` attribute to define the command name and description.

There is one base class. A command whose work is sequential returns `Task.CompletedTask`; anything that touches the network needs an async environment anyway.

```csharp
using Benday.CommandsFramework;

[Command(Name = "greet",
    Description = "Says hello to someone",
    Category = "Demo")]
public class GreetCommand : Command
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

    protected override Task OnExecute(CancellationToken cancellationToken)
    {
        var name = Arguments.GetStringValue("name");
        var loud = Arguments.GetBooleanValue("loud");

        var message = $"Hello, {name}!";
        WriteLine(loud ? message.ToUpper() : message);

        return Task.CompletedTask;
    }
}
```

### 2. Set Up Program.cs

The whole of Program.cs can be one line. Commands are discovered in the entry assembly, and
the application name, version and website come from that assembly's own metadata.

```csharp
using Benday.CommandsFramework;

return await CommandsApp.RunAsync(args);
```

`RunAsync` returns the exit code and also sets `Environment.ExitCode`, so a
`static async Task<int> Main` works either way.

When your commands live in a different assembly than the executable, name any type from that
assembly:

```csharp
await CommandsApp.RunAsync<GreetCommand>(args);
```

Use the `CommandsApp` builder when you need to configure anything — dependency injection,
configuration sources, or how usage is displayed. Pass any command type from your assembly to
`Create<T>()` — the framework discovers all `[Command]`-attributed classes in that assembly.

```csharp
using Benday.CommandsFramework;

return await CommandsApp
    .Create<GreetCommand>(args)
    .WithAppInfo("My CLI Tool", "https://www.example.com")
    .WithVersionFromAssembly()
    .RunAsync();
```

`Create(args)` with no type argument does the same thing using the entry assembly, and
`WithAppInfoFromAssembly()` fills in whichever of name, version and website you have not set
yourself. A value that is never set is simply left out of the usage header rather than printing
as a blank line.

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

Argument names are matched without regard to case, so `/verbose`, `/Verbose`, and `/VERBOSE` all reach the same argument. Argument *values* keep their case — only names are case-insensitive.

### Positional Arguments

Use `FromPositionalArgument(n)` to read a value from its position on the command line instead of making the user type the argument name. Positions start at 1 and count the bare values that follow the command name:

```csharp
public override ArgumentCollection GetArguments()
{
    var args = new ArgumentCollection();

    args.AddString("source").AsRequired()
        .WithDescription("Source file")
        .FromPositionalArgument(1);

    args.AddString("destination").AsNotRequired()
        .WithDescription("Destination file")
        .FromPositionalArgument(2);

    args.AddBoolean("overwrite").AsNotRequired().AllowEmptyValue()
        .WithDescription("Overwrite the destination");

    return args;
}
```

```bash
mytool copy input.txt output.txt
mytool copy input.txt output.txt /overwrite
```

Named arguments do not consume positions, so they can appear anywhere in the command line without shifting the positional values:

```bash
mytool copy /overwrite input.txt output.txt   # source=input.txt, destination=output.txt
```

Unix style paths are handled correctly. A value like `/home/user/data.txt` contains more than one slash and no colon, so it is treated as a positional value rather than as an argument name.

Positional arguments show up in usage output wrapped in braces rather than with a leading slash:

```
{source:String}         - Source file
[{destination:String}]  - Destination file
```

### Argument Aliases

Use `WithAlias()` to give an argument a second name, which is handy for offering a short form:

```csharp
args.AddString("environment").AsRequired()
    .WithAlias("env")
    .WithDescription("Target environment");
```

```bash
mytool deploy /environment:production
mytool deploy /env:production            # same thing
```

The real argument name is matched first, so an alias can never shadow another argument's name.

`WithAlias()` and `FromPositionalArgument()` both write to the same underlying alias slot — `FromPositionalArgument(n)` works by setting the alias to `POSITION_n`. Use one or the other on any given argument, not both, since the second call overwrites the first.

### Friendly Names

Use `WithFriendlyName()` to give an argument a human readable label. This does not change the console usage output — it is carried in the `--json` schema and is used as the field label when the command is rendered in [cmdui](https://www.nuget.org/packages/Benday.CommandsFramework.CmdUi/):

```csharp
args.AddString("api-key").AsRequired()
    .WithFriendlyName("API Key")
    .WithDescription("Your API key");
```

### File and Directory Existence

`MustExist()` and `ExistenceOptional()` apply to `AddFile()` and `AddDirectory()` arguments only. Calling either one on any other argument type throws an `InvalidOperationException`.

Existence is optional by default, so `MustExist()` is the one you normally reach for. When set, validation fails if the file or directory is not there:

```csharp
args.AddFile("input").AsRequired()
    .MustExist()
    .WithDescription("Input file, must already exist");

args.AddDirectory("output-dir").AsNotRequired()
    .ExistenceOptional()
    .WithDescription("Output directory, created if missing");
```

Relative paths are resolved against the current working directory before the existence check. Read the resolved path back with the argument's `AbsolutePath` property, or with the `GetPathToFile()` / `GetPathToDirectory()` helpers:

```csharp
var inputPath = Arguments.GetPathToFile("input", mustExist: true, fullyQualifiedPath: true);
var outputPath = Arguments.GetPathToDirectory("output-dir");
```

## Default Values

`WithDefaultValue()` sets the value an argument falls back to when nothing is supplied. The default is reported in the `--help` output on a line of its own, so users can see what a command will do before they run it:

```csharp
args.AddString("thing").AsNotRequired()
    .WithDescription("thing to deploy")
    .WithDefaultValue("the-usual-thing");
```

```
deploy --help

** USAGE **
deploy
/environment:String - environment to deploy to
[/thing:String]     - thing to deploy
                      (default: the-usual-thing)
```

The default is also reported when a command fails validation, and it always shows the configured default rather than whatever was typed on the command line. Defaults are exposed on `IArgument.DefaultValue` and `IArgument.HasDefaultValue`, and are included in the `--json` schema output.

## Command Aliases

### Short Names

Use `Aliases` on the `[Command]` attribute to give a command extra names. This is handy for offering a short form of a long command name:

```csharp
[Command(Name = "generate-project-scaffolding",
    Aliases = new[] { "gps", "scaffold" },
    Description = "Generates project scaffolding")]
public class GenerateScaffoldingCommand : Command
```

```bash
mytool gps            # same as: mytool generate-project-scaffolding
```

Aliases are resolved to the real command name before the command runs, so `ExecutionInfo.CommandName` is always the real name. A real command name always wins over an alias, so an alias can never shadow an actual command. Aliases appear next to the command in the available commands list:

```
generate-project-scaffolding (gps, scaffold) - Generates project scaffolding
```

### Aliases That Supply Argument Values

Use `[CommandAlias]` to create a shortcut for a command that is usually run with the same set of arguments. Each entry is in `name=value` form; an entry with no `=` is treated as a flag style argument:

```csharp
[Command(Name = "deploy", Description = "Deploys a thing to an environment")]
[CommandAlias("deploy-prod", "environment=production", "verbose",
    Description = "Deploy to production with verbose output")]
[CommandAlias("deploy-dev", "environment=development",
    Description = "Deploy to development")]
public class DeployCommand : Command
```

```bash
mytool deploy-prod                          # environment=production, verbose=true
mytool deploy-prod /environment:staging     # environment=staging, verbose=true
```

The values are applied as though they had been typed on the command line, so anything actually supplied on the command line wins over them. The full order of precedence is:

**command line → alias → configuration (`FromConfig()`) → default value**

A command can have as many `[CommandAlias]` attributes as you like. They are listed in their own section of the usage output:

```
Command aliases:
deploy-dev  - Deploy to development (deploy /environment:development)
deploy-prod - Deploy to production with verbose output (deploy
              /environment:production /verbose)
```

Nothing validates aliases automatically. Call `CommandAttributeUtility.GetCommandNameProblems()` from a unit test to catch duplicate command names, aliases that collide with a command name or with a reserved keyword, aliases claimed by two commands, and empty aliases:

```csharp
[Fact]
public void NoCommandNameProblems()
{
    var util = new CommandAttributeUtility(new DefaultProgramOptions());

    Assert.Empty(util.GetCommandNameProblems(typeof(MyCommand).Assembly));
}
```

## Reusing Command Logic

A command can run another command in process rather than shelling out to the command line. Use `ExecuteCommandAsync<T>()`, which returns the command instance so you can read results back off it.

Expose whatever the caller needs as public properties set in `OnExecute()`:

```csharp
[Command(Name = "greeting", Description = "Builds a greeting for a person")]
public class GreetingCommand : Command
{
    public GreetingCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider)
        : base(info, outputProvider) { }

    public string Greeting { get; private set; } = string.Empty;

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();
        args.AddString("name").AsRequired().WithDescription("Name of the person to greet");
        return args;
    }

    protected override Task OnExecute(CancellationToken cancellationToken)
    {
        Greeting = $"Hello, {Arguments.GetStringValue("name")}!";
        WriteLine(Greeting);

        return Task.CompletedTask;
    }
}
```

```csharp
[Command(Name = "greet-everybody", Description = "Greets several people")]
public class GreetEverybodyCommand : Command
{
    public GreetEverybodyCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider)
        : base(info, outputProvider) { }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();
        args.AddString("names").AsRequired().WithDescription("Comma separated list of names");
        return args;
    }

    protected override async Task OnExecute(CancellationToken cancellationToken)
    {
        var names = Arguments.GetStringValue("names")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var name in names)
        {
            var command = await ExecuteCommandAsync<GreetingCommand>(
                args => args["name"] = name, cancellationToken: cancellationToken);

            WriteLine(command.Greeting);
        }
    }
}
```

Things worth knowing:

- The command that gets run shares the calling command's program options, configuration, and output provider.
- It runs in **quiet mode** by default, which suppresses its `WriteLine()` output so it does not write over the calling command's output. Pass `quiet: false` to let it write.
- A validation failure **throws** a `KnownException` instead of printing usage information. Running a command from the command line prints usage and returns, which would leave the calling command with no way of knowing that the command never ran.
- The process exit code is left alone — nothing below the console entry point touches it. A command reports how it went by returning a `CommandResult`.
- `CreateCommand<T>()` builds the command without running it, if you need to inspect or configure it first.
- Commands nested more than `CommandFrameworkConstants.MaxCommandNestingDepth` levels deep throw, so an accidental "A calls B calls A" loop produces a clear error rather than a stack overflow.

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
| `RunAsync(args)` | **Static.** Create, configure from assembly metadata, and run in one call |
| `RunAsync<TCommand>(args)` | **Static.** Same, with commands discovered in the assembly containing `TCommand` |
| `Create<TCommand>(args)` | Create builder, discover commands from the assembly containing `TCommand` |
| `Create(args)` | Create builder, discover commands from the entry assembly |
| `Create(args, assembly)` | Create builder with explicit assembly |
| `WithAppInfoFromAssembly()` | Fill in name, version and website from assembly metadata, leaving anything already set alone |
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
| `Run()` | Build and run the application, returning the exit code |
| `RunAsync(cancellationToken)` | Build and run the application asynchronously, returning the exit code |

## Argument Rules

Some requirements are about the *combination* of arguments rather than any one of them.
Declare them and the framework enforces them, prints them in the usage output, and ships them
in the schema:

```csharp
public override ArgumentCollection GetArguments()
{
    var args = new ArgumentCollection();

    args.AddString("token").AsNotRequired().WithDescription("Personal access token");
    args.AddBoolean("windowsauth").AsNotRequired().AllowEmptyValue();
    args.AddString("username").AsNotRequired();
    args.AddString("password").AsNotRequired();

    args.ExactlyOneOf("token", "windowsauth");
    args.RequiredTogether("username", "password");
    args.When("mode", "advanced").Require("level").Forbid("simpleflag");

    return args;
}
```

| Rule | Meaning |
|------|---------|
| `ExactlyOneOf(...)` | Exactly one has to be supplied |
| `AtLeastOneOf(...)` | At least one has to be supplied |
| `MutuallyExclusive(...)` | No two of these together; none is required |
| `RequiredTogether(...)` | All of them or none of them |
| `When(arg, value).Require(...)` | Required only when `arg` has that value |
| `When(arg, value).Forbid(...)` | Not allowed when `arg` has that value |

`When(arg)` with no value means "whenever that argument is supplied at all". Zero and several
produce different messages, because they are different mistakes:

```
$ mytool connect
One of 'token', 'windowsauth' is required.

$ mytool connect /token:abc /windowsauth
Only one of 'token', 'windowsauth' can be supplied, but 'token', 'windowsauth' were.
```

Rules are declarative rather than a callback in `OnExecute()` so that the `--json` schema
carries them — which is what lets a form apply them as it is being filled in rather than only
when it is submitted.

## Multi-level Commands

Give a command a `Group` and it is run as two words:

```csharp
[Command(Group = "widget", Name = "list", Description = "Lists the widgets")]
public class WidgetListCommand : Command
```

```bash
mytool widget list /filter:blue
```

Resolution is greedy longest-first, so a two-word name wins over a one-word name that happens
to match the first word. A group on its own is not a command.

`Group` is deliberately separate from `Category`. Category is a display heading for the command
list — strings like "Work Items" — and using it as a prefix would produce command names nobody
would type. Grouping is a rename, not a prefix.

Adopting groups in an existing tool does not have to break anyone's scripts. Keep the old flat
name as an alias:

```csharp
[Command(Group = "widget", Name = "show",
    Description = "Shows one widget",
    Aliases = ["showwidget"])]
```

Both `mytool widget show /name:sprocket` and `mytool showwidget /name:sprocket` work, and the
command list shows `widget show (showwidget)`.

## Output Channels

Commands write on three channels, the same split every other command line tool uses:

| Method | What it is for | Console destination |
|--------|----------------|---------------------|
| `WriteLine()` / `Write()` | The result — what the command was asked to produce | stdout |
| `WriteStatus()` | Commentary about the work — progress, notes | stderr |
| `WriteError()` | Failures. Never suppressed by quiet mode | stderr |

This is what makes a command's output pipeable. A command that writes its result with
`WriteLine()` and everything else with `WriteStatus()` can have its output redirected to a
file without the commentary landing in it:

```bash
mytool export /format:json > data.json     # only the result is captured
```

`StringBuilderTextOutputProvider` captures the channels separately, so a test can assert on
the payload without the chatter:

```csharp
Assert.Equal(expectedJson, output.GetResultOutput());
Assert.Contains("Exported 42 rows", output.GetStatusOutput());
```

`GetOutput()` still returns everything in the order it was written.

If you have written your own `ITextOutputProvider`, nothing breaks — `WriteStatus()` and
`WriteError()` fall back to `WriteLine()` until you override them.

## Reporting Progress

```csharp
protected override async Task OnExecute(CancellationToken cancellationToken)
{
    var items = await LoadItems(cancellationToken);

    for (var i = 0; i < items.Count; i++)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ReportProgress($"Processing {items[i].Name}", i + 1, items.Count);
    }

    WriteLine($"Processed {items.Count} items.");
}
```

Progress goes to the diagnostic channel, so it never lands inside a redirected result:

```bash
mytool process > results.txt     # progress still shows on screen; results.txt has only results
mytool process 2>/dev/null       # progress silenced, results still produced
```

On a terminal the console provider redraws a single line in place. When stderr is redirected it
writes plain lines instead — otherwise the carriage returns would fill the destination with
unreadable spam. `CommandBase.Progress` is an `IProgress<CommandProgress>`, so it can be handed
straight to any API that already takes one.

In a test, assert on what was reported:

```csharp
Assert.Equal(3, output.ProgressReports.Count);
Assert.Equal(1.0, output.ProgressReports[^1].Fraction);
```

## Prompting for Input

Commands read input through `ITextInputProvider`, the counterpart to `ITextOutputProvider`.
`CommandBase` gives you `ReadLine()`, `Prompt()` and `PromptForYesNo()`:

```csharp
protected override void OnExecute()
{
    var name = Arguments.GetStringValue("name");

    if (string.IsNullOrWhiteSpace(name))
    {
        name = Prompt("What is your name? ");
    }

    if (PromptForYesNo($"Say hello to {name}?"))
    {
        WriteLine($"Hello, {name}!");
    }
}
```

Because the provider comes from the program options rather than from the console, an
interactive command is testable — queue up the answers and run it:

```csharp
var output = new StringBuilderTextOutputProvider();
var input = new QueuedTextInputProvider("Ben", "y");

var options = new DefaultProgramOptions
{
    ApplicationName = "My CLI Tool",
    OutputProvider = output,
    InputProvider = input
};

// ...run the command, then assert
Assert.Contains("Hello, Ben!", output.GetOutput());
Assert.Equal(2, input.ReadCount);
```

| Type | Description |
|------|-------------|
| `ConsoleTextInputProvider` | Reads from the console. The default. |
| `QueuedTextInputProvider` | Hands out queued lines, then `null`. For tests. |

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
- `quiet` — Suppress a command's `WriteLine()` output. Applied automatically to commands that are run by another command.


