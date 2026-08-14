# Benday.CommandsFramework

## What This Is
A .NET CLI framework for building command-line tools. Provides structured command definitions, argument parsing/validation, and output formatting. Distributed as a NuGet package (`Benday.CommandsFramework`).

## Solution Structure
- `src/Benday.CommandsFramework/` - Core framework library (NuGet package, targets net8.0;net9.0;net10.0)
- `src/Benday.CommandsFramework.CmdUi/` - Blazor Server web UI shell for any framework-based tool (dotnet global tool `cmdui`, targets net10.0)
- `test/Benday.CommandsFramework.Tests/` - Unit tests (xUnit)
- `test/Benday.CommandsFramework.Samples/` - Sample commands demonstrating framework features

Solution file is `Benday.CommandsFramework.slnx` (XML-based slnx format, not .sln).

## Key Patterns

### Defining Commands
Commands inherit from `SynchronousCommand`, `AsynchronousCommand`, or `DependencyInjectionCommand` and use the `[Command]` attribute:
```csharp
[Command(Name = "mycommand", Description = "Does something", Category = "MyCategory")]
public class MyCommand : SynchronousCommand
{
    public override ArgumentCollection GetArguments() { ... }
    protected override void OnExecute() { ... }
}
```

### Argument Types
`String`, `Int32`, `Boolean`, `DateTime`, `File`, `Directory` — configured via fluent API on `ArgumentCollection`
(`AddString()`, `AddInt32()`, `AddBoolean()`, `AddDateTime()`, `AddFile()`, `AddDirectory()`).

Fluent configuration methods live in `ExtensionMethods.cs`:
`WithDescription()`, `WithFriendlyName()`, `WithAlias()`, `WithDefaultValue()`, `AsRequired()`,
`AsNotRequired()`, `AllowEmptyValue()`, `MustExist()` / `ExistenceOptional()` (file/dir only),
`WithAllowedValues()` (string only — renders as a dropdown in cmdui), `FromPositionalArgument(n)`,
`FromConfig()`.

Note: `WithDefaultValue()` does not store a separate "default" — it calls `TrySetValue()`, which sets
`Value` and flips `HasValue` to true. There is no `DefaultValue` property on `IArgument`.

### CLI Argument Format
Arguments use `/name:value` syntax. Boolean flags with `AllowEmptyValue` use `/name` (presence = true). Positional args are bare values mapped to `POSITION_1`, `POSITION_2`, etc.

Value precedence: command line > configuration (`FromConfig()`) > default value.

### Built-in Keywords
- `--help` — display usage
- `--json` — dump full command schema as JSON (used by cmdui for auto-generating UI)
- `gui` — launch `cmdui` for the current tool

### Configuration
`FromConfig()` arguments read from a stored config file, managed by built-in commands
`set-configuration`, `get-configuration`, `remove-configuration` (enabled via
`ICommandProgramOptions.UsesConfiguration`). `CommandsApp.ConfigureConfiguration()` adds custom
`IConfiguration` sources. Config-sourced args print in a separate `** CONFIGURATION **` section of usage output.

### Validation
`CommandBase.Validate()` calls `SetValuesFromExecutionInfo()` first (config values, then command line
on top), then validates each argument. `StrictArgumentValidation` (off by default) makes unrecognized
command-line arguments fail validation via `ArgumentCollection.UnrecognizedKeys`.

### Usage Output
`CommandBase.DisplayUsage(StringBuilder)` builds the per-command usage text. Argument names are
padded to a shared column width and descriptions are line-wrapped against `Console.WindowWidth`
(60 when output is redirected) via `LineWrapUtilities`. It is called from two places: the `--help`
path (before values are set) and `OnValidationFailure` (after values are set).

### Output
Commands use `WriteLine()` which goes through `ITextOutputProvider`. `ConsoleTextOutputProvider` for console, `StringBuilderTextOutputProvider` for testing/capturing.

### Data Formatting
`DataFormatting/` has `CsvReader`, `CsvWriter`, `CsvRow`, and `TableFormatter` /
`TableColumnDefinition` for tabular console output.

### Dependency Injection
`DependencyInjectionCommand` base class plus `CommandsApp` fluent setup for registering services into
the command's `IServiceProvider`.

## CmdUI Project
`cmdui` is a schema-driven Blazor Server app that auto-generates a web UI for any CommandsFramework tool:
- `cmdui slnutil` — runs `slnutil --json`, renders forms for each command
- `cmdui` (no args) — discovers all installed tools via `dotnet tool list --global`
- Skips itself during discovery to avoid recursion
- 10-second timeout on schema probing
- Static files served from assembly directory when installed as tool; via manifest in dev mode

## Build & Test
```bash
dotnet build                    # build entire solution
dotnet test                     # run tests
dotnet run --project src/Benday.CommandsFramework.CmdUi -- slnutil   # test cmdui locally
```

## Packaging
- Framework: `dotnet pack src/Benday.CommandsFramework/` (also `GeneratePackageOnBuild`)
- CmdUI: builds NuGet package on build automatically (`GeneratePackageOnBuild`)
- Both use `<TheVersion>` property for version synchronization
- When bumping `<TheVersion>` in `Benday.CommandsFramework.csproj`, add a matching line to
  `<PackageReleaseNotes>` in the same file — that list is the release history

## Docs
API docs are generated with docfx (`generate-docfx-docs.sh` / `.ps1`) from `docfx_project/`, then
copied into `docs/` (`copy-docfx-site-to-docs.sh` / `.ps1`) for GitHub Pages. `docs/` is generated
output — don't hand-edit it.
