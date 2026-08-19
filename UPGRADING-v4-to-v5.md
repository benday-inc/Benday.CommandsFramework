# Upgrading from Benday.CommandsFramework v4 to v5

This document is written to be handed to an AI coding assistant along with the source of a
tool built on Benday.CommandsFramework v4, so the upgrade can be performed without anyone
having to remember what changed. It is equally readable by a person.

**Status: in progress.** v5 is not finished. Entries are added as each breaking change
lands, so this document is never reconstructed from memory. The
[What has not landed yet](#what-has-not-landed-yet) section says what is still coming.

---

## How to use this document

1. Read [Ordering](#ordering) and do the steps in that order. Some changes depend on others.
2. For each entry, run its **Detect** command first. If it finds nothing, skip the entry.
3. Apply the **Change**. Entries are marked either **Mechanical** or **Judgment**:
   - **Mechanical** — the transform is exact. Apply it everywhere Detect found a hit.
   - **Judgment** — the right answer depends on what the code means. Do not guess. Make the
     change where you are confident, and produce a list of every remaining site for a human
     to decide on.
4. Run [Verification](#verification) at the end.

Do not skip a Detect step because the change "looks like it does not apply" — these commands
are the definition of what applies.

---

## Ordering

1. [1. Target the v5 package](#1-target-the-v5-package)
2. [2. The `--json` schema is now an object](#2-the---json-schema-is-now-an-object)
3. [3. Command names are matched without regard to case](#3-command-names-are-matched-without-regard-to-case)
4. [4. Duplicate command names and aliases now fail at startup](#4-duplicate-command-names-and-aliases-now-fail-at-startup)
5. [5. `ICommandProgramOptions` gained two members](#5-icommandprogramoptions-gained-two-members)
6. [6. One command base class](#6-one-command-base-class)
7. [7. `OnExecute` takes a `CancellationToken`](#7-onexecute-takes-a-cancellationtoken)
8. [8. Commands return a result instead of setting `Environment.ExitCode`](#8-commands-return-a-result-instead-of-setting-environmentexitcode)
9. [9. `Program.cs` returns the exit code](#9-programcs-returns-the-exit-code)
10. [10. `ExecuteCommand<T>` is now `ExecuteCommandAsync<T>`](#10-executecommandt-is-now-executecommandasynct)
11. [11. Commands are created through `ActivatorUtilities`](#11-commands-are-created-through-activatorutilities)
12. [12. The request is split out of `CommandExecutionInfo`](#12-the-request-is-split-out-of-commandexecutioninfo)

---

## 1. Target the v5 package

**Mechanical.**

### Detect

```bash
grep -rn 'Include="Benday.CommandsFramework"' --include=*.csproj .
```

### Change

```xml
<!-- before -->
<PackageReference Include="Benday.CommandsFramework" Version="4.*" />

<!-- after -->
<PackageReference Include="Benday.CommandsFramework" Version="5.*" />
```

A floating `4.*` will not move to 5.x on its own, which is the point — nothing upgrades by
accident.

### Note

Do this first and then build. The compiler errors from the rest of this document are the
work list, and they are easier to read than any description of them.

---

## 2. The `--json` schema is now an object

**Mechanical**, and only if something you own reads a tool's schema. Most tools produce the
schema and never read one; if that is you, there is nothing to do — the framework writes the
new shape for you.

v4 wrote a bare JSON array of commands with nothing identifying it:

```json
[ { "Name": "greet", "Arguments": [ ... ] } ]
```

v5 wraps it:

```json
{
  "SchemaVersion": 2,
  "ApplicationName": "My CLI Tool",
  "ApplicationVersion": "v5.0.0",
  "Commands": [ { "Name": "greet", "Arguments": [ ... ] } ]
}
```

Because the old form is an **array** and the new one is an **object**, a consumer tells them
apart from the root JSON token alone. There is no negotiation and nothing to ask the tool.

### Detect

```bash
grep -rn -- '--json' --include=*.cs . | grep -v 'ArgumentFrameworkConstants'
grep -rn 'Deserialize<List<' --include=*.cs .
```

### Change

```csharp
// before
var commands = JsonSerializer.Deserialize<List<ToolCommandInfo>>(json, options);

// after
using var parsed = JsonDocument.Parse(json);

var commands = parsed.RootElement.ValueKind == JsonValueKind.Array
    ? parsed.RootElement.Deserialize<List<ToolCommandInfo>>(options)          // v4 tool
    : parsed.RootElement.GetProperty("Commands")
        .Deserialize<List<ToolCommandInfo>>(options);                          // v5 tool
```

Read `SchemaVersion` and fail loudly on a version you do not recognise rather than guessing.
`Benday.CommandsFramework.CmdUi.Services.ToolSchemaService.ParseSchema` is a worked example
of exactly this, including the error message it produces for a too-new schema.

### Gotcha

`CommandSchema` and `CommandInfo` **serialize but do not deserialize.** `CommandInfo`'s
setters are internal and `Arguments` is a collection of an interface, so
`JsonSerializer.Deserialize<CommandSchema>(json)` succeeds and hands back objects with every
property blank rather than throwing. Read the schema through your own mirror types, the way
cmdui does.

---

## 3. Command names are matched without regard to case

**Judgment**, and usually there is nothing to do.

In v4, command names and aliases were matched with ordinal comparison: `mytool GREET` did not run
`greet`. In v5 the registry is keyed with `ArgumentCollection.ArgumentNameComparer`, so command
names follow the same rule argument names have followed since v4.18.

Nothing needs changing for this to work. It matters only if your tool relied on case to tell two
commands apart.

### Detect

```bash
grep -rn '\[Command(' --include=*.cs . | grep -oiP 'Name\s*=\s*"\K[^"]+' | \
  tr 'A-Z' 'a-z' | sort | uniq -d
```

Anything this prints is a pair of command names that differed only by case and are now a
collision — the registry will refuse to build. Rename one of them.

### Note

Test assertions are the other place this shows up. A test asserting that a wrong-case name does
**not** resolve now fails, and the fix is to assert the new behavior:

```csharp
// before
Assert.Null(utility.ResolveCommandName(assembly, "MC"));

// after
Assert.Equal("my-command", utility.ResolveCommandName(assembly, "MC"));
```

---

## 4. Duplicate command names and aliases now fail at startup

**Judgment.**

In v4 two commands claiming the same name resolved to whichever the reflection scan happened to
find first, silently. In v5, building the registry throws `KnownException` for anything that makes
resolution genuinely ambiguous:

- two commands with the same name (case-insensitively — see entry 3)
- two commands claiming the same alias

Everything else that makes a command unreachable is *reported* rather than thrown, on
`CommandRegistry.Problems`: an alias that is also a real command name, an alias that collides with
a reserved keyword (`--help`, `--json`, `gui`, `quiet`), an empty alias, and a `[Command]` attribute
on a class that is not a runnable `CommandBase`. One unusable alias should not stop the other 63
commands from running.

### Detect

Add this test to the tool's test project and run it. This is the fastest way to find every
collision at once, and it is worth keeping afterwards.

```csharp
[Fact]
public void CommandsHaveNoRegistryProblems()
{
    var options = new DefaultProgramOptions
    {
        ApplicationName = "My CLI Tool",
        UsesConfiguration = true      // match what Program.cs uses
    };

    var registry = CommandRegistry.Build(options, typeof(SomeCommand).Assembly);

    Assert.Empty(registry.Problems);
}
```

If `CommandRegistry.Build` throws, that is a genuine ambiguity and the message names both
offenders. If it returns and `Problems` is non-empty, each entry says what is unreachable and why.

### Change

Judgment, one collision at a time: rename a command, drop an alias, or delete the dead one. Do not
suppress the check.

---

## 5. `ICommandProgramOptions` gained two members

**Mechanical**, and only if you implement the interface yourself. Almost nobody does — the usual
thing is to use `DefaultProgramOptions`, which already has both.

```csharp
ITextInputProvider InputProvider { get; set; }   // where commands read input from
CommandRegistry? CommandRegistry { get; set; }   // built once, then shared
```

`InputProvider` shipped in v4.20 as a get-only default interface member so that adding it broke
nothing. In v5 it is a normal settable member.

### Detect

```bash
grep -rn ': ICommandProgramOptions' --include=*.cs .
```

### Change

```csharp
public ITextInputProvider InputProvider { get; set; } = new ConsoleTextInputProvider();
public CommandRegistry? CommandRegistry { get; set; } = null;
```

`CommandRegistry` is a cache slot — the framework populates it the first time it needs the
registry. Return whatever was last set and do not build one yourself.

---

## 6. One command base class

**Mechanical**, and the compiler finds every site.

`SynchronousCommand` is deleted. `AsynchronousCommand` still exists as an `[Obsolete]` empty
subclass of the new `Command`, so code deriving from it still compiles with a warning.
`CommandAttribute.IsAsync` is `[Obsolete]` and read by nothing — the type system already says how a
command runs, and that flag could disagree with it: `IsAsync = false` on an async command built
cleanly and then threw `Could not convert type to ISynchronousCommand` at run time.

`ISynchronousCommand` and `IAsyncCommand` are deleted. Nothing needs them once there is one base
class.

### Detect

```bash
grep -rn ': SynchronousCommand\|: AsynchronousCommand\|ISynchronousCommand\|IAsyncCommand' --include=*.cs .
grep -rn 'IsAsync' --include=*.cs .
```

### Change

```csharp
// before
[Command(Name = "greet", IsAsync = false)]
public class GreetCommand : SynchronousCommand

// after
[Command(Name = "greet")]
public class GreetCommand : Command
```

Do **all three** parts: the base class, the `IsAsync` argument, and — see the next entry — the
`OnExecute` signature. A command whose work really is sequential returns `Task.CompletedTask`; it
does not need to become genuinely asynchronous.

### Note

Do entries 6 and 7 in the same pass. They both change how a command is declared, and doing them
together is one edit per command instead of two.

---

## 7. `OnExecute` takes a `CancellationToken`

**Mechanical** for the signature. **Judgment** for whether to use the token.

There was no way to stop a running command short of killing the process.

### Detect

```bash
grep -rn 'override void OnExecute()\|override Task OnExecute()\|override async Task OnExecute()' --include=*.cs .
```

### Change

For a command that was already async:

```csharp
// before
protected override async Task OnExecute()

// after
protected override async Task OnExecute(CancellationToken cancellationToken)
```

For a command that was synchronous, the body also has to return a task. Every `return;` in the
body becomes `return Task.CompletedTask;`, and a single trailing one is added:

```csharp
// before
protected override void OnExecute()
{
    WriteLine("done");
}

// after
protected override Task OnExecute(CancellationToken cancellationToken)
{
    WriteLine("done");

    return Task.CompletedTask;
}
```

An easier variant when the body already awaits something: mark it `async` and drop the return
entirely.

### Judgment: actually using the token

The signature change is mechanical. Passing the token onward is not. Do it where you are
confident — `HttpClient` calls, `Task.Delay`, anything that already takes one — and add
`cancellationToken.ThrowIfCancellationRequested()` between iterations of a long loop. Produce a list
of every command you did **not** thread it through, for a human to review. A token that is accepted
and ignored is worse than none, because it looks like cancellation works.

---

## 8. Commands return a result instead of setting `Environment.ExitCode`

**Judgment**, and usually there is nothing to do inside a command.

In v4, `Validate()` assigned `Environment.ExitCode` as a side effect, `DisplayUsage()` set failure,
and `CommandBase` saved and restored the value around nested calls to contain the damage. Nothing
in the framework touches `Environment.ExitCode` any more except `CommandsApp.Run/RunAsync`, which is
the console entry point.

`ExecuteAsync` now returns a `CommandResult`:

| Member | Meaning |
|---|---|
| `Status` | `Success`, `ValidationFailed`, `UsageDisplayed`, `Failed`, `Cancelled` |
| `IsSuccess` | true for `Success` and `UsageDisplayed` — the user asked for usage and got it |
| `ExitCode` | 0 or 1, for a caller that has to produce one |
| `Message` | why it failed, when it failed |
| `InvalidArguments` | which arguments failed validation |

### Detect

```bash
grep -rn 'Environment.ExitCode' --include=*.cs .
```

### Change

Judgment. A command that read `Environment.ExitCode` to find out whether something it called had
failed should read the returned `CommandResult` instead. A command that *set* it to report its own
failure should throw `KnownException` — the framework turns that into a failure exit code and
writes the message to the error channel.

---

## 9. `Program.cs` returns the exit code

**Mechanical.** The compiler forces this one: `Run(string[])` no longer exists.

### Detect

```bash
grep -rn 'DefaultProgram\|CommandsApp' --include=Program.cs .
```

### Change

```csharp
// before
static void Main(string[] args)
{
    CommandsApp.Create<SomeCommand>(args)
        .WithAppInfo("My CLI Tool", "https://www.example.com")
        .Run();
}

// after
static async Task<int> Main(string[] args)
{
    return await CommandsApp.Create<SomeCommand>(args)
        .WithAppInfo("My CLI Tool", "https://www.example.com")
        .RunAsync();
}
```

For a tool still on `DefaultProgram` rather than the builder:

```csharp
// before
var program = new DefaultProgram(options, assembly);
program.Run(args);

// after
var program = new DefaultProgram(options, assembly);
return await program.RunAsync(args);
```

`DefaultProgram.RunAsync` returns the code without assigning it, so a `Main` that ignores the
return value silently always exits 0. Assign it or return it.

---

## 10. `ExecuteCommand<T>` is now `ExecuteCommandAsync<T>`

**Mechanical.** One base class means one way to run a command from inside another.

### Detect

```bash
grep -rn 'ExecuteCommand<' --include=*.cs .
```

### Change

```csharp
// before
var result = ExecuteCommand<OtherCommand>(args => { args["name"] = name; });

// after
var result = await ExecuteCommandAsync<OtherCommand>(
    args => { args["name"] = name; }, cancellationToken: cancellationToken);
```

The enclosing `OnExecute` has to be `async` for this. If it was the synchronous kind, that follows
from entry 7 anyway.

---

## 11. Commands are created through `ActivatorUtilities`

**Mechanical** where it applies, and for most tools it does not apply at all.

v4 looked for one hardcoded constructor — `(CommandExecutionInfo, ITextOutputProvider)` — in two
places. That meant adding a framework parameter would break every downstream command at *run* time
rather than at compile time, because the lookup simply returned null. Commands are created through
`ActivatorUtilities.CreateInstance` now, so a command can declare the services it needs as
constructor parameters after those two.

Existing two-argument constructors keep working unchanged. There is nothing to do unless you are
using `DependencyInjectionCommand`.

### Detect

```bash
grep -rn ': DependencyInjectionCommand' --include=*.cs .
grep -rn 'GetRequiredService<' --include=*.cs .
```

### Change

`DependencyInjectionCommand` is `[Obsolete]` but still works. When you move a command off it:

```csharp
// before
public class GreetCommand : DependencyInjectionCommand
{
    public GreetCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider)
        : base(info, outputProvider) { }

    protected override Task OnExecute(CancellationToken cancellationToken)
    {
        var service = GetRequiredService<IGreetingService>();
        ...
    }
}

// after
public class GreetCommand : Command
{
    private readonly IGreetingService _GreetingService;

    public GreetCommand(
        CommandExecutionInfo info,
        ITextOutputProvider outputProvider,
        IGreetingService greetingService) : base(info, outputProvider)
    {
        _GreetingService = greetingService;
    }

    protected override Task OnExecute(CancellationToken cancellationToken)
    {
        ...
    }
}
```

`GetRequiredService<T>()` still works from any command — it moved to `CommandBase` — so changing
only the base class is a valid, smaller step.

### Gotcha: do not use an injected field from `GetArguments()`

It will be null. `CommandBase`'s constructor calls `GetArguments()`, which runs *before* any derived
field is assigned. This was already true in v4; it becomes easier to trip over now that constructor
injection is available.

### Registering an assembly's services

If Program.cs currently enumerates the services an assembly of commands needs, that assembly can
declare them itself:

```csharp
public class MyToolServiceRegistrar : IServiceRegistrar
{
    public void Register(IServiceCollection services)
    {
        services.AddSingleton<IGreetingService, GreetingService>();
    }
}
```

`CommandsApp` finds it during the assembly scan and calls it before building the provider. It has to
be a startup hook rather than a per-command one: `Microsoft.Extensions.DependencyInjection` seals
registrations at `BuildServiceProvider()`, and the provider is cached so that singletons really are
singletons — so a hook that ran any later would compile, run, and silently do nothing.

### Note on scopes

Commands are `IDisposable` now and own a dependency injection scope. `DefaultProgram` disposes the
command it ran; if you create commands yourself, dispose them. A command run through
`ExecuteCommandAsync<T>` shares the caller's scope and must not be disposed separately — awaiting it
is enough.

---

## 12. The request is split out of `CommandExecutionInfo`

**Mechanical**, and the compiler finds every site that matters.

`CommandExecutionInfo` conflated three things: what was asked for (name, arguments), the ambient
services a command runs against (options, configuration), and the framework's own bookkeeping
(`NestingDepth`). What was asked for is now a `CommandCallRequest` on
`CommandExecutionInfo.Request`.

`ExecutionInfo.CommandName` and `ExecutionInfo.Arguments` still **read**, forwarding to the request,
so command bodies do not have to change. They are get-only, so anything that **assigned** them fails
to compile — which is the point: alias resolution used to assign `CommandName` in place and destroy
the record of what the user typed. That record is `Request.RequestedName` now.

### Detect

```bash
grep -rn 'CommandName = \|\.Arguments = ' --include=*.cs . | grep -i 'executioninfo\|execinfo'
grep -rn 'new CommandExecutionInfo' --include=*.cs .
```

### Change

```csharp
// before
var info = new CommandExecutionInfo
{
    CommandName = "greet",
    Arguments = arguments,
    Options = options
};

// after
var info = new CommandExecutionInfo
{
    Request = new CommandCallRequest("greet", arguments),
    Options = options
};
```

Reading is unchanged — `ExecutionInfo.Arguments.GetStringValue(...)` and
`ExecutionInfo.CommandName` still work.

### `CommandArgumentValues` replaces the raw dictionary

`CreateCommand<T>` and `ExecuteCommandAsync<T>` take a `CommandArgumentValues` builder rather than a
`Dictionary<string, string>`. Every caller used to format its own values, and got dates and booleans
subtly wrong, because the parser expects the same formats the command line uses.

```csharp
// before
await ExecuteCommandAsync<OtherCommand>(args =>
{
    args["name"] = name;
    args["count"] = count.ToString();
    args["verbose"] = "true";
});

// after
await ExecuteCommandAsync<OtherCommand>(args => args
    .Set("name", name)
    .Set("count", count)
    .Set("verbose", true),
    cancellationToken: cancellationToken);
```

`Set` has overloads for `string`, `int`, `bool` and `DateTime`; `SetFlag(name)` is the equivalent of
typing `/name` with no value.

---

## What has not landed yet

These are planned for v5 and will get entries here as they land. Do not act on them yet.

- Parser modes (`--arg value`, `--arg=value`, the deprecated `/arg:value`).
- Multi-level commands (`mytool workitem list`).
- Declarative validation rules.
- The redefinition of quiet mode: result never suppressed, status and progress suppressed,
  errors never suppressed.

---

## Verification

Run both, in this order, from the root of the tool being upgraded:

```bash
dotnet build
dotnet test
```

`dotnet build` must report **0 errors**. Warnings that existed before the upgrade are fine;
new ones are not, and `CS0108` in particular means something in the tool is now hiding a
framework member that did not exist in v4.

For `dotnet test`, state the pass count from **before** the upgrade and compare. Exit codes
from the Microsoft.Testing.Platform runner:

| Exit code | Meaning |
|---|---|
| 0 | Everything passed |
| 2 | A test failed |
| 5 | **Zero tests ran** — treat this as a failure, not a pass |

Exit code 5 is the one that catches people out: a run that discovers no tests is not a green
run.
