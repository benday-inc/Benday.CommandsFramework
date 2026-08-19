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
Commands inherit from `Command` (or `DependencyInjectionCommand`) and use the `[Command]` attribute.
There is **one** base class: `SynchronousCommand` is gone, `AsynchronousCommand` is an `[Obsolete]`
empty subclass of `Command` kept so existing code compiles, and `CommandAttribute.IsAsync` is
`[Obsolete]` and read by nothing — the type system already says how a command runs, and that flag
could disagree with it (it built cleanly and then threw at run time).

`CommandAttributeUtility.IsCommandType()` is the single definition of what counts as a command — a
concrete `CommandBase` subclass carrying the attribute — and every discovery path goes through it, so
the list of commands shown to the user can never disagree with the list that can be instantiated.
A `[Command]` class failing either half is skipped and reported by `GetCommandNameProblems()`:
```csharp
[Command(Name = "mycommand", Description = "Does something", Category = "MyCategory")]
public class MyCommand : Command
{
    public override ArgumentCollection GetArguments() { ... }
    protected override async Task OnExecute(CancellationToken cancellationToken) { ... }
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

`AllowedValues` lives on `StringArgument`, which is the only argument type whose `Validate()`
enforces it. `Argument<T>.AllowedValues` reads as empty and **throws `InvalidOperationException`
when set**, so putting a list on an int or boolean argument fails loudly instead of shipping a
dropdown in the schema that nothing enforces. `FileArgument` / `DirectoryArgument` derive from
`StringArgument`, so they keep the feature.

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

Their `DataType` is `String` — parsing and conversion really are a string's. What sets them apart in
the schema is `IArgument.PathType` (`ArgumentPathType.None` / `File` / `Directory`) and
`IArgument.MustExist`. Both are **default interface members**, so adding them broke no existing
implementor, and both are re-declared on `FileArgument` / `DirectoryArgument` — which is why those
two name `IArgument` again in their base list. Without that, the interface mapping established by
`Argument<T>` wins and a file argument still reports `None`. Nothing switches on `DataType`
differently as a result; anything that wants to know a path from a string reads `PathType`.

### Schema Envelope
`--json` writes a `CommandSchema` object: `SchemaVersion`, `ApplicationName`, `ApplicationVersion`,
`Commands`. v4 wrote a bare array, so consumers discriminate on the **root JSON token alone** — no
negotiation. `CommandFrameworkConstants.CurrentSchemaVersion` is the version;
`ToolSchemaService.ParseSchema` in cmdui is the reference reader and refuses a version newer than it
understands rather than guessing.

The schema types **serialize but do not deserialize** — `CommandInfo`'s setters are internal and
`Arguments` is a collection of an interface, so `JsonSerializer.Deserialize<CommandSchema>` hands
back blank objects instead of throwing. Read a schema through mirror types, the way cmdui does.

### Built-in Keywords
- `--help` — display usage
- `--json` — dump full command schema as JSON (used by cmdui for auto-generating UI)
- `gui` — launch `cmdui` for the current tool
- `quiet` — reserved argument; suppresses `CommandBase.WriteLine()` output

### Command Registry
`CommandRegistry` is the single place commands are discovered. `CommandAttributeUtility.GetRegistry()`
builds it once and caches it on `ICommandProgramOptions.CommandRegistry`; a cached registry is only
reused when `WasBuiltFor(assembly, usesConfiguration)` agrees, since flipping `UsesConfiguration`
changes whether the built-ins are registered.

- Built-in configuration commands are **ordinary registrations**, marked `IsBuiltIn`. The
  `UsesConfiguration` / `IsDefaultCommandName` routing that used to be decided three separate times
  (twice in `DefaultProgram.Run()`, once in `GetCommand()`) is gone.
- Keyed with `ArgumentCollection.ArgumentNameComparer`, so **command names and aliases are
  case-insensitive** — the rule argument names have followed since v4.18.
- `Resolve(tokens)` matches greedy longest-first and returns a `CommandResolution` carrying the
  registration, the leftover tokens for the parser, the `PresetArguments` from a `[CommandAlias]`,
  and `MatchedAs` (what was actually typed). Resolution no longer overwrites the typed name in place.
- `CommandRegistration.Path` is a list so a `Group` can become the first segment
  (`CommandAttribute.Group`, distinct from `Category` which is only a display heading). Multi-level
  *dispatch* is not wired into `DefaultProgram` yet — that is FEAT-2.
- `BuildFromTypes()` builds from an explicit type list; useful for tests that need a registry
  without whatever else is in the assembly.

**Two commands claiming the same name, or the same alias, throws `KnownException` when the registry
is built.** Everything else that makes a command unreachable — an alias shadowed by a real name, a
reserved-keyword collision, an empty alias, `[Command]` on a class that isn't a runnable
`CommandBase` — lands on `CommandRegistry.Problems`, because one bad alias shouldn't stop the other
63 commands running. Assert `Problems` is empty from a unit test.

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
command names or reserved keywords, aliases claimed by two commands, empty aliases, and classes
carrying a `[Command]` attribute that the framework cannot run. Nothing calls it automatically —
call it from a unit test.

### Execution Contract
`Command.ExecuteAsync(CancellationToken)` returns a **`CommandResult`** — `Status`
(`Success` / `ValidationFailed` / `UsageDisplayed` / `Failed` / `Cancelled`), `Message`,
`InvalidArguments`, `IsSuccess`, `ExitCode`. `UsageDisplayed` counts as success: the user asked for
usage and got it.

**Nothing in the framework assigns `Environment.ExitCode` except `CommandsApp.Run/RunAsync`**, the
console entry point. `Validate()` used to set it as a side effect and `DisplayUsage()` set failure,
which forced `CommandBase` into save/restore dances around nested calls; all of that is gone.
`DefaultProgram.RunAsync()` *returns* the exit code, so the same commands can run in a host that
outlives any one of them.

`OnExecute(CancellationToken)` takes a token: pass it to anything that accepts one and check it
between units of work. `ExecuteAsync` converts an `OperationCanceledException` into
`CommandResult.Cancelled()` when the token was the cause, so cancelling *this command* does not have
to mean stopping the process.

### Calling Commands From Commands
`CommandBase.CreateCommand<T>()` and `ExecuteCommandAsync<T>()` instantiate and run another command
in process and return the instance so results can be read off it.
Expose results as public properties set in `OnExecute()`.
- The child shares the caller's `Options`, `Configuration` and `_OutputProvider`, and runs quiet by default.
- Validation failure **throws** `KnownException` rather than printing usage — an in-process caller
  needs to know the command didn't run.
- `CommandExecutionInfo.NestingDepth` guards against A→B→A loops (`MaxCommandNestingDepth`).
- There is no `ExecuteCommand<T>` any more — one base class means one method, `ExecuteCommandAsync<T>`.

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
Three channels, following the convention every other CLI uses:

| Channel | Method | Console destination | Quiet mode |
|---|---|---|---|
| result | `WriteLine()` / `Write()` | stdout | suppressed *(v4 behavior; OUT-1 defers the redefinition to v5)* |
| status | `WriteStatus()` | stderr | suppressed |
| error | `WriteError()` | stderr | **never** suppressed |

Keeping them apart is what lets output be piped: a command that grows a `/json` flag emits invalid
JSON the moment anything else writes to the same stream. `DefaultProgram`'s `catch (KnownException)`
now writes to the error channel, so a failed command piping `--json` to a file no longer lands its
error text inside the JSON.

`WriteStatus()` / `WriteError()` are **default interface members** on `ITextOutputProvider` that fall
back to `WriteLine()`, so a provider written before the split keeps working and keeps everything on
one channel. `ConsoleTextOutputProvider` overrides them to `Console.Error`.
`StringBuilderTextOutputProvider` buffers each channel separately: `GetOutput()` still returns
everything in write order (what every existing test asserts on), and `GetResultOutput()` /
`GetStatusOutput()` / `GetErrorOutput()` give the separated views.

Writing to stderr does not fail a build by default — GitHub Actions goes by exit code, and Azure
DevOps `Bash@3` / `PowerShell@2` both default `failOnStderr: false`. The hazard is opt-in.

### Reserved Keywords
`ReservedKeywords` is the single source for the names the framework claims — it backs both the
usage output that lists them and the argument validation that skips them (`ArgumentCollection`).
`ForCommands` (`--help`, `quiet`) prints as an `** ALSO AVAILABLE **` section in per-command usage;
`ForPrograms` (`--help`, `--json`, `gui`) prints as `Also available:` under the command list. Before
this they appeared nowhere, since usage output only ever listed a command's own arguments.

### Input
`ITextInputProvider` is the counterpart to `ITextOutputProvider` and hangs off
`ICommandProgramOptions.InputProvider` — *not* the constructor, which is a hardcoded two-arg
reflection contract in two places. `ConsoleTextInputProvider` for real use;
`QueuedTextInputProvider` queues answers for tests (`ReadCount` / `RemainingLineCount` let a test
assert how many times a command prompted). It returns `null` once the queue is empty, which is what
`Console.ReadLine()` returns at end of input.

`CommandBase` exposes `ReadLine()`, `Prompt(text)` (writes without a newline, trims the answer) and
`PromptForYesNo(text, defaultAnswer)`. `DefaultProgram`'s cmdui install prompt goes through the same
provider. On `ICommandProgramOptions` the member is a get-only default interface member so adding it
broke no implementor; `DefaultProgramOptions` declares it settable.

### Data Formatting
`DataFormatting/` has `CsvReader`, `CsvWriter`, `CsvRow`, and `TableFormatter` /
`TableColumnDefinition` for tabular console output.

### Bootstrapping
`CommandsApp.RunAsync(args)` is the whole of Program.cs for a tool with no DI or configuration
setup: commands come from the entry assembly, and `ApplicationName` / `Version` / `Website` come
from that assembly's metadata (`AssemblyTitle` → `AssemblyProduct` → simple name; informational
version with the `+sha` suffix trimmed, then file version; `AssemblyMetadata` named
`PackageProjectUrl` / `RepositoryUrl` / `Website`, none of which the SDK emits by default).
`RunAsync<T>(args)` is the same when the commands live in another assembly.

`Create(args)` (entry assembly) and `WithAppInfoFromAssembly()` expose the same defaults to the
fluent builder; the latter never overwrites a value that was already set. `DisplayUsage()` skips
a header line whose value is blank, so an unset website does not print as an empty line.

Note that `GetCommand()` builds a `FileBasedConfigurationManager` from `ConfigurationFolderName`
regardless of `UsesConfiguration`, and that throws on a blank name — so a builder-configured app
still needs an `ApplicationName` even with configuration turned off.

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
