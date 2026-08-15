# Benday.CommandsFramework

## What This Is
A .NET CLI framework for building command-line tools. Provides structured command definitions, argument parsing/validation, and output formatting. Distributed as a NuGet package (`Benday.CommandsFramework`).

## Solution Structure
- `src/Benday.CommandsFramework/` - Core framework library (NuGet package, targets net8.0;net9.0;net10.0)
- `src/Benday.CommandsFramework.CmdUi/` - Blazor Server web UI shell for any framework-based tool (dotnet global tool `cmdui`, targets net10.0)
- `test/Benday.CommandsFramework.Tests/` - Unit tests (xunit.v3, via Microsoft.Testing.Platform)
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

Ordering gotcha: methods declared on `Argument<T>` return `Argument<T>`, so type-specific methods like
`WithAllowedValues()` (needs `StringArgument`) must come **first** in the chain, right after `AddString()`.

`WithDefaultValue()` applies the value via `TrySetValue()` *and* records it on
`IArgument.DefaultValue` / `HasDefaultValue` (through `Argument<T>.TrySetDefaultValue()`). The recorded
default is never overwritten by command-line or config values, which is what lets usage output report
the real default even on the validation-failure path. The implicit type default (`""`, `false`, `0`,
`DateTime.MinValue`) does not count — `HasDefaultValue` is false unless `WithDefaultValue()` was called.

### CLI Argument Format
Arguments use `/name:value` syntax. Boolean flags with `AllowEmptyValue` use `/name` (presence = true).
Parsing lives in `ArgumentCollectionFactory.GetArgsAsDictionary()`; `input[0]` is the command name and
everything after it is parsed.

Value precedence: command line > command alias presets > configuration (`FromConfig()`) > default value.

**Argument names are matched case-insensitively** via `ArgumentCollection.ArgumentNameComparer`
(`OrdinalIgnoreCase`), which backs the argument dictionary, the alias lookup in `SetValues()`, the
parsed dictionary from `ArgumentCollectionFactory`, and `CommandExecutionInfo.Arguments`. So
`/verbose`, `/Verbose` and `/VERBOSE` all reach the same argument, for both the `/name:value` and
flag-style forms. Argument *values* keep their case; only names are case-insensitive.

Use `ArgumentCollection.ArgumentNameComparer` whenever you build a dictionary that will hold argument
names, so a name can't get in twice under different casing.

### Positional Arguments
`FromPositionalArgument(n)` sets `Alias = "POSITION_n"` and `IsPositionalSource = true`; binding then
happens through the normal alias path in `ArgumentCollection.SetValues()`. Position must be >= 1.
- Counting covers only bare positional values, so named args interleave freely without shifting positions.
- A `/`-prefixed token with **more than one slash** and no colon is treated as a Unix path and becomes
  positional; one slash and no colon is treated as a flag name.
- `WithAlias()` and `FromPositionalArgument()` share the `Alias` slot — using both on one argument
  breaks whichever was set first.
- Usage output renders these as `{name:Type}` (required) / `[{name:Type}]` (optional) via `GetKeyString()`.

### Argument Aliases vs Command Aliases
Different mechanisms, easy to confuse. `WithAlias()` is an *argument* alias (a second name for a
`/switch`), matched in `SetValues()` after real names. Command aliases are `CommandAttribute.Aliases`
and `[CommandAlias]` — see below.

### FriendlyName
`WithFriendlyName()` is **not** used by console usage output (`GetKeyString()` uses `Name`). It only
travels in the `--json` schema and becomes the form field label in cmdui
(`ArgumentField.razor`, `CommandRunner.razor`).

### File and Directory Arguments
`FileArgument` / `DirectoryArgument` both derive from `StringArgument` and add `MustExist`
(default `false`) plus an `AbsolutePath` property that fully-qualifies the value. `MustExist()` /
`ExistenceOptional()` throw `InvalidOperationException` on any other argument type. Validation checks
`File.Exists`/`Directory.Exists` against `AbsolutePath`. `GetPathToFile()` / `GetPathToDirectory()`
extension methods do the same resolution off the collection.

### Built-in Keywords
- `--help` — display usage
- `--json` — dump full command schema as JSON (used by cmdui for auto-generating UI)
- `gui` — launch `cmdui` for the current tool
- `quiet` — reserved argument; suppresses `CommandBase.WriteLine()` output

### Command Aliases
Two kinds, both resolved to the real command name at a single chokepoint before anything else reads
the command name (`CommandAttributeUtility.ResolveCommandName`, called from `GetCommand()` and
`DefaultProgram.Run()`). Real command names always beat aliases.
- `[Command(Name = "long-name", Aliases = new[] { "ln" })]` — plain rename. Shown inline in the
  command list as `long-name (ln)`.
- `[CommandAlias("deploy-prod", "environment=production", "verbose")]` — alias that also supplies
  argument values. Values are injected into the parsed command-line dictionary via `TryAdd`, so
  explicit command-line args win and no new precedence logic exists. Listed in a separate
  `Command aliases:` section.

`CommandAttributeUtility.GetCommandNameProblems()` reports duplicate names, aliases colliding with
command names or reserved keywords, aliases claimed by two commands, and empty aliases. Nothing calls
it automatically — call it from a unit test.

### Calling Commands From Commands
`CommandBase.CreateCommand<T>()`, `ExecuteCommand<T>()` (sync) and `ExecuteCommandAsync<T>()` (async)
instantiate and run another command in process and return the instance so results can be read off it.
Expose results as public properties set in `OnExecute()`.
- The child shares the caller's `Options`, `Configuration` and `_OutputProvider`, and runs quiet by default.
- Validation failure **throws** `KnownException` rather than printing usage — an in-process caller
  needs to know the command didn't run.
- `Environment.ExitCode` is saved/restored around the call so a child can't set the process exit code.
- `CommandExecutionInfo.NestingDepth` guards against A→B→A loops (`MaxCommandNestingDepth`).

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
path (before values are set) and `OnValidationFailure` (after values are set) — which is exactly why
defaults must be recorded separately rather than read off `Value`.

Arguments with a configured default get a `(default: value)` line of their own, indented to align
with the description column. Whitespace-only defaults are suppressed.

### Output
Commands use `WriteLine()` which goes through `ITextOutputProvider`. `ConsoleTextOutputProvider` for console, `StringBuilderTextOutputProvider` for testing/capturing.

### Data Formatting
`DataFormatting/` has `CsvReader`, `CsvWriter`, `CsvRow`, and `TableFormatter` /
`TableColumnDefinition` for tabular console output.

### Dependency Injection
`DependencyInjectionCommand` base class plus `CommandsApp` fluent setup for registering services into
the command's `IServiceProvider`. The provider is built once on first use and cached on
`ICommandProgramOptions.ServiceProvider`, so all commands in a process share it (singletons really are
singletons). Each command creates its own `IServiceScope`; `DependencyInjectionCommand` implements
`IDisposable` to release it.

## CmdUI Project
`cmdui` is a schema-driven Blazor Server app that auto-generates a web UI for any CommandsFramework tool:
- `cmdui slnutil` — runs `slnutil --json`, renders forms for each command
- `cmdui` (no args) — discovers all installed tools via `dotnet tool list --global`
- Skips itself during discovery to avoid recursion
- 10-second timeout on schema probing
- Static files served from assembly directory when installed as tool; via manifest in dev mode
- `ToolSchemaService` sets `FileName = toolName`, so an **absolute path to a built binary works**
  for local testing without installing a global tool:
  `dotnet run --project src/Benday.CommandsFramework.CmdUi -- /abs/path/to/Tool`

**Keeping cmdui in sync with the schema.** `Models/ToolCommandInfo.cs` and `Models/ToolArgumentInfo.cs`
are hand-maintained mirrors of `CommandInfo` / `IArgument`. Deserialization ignores unknown JSON
properties, so adding a property to the framework schema does **not** break cmdui — it silently goes
missing in the UI. When adding anything to `IArgument` or `CommandInfo`, add the matching property here
too.

`CommandInfo` exposes the two alias kinds separately: `Aliases` (plain renames from
`CommandAttribute.Aliases`) and `CommandAliases` (a `List<CommandAliasInfo>` for `[CommandAlias]`
presets, populated in `PopulateUsages` by filtering `GetCommandAliases(asm)`).

## Build & Test
```bash
dotnet build                    # build entire solution
dotnet test                     # run tests
dotnet run --project src/Benday.CommandsFramework.CmdUi -- slnutil   # test cmdui locally
```

Tests are xunit.v3, which builds the test project as an executable running on Microsoft.Testing
Platform (MTP) rather than VSTest.

**`global.json` is required for `dotnet test` to find any tests.** It sets
`test.runner = "Microsoft.Testing.Platform"`, which is the .NET 10 SDK opt-in to the MTP-based
`dotnet test`. Without it the run falls back to VSTest, which discovers zero tests in an MTP project
**and still exits 0** — a green build that ran nothing. Don't delete that file. (It deliberately has
no `sdk` section, so it does not pin the SDK version.)

Gotcha: in MTP mode, non-build flags are forwarded to the test app, which rejects unknown ones.
`--nologo` fails with `Unknown option '--nologo'`; `--configuration`, `--no-build`, `--verbosity`,
`--framework` are fine. Exit codes: 0 pass, 2 test failure, 5 zero tests ran.

The test binary can also be run directly:
```bash
./test/Benday.CommandsFramework.Tests/bin/Debug/net10.0/Benday.CommandsFramework.Tests
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
