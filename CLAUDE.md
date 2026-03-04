# Benday.CommandsFramework

## What This Is
A .NET CLI framework for building command-line tools. Provides structured command definitions, argument parsing/validation, and output formatting. Distributed as a NuGet package (`Benday.CommandsFramework`).

## Solution Structure
- `src/Benday.CommandsFramework/` - Core framework library (NuGet package, targets net8.0;net9.0)
- `src/Benday.CommandsFramework.CmdUi/` - Blazor Server web UI shell for any framework-based tool (dotnet global tool `cmdui`, targets net10.0)
- `test/Benday.CommandsFramework.Tests/` - Unit tests (xUnit)
- `test/Benday.CommandsFramework.Samples/` - Sample commands demonstrating framework features

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
`String`, `Int32`, `Boolean`, `DateTime`, `File`, `Directory` — configured via fluent API on `ArgumentCollection`.

### CLI Argument Format
Arguments use `/name:value` syntax. Boolean flags with `AllowEmptyValue` use `/name` (presence = true). Positional args are bare values mapped to `POSITION_1`, `POSITION_2`, etc.

### Built-in Keywords
- `--help` — display usage
- `--json` — dump full command schema as JSON (used by cmdui for auto-generating UI)
- `gui` — launch `cmdui` for the current tool

### Output
Commands use `WriteLine()` which goes through `ITextOutputProvider`. `ConsoleTextOutputProvider` for console, `StringBuilderTextOutputProvider` for testing/capturing.

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
- Framework: `dotnet pack src/Benday.CommandsFramework/`
- CmdUI: builds NuGet package on build automatically (`GeneratePackageOnBuild`)
- Both use `<TheVersion>` property for version synchronization
