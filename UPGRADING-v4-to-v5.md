# Upgrading from Benday.CommandsFramework v4 to v5

This document is written to be handed to an AI coding assistant along with the source of a
tool built on Benday.CommandsFramework v4, so the upgrade can be performed without anyone
having to remember what changed. It is equally readable by a person.

**Status: in progress.** v5 is not finished. Entries are added as each breaking change
lands, so this document is never reconstructed from memory. The
[What has not landed yet](#what-has-not-landed-yet) section says what is still coming.

**Entries 1 through 13 are not optional busywork.** Each is a change that will either stop the
tool compiling or change what it does at run time. Entry 1 is the one that makes the compiler
produce most of the work list, so do it first and let the errors guide you.

**Entry 14 is the exception** — the POSIX argument syntax is additive, and a tool that skips it
keeps working. It is in the numbered list rather than the "do not have to adopt" list below
because it does break tests that assert on usage text, and because a tool's own docs and scripts
now describe a deprecated syntax. Read it; most of it is a search and replace on documentation.

**The compiler does not find everything.** Entries 3, 8, 9 and 14 can all leave a tool that
builds clean with zero warnings and behaves differently than it did in v4. Entry 9 is the one
most often missed: a tool that uses the fluent builder still compiles, and silently always
exits 0. Read those four even when the build is green.

**What v5 adds that you do not have to adopt**, but probably want to: multi-level command names
(`Group` on `[Command]`), declarative argument rules, single-match discovery, progress
reporting, shell completion, `check-configuration`, and the status/error output channels. None
of these break anything; each has a section in the README.

---

## How to use this document

0. **Record the baseline before touching anything.** [Verification](#verification) compares
   against it, and once the upgrade starts you cannot get it back. Write down the error count,
   the warning count, and the number of tests that pass:

   ```bash
   dotnet build 2>&1 | tail -3
   dotnet test
   ```

   Confirm the test run reports a **total**, not just "Determining projects to restore" — see
   [Verification](#verification) for why a run that reports nothing is not a baseline.

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

### A Detect command that fails looks exactly like one that finds nothing

Both print nothing, and step 2 says to skip an entry that finds nothing. So a broken shell
quietly turns this document into a no-op. Before starting, confirm the shape of these commands
works at all:

```bash
grep -rn 'class' --include='*.cs' . | head -1      # must print a line
```

Two defaults on macOS break them:

- **zsh** expands an unquoted `--include=*.cs` before `grep` ever sees it, and aborts the whole
  command with `no matches found`. Every glob in this document is quoted for that reason. If
  you retype one, keep the quotes.
- **BSD `grep`** has no `-P`, so a PCRE pattern fails with `invalid option -- P`. No Detect
  command in this document needs `-P` any more.

And one that bites when searching for a pattern starting with a dash: `--` ends option parsing,
so anything after it is a filename, including `--include`. Put `--include` first.

```bash
grep -rn -- '--json' --include='*.cs' .    # --include treated as a file: "No such file"
grep -rn --include='*.cs' -- '--json' .    # correct
```

Run them from the root of the tool being upgraded. If one errors, fix the command — do not
conclude that the entry does not apply.

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
13. [13. Validation returns failures, not arguments](#13-validation-returns-failures-not-arguments)
14. [14. The POSIX argument syntax, and `/arg:value` is deprecated](#14-the-posix-argument-syntax-and-argvalue-is-deprecated)

---

## 1. Target the v5 package

**Mechanical.**

### Detect

```bash
grep -rn 'Include="Benday.CommandsFramework"' --include='*.csproj' .
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

A tool that pins exact versions instead — `Version="4.17.0"` — moves to `Version="5.0.0"`.
Keep whichever style the tool already uses. Switching a pinned reference to a floating one
changes how the tool takes every future update, and that is not a mechanical step.

Change **every** project that references the package, not just the one holding `Program.cs`.
A test project left on 4.x restores a second copy of the framework and produces errors that
read as though the upgrade half-failed.

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
  "SchemaVersion": 3,
  "ApplicationName": "My CLI Tool",
  "ApplicationVersion": "v5.1.0",
  "ArgumentSyntax": "Both",
  "Commands": [ { "Name": "greet", "Arguments": [ ... ] } ]
}
```

`SchemaVersion` was 2 in v5.0 and is 3 from v5.1, which added `ArgumentSyntax` — see
[entry 14](#14-the-posix-argument-syntax-and-argvalue-is-deprecated). Both are objects, so the
root-token test below is unaffected.

Because the old form is an **array** and the new one is an **object**, a consumer tells them
apart from the root JSON token alone. There is no negotiation and nothing to ask the tool.

### Detect

```bash
grep -rn --include='*.cs' -- '--json' . | grep -v 'ArgumentFrameworkConstants'
grep -rn 'Deserialize<List<' --include='*.cs' .
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

Use the registry test from
[entry 4](#4-duplicate-command-names-and-aliases-now-fail-at-startup). `CommandRegistry.Build`
keys the registry case-insensitively, so two commands differing only by case are precisely the
ambiguity it throws on, and the message names both offenders. Rename one of them.

**Do not grep for `Name = "..."` to find these.** A `[Command]` attribute is commonly written
across several lines:

```csharp
[Command(
    Name = "mail",
    Description = "Launch the UI.")]
```

Any pattern anchored to the `[Command(` line matches a bare `[Command(` and extracts no name at
all, so it reports "no duplicates" for a tool that has them. Building the registry is the only
detection here that cannot quietly return a false negative.

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

v5.1 added two more — `ArgumentSyntax` and `WarnOnDeprecatedArgumentSyntax` — but both are
get-only **default interface members**, so an implementation that does not declare them still
compiles and gets the defaults. Declare them settable only if you want to change them:

```csharp
public ArgumentSyntax ArgumentSyntax { get; set; } = ArgumentSyntax.Both;
public bool WarnOnDeprecatedArgumentSyntax { get; set; } = true;
```

### Detect

```bash
grep -rn ': ICommandProgramOptions' --include='*.cs' .
```

### Change

```csharp
public ITextInputProvider InputProvider { get; set; } = new ConsoleTextInputProvider();
public CommandRegistry? CommandRegistry { get; set; } = null;
```

`CommandRegistry` is a cache slot — the framework populates it the first time it needs the
registry. Return whatever was last set and do not build one yourself.

### Opportunity: commands that prompt for input

`InputProvider` exists so that a command that prompts reads through it instead of calling
`Console.ReadLine()` directly. That is what makes an interactive command testable — a test
hands the command a `QueuedTextInputProvider` and drives it without a console — and what lets
a command run somewhere that has no console at all. Nothing breaks if you skip this, but the
upgrade is when to find the sites.

### Detect

```bash
grep -rn 'Console.ReadLine\|Console.ReadKey' --include='*.cs' .
```

### Change

`CommandBase` already wraps the provider, so this is usually shorter than what it replaces:

```csharp
// before
Console.Write("Name: ");
var name = Console.ReadLine()?.Trim();

// after
var name = Prompt("Name: ");
```

`ReadLine()`, `Prompt(prompt)` and `PromptForYesNo(prompt, defaultAnswer)` are all on
`CommandBase`. `ITextInputProvider` exposes `ReadLine()` only, so a `Console.ReadKey()` — a
"press any key to continue" — has no equivalent and stays as it is.

---

## 6. One command base class

**Mechanical**, and the compiler finds every site.

`SynchronousCommand` is deleted. `AsynchronousCommand` still exists as an `[Obsolete]` empty
subclass of the new `Command`, so code deriving from it still compiles — with a `CS0618`
warning that the tool did not have before, which [Verification](#verification) treats as a
failure. It exists so that a large tool can be moved a few commands at a time, not so that the
upgrade can stop half way. Finish the move in this pass.
`CommandAttribute.IsAsync` is `[Obsolete]` and read by nothing — the type system already says how a
command runs, and that flag could disagree with it: `IsAsync = false` on an async command built
cleanly and then threw `Could not convert type to ISynchronousCommand` at run time.

`ISynchronousCommand` and `IAsyncCommand` are deleted. Nothing needs them once there is one base
class.

### Detect

```bash
grep -rn ': SynchronousCommand\|: AsynchronousCommand\|ISynchronousCommand\|IAsyncCommand' --include='*.cs' .
grep -rn 'IsAsync' --include='*.cs' .
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
grep -rn 'override void OnExecute()\|override Task OnExecute()\|override async Task OnExecute()' --include='*.cs' .
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
grep -rn 'Environment.ExitCode' --include='*.cs' .
```

### Change

Judgment. A command that read `Environment.ExitCode` to find out whether something it called had
failed should read the returned `CommandResult` instead. A command that *set* it to report its own
failure should throw `KnownException` — the framework turns that into a failure exit code and
writes the message to the error channel.

---

## 9. `Program.cs` returns the exit code

**Mechanical** once you find it, but **the compiler will not find it for you.** This is the
most commonly missed entry in the document — read it even if the build is clean.

The static `CommandsApp.Run(string[])` is gone, so a tool that called *that* fails to compile.
But the fluent builder's instance `Run()` still exists and still returns `int`. A `Program.cs`
shaped like the "before" example below therefore compiles against v5 with **zero errors and
zero warnings** — and, because `static void Main` discards the returned `int`, the tool then
always exits 0. Every failure reports success to whatever called it: a shell script, a
pipeline, a scheduled task.

Detect is the only thing that catches this. Run it even if nothing else in this document
applied.

### Detect

```bash
grep -rn 'DefaultProgram\|CommandsApp' --include='Program.cs' .
```

Then confirm the entry point returns what it gets. Both of these are the bug:

```bash
grep -rn 'static void Main' --include='Program.cs' .
grep -rn '\.Run()' --include='Program.cs' .
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

Nothing in the framework assigns `Environment.ExitCode` on your behalf any more (see
[entry 8](#8-commands-return-a-result-instead-of-setting-environmentexitcode)), so a `Main`
that ignores the returned code silently always exits 0. Assign it or return it.

### Verify it, because the build cannot

```bash
mytool no-such-command > /dev/null 2>&1; echo $?   # must be 1
mytool --help          > /dev/null 2>&1; echo $?   # must be 0
```

A tool that prints an error and exits 0 has this entry outstanding, whatever the build says.

---

## 10. `ExecuteCommand<T>` is now `ExecuteCommandAsync<T>`

**Mechanical.** One base class means one way to run a command from inside another.

### Detect

```bash
grep -rn 'ExecuteCommand<' --include='*.cs' .
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
grep -rn ': DependencyInjectionCommand' --include='*.cs' .
grep -rn 'GetRequiredService<' --include='*.cs' .
```

### Change

`DependencyInjectionCommand` is `[Obsolete]` but still works — at the cost of a new `CS0618`,
which [Verification](#verification) treats as a failure, so move off it in this pass.

A command that never called `GetRequiredService<T>()` needs nothing but the base class name
changed; that case is a one-word edit and there is no reason to defer it. When you move a
command that does use it:

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
grep -rn 'CommandName = \|\.Arguments = ' --include='*.cs' . | grep -i 'executioninfo\|execinfo'
grep -rn 'new CommandExecutionInfo' --include='*.cs' .
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

## 13. Validation returns failures, not arguments

**Mechanical** where it applies. Most commands never touch validation and have nothing to do.

`CommandBase.Validate()` returned `List<IArgument>`, so every failure had to be expressed as an
argument. `UnknownArgument` -- a fake `IArgument` invented to stand for a key the command does not
define -- was the proof that this was too narrow, and a rule about the *combination* of arguments
has no single argument to blame either. Validation returns `List<ValidationFailure>` now, and
`UnknownArgument` is deleted.

### Detect

```bash
grep -rn 'override.*Validate()\|OnValidationFailure\|DisplayValidationSummary\|UnknownArgument\|InvalidArguments' --include='*.cs' .
```

### Change

```csharp
// before
protected override List<IArgument> Validate()
protected override void OnValidationFailure(List<IArgument> validationResult)
protected override void DisplayValidationSummary(List<IArgument> invalidArguments)

// after
protected override List<ValidationFailure> Validate()
protected override void OnValidationFailure(List<ValidationFailure> validationResult)
protected override void DisplayValidationSummary(List<ValidationFailure> failures)
```

A `ValidationFailure` has a ready-made `Message`, so a custom summary usually gets simpler:

```csharp
// before
foreach (var item in invalidArguments)
{
    WriteLine(item is UnknownArgument
        ? $"Unknown argument: {item.Name}"
        : $"{item.Name} is not valid or missing");
}

// after
foreach (var failure in failures)
{
    WriteLine(failure.Message);
}
```

`CommandResult.InvalidArguments` is `CommandResult.ValidationFailures` for the same reason. Read
`failure.ArgumentNames` for the names, `failure.Argument` for the argument itself when there is one,
and `failure.Kind` to tell the three cases apart.

### Opportunity: delete hand written checks

Requirements about combinations of arguments are usually enforced by `if` statements at the top of
`OnExecute()`. Those can become rules, which get enforced before the command runs, printed in the
usage output, and shipped in the `--json` schema:

```csharp
// before, in OnExecute()
if (HasValue(token) && HasValue(windowsauth)) throw new KnownException("Cannot set both");
if (!HasValue(token) && !HasValue(windowsauth)) throw new KnownException("You must set either");

// after, in GetArguments()
args.ExactlyOneOf("token", "windowsauth");
```

Also available: `AtLeastOneOf`, `MutuallyExclusive`, `RequiredTogether`, and
`When(arg, value).Require(...).Forbid(...)`.

This one is **judgment**: finding the checks is a search, and deciding that a given `if` is really a
rule about arguments rather than about the state of the world is not something to guess at. List the
candidates for a human rather than converting them silently.

---

## 14. The POSIX argument syntax, and `/arg:value` is deprecated

**Judgment**, and **nothing here is required to make the tool work.** This is the one entry in
this document that is not a break: `/arg:value` still parses, and a tool that ignores this entry
entirely keeps running. Two things do need attention, and both are found by the Detect commands
below — a test that asserts on usage text, and the tool's own docs and scripts.

Arguments are now typed the way every modern cross platform CLI types them:

```bash
mytool deploy --environment production      # space
mytool deploy --environment=production      # equals
mytool deploy --environment:production      # colon
mytool deploy --verbose                     # flag (a Boolean with AllowEmptyValue)
mytool deploy -e production                 # an argument alias as a short option
mytool commit -- --not-an-option            # end of options
```

The default, `ArgumentSyntax.Both`, accepts both forms, renders the POSIX one everywhere, and
prints a deprecation warning on the **diagnostic channel** when a slash argument is used.

### Detect

```bash
# 1. tests and code that assert on the old rendering -- these will fail
grep -rn --include='*.cs' -E '"\[?/[A-Za-z][A-Za-z0-9_-]*[:"]' . | grep -v '/bin/\|/obj/'

# 2. the tool's own docs, scripts and README
grep -rn --include='*.md' --include='*.sh' --include='*.ps1' \
    -E '(^|[ `"])/[A-Za-z][A-Za-z0-9_-]*:[A-Za-z0-9]' .

# 3. anything that builds a command line for this tool or reads its schema
grep -rn --include='*.cs' -E '\$?"/\{|ArgumentList\.Add' .
```

Hit 1 is the only one that fails a build. Hits 2 and 3 are correctness of a different kind:
documentation that tells people to type the deprecated form, and code that generates it.

Each grep must print something or print nothing for a reason you understand — a grep that
silently matches nothing because of a quoting problem looks exactly like a clean tool. Confirm
the shape works by running grep 1 against a file you know contains `"/something:"`.

### Change

**Hit 1 — tests.** Usage output now renders `--arg1 <String>` where it rendered `/arg1:String`,
and the reserved `quiet` keyword is listed as `--quiet` rather than as a bare word nobody could
type. Update the expected strings:

```csharp
// before
Assert.Contains("/environment:String", usageText);
Assert.Contains("[/verbose", usageText);

// after
Assert.Contains("--environment <String>", usageText);
Assert.Contains("[--verbose]", usageText);
```

A fixture that is not ready to move can pin itself instead, and keep asserting the old strings:

```csharp
var options = new DefaultProgramOptions { ArgumentSyntax = ArgumentSyntax.Slash };
```

Tests that *pass* arguments (`"/name:Ben"`) do not need to change — that syntax still parses.

**Hit 2 — docs and scripts.** Mechanical: `/name:value` becomes `--name value`, and `/flag`
becomes `--flag`. Watch for absolute paths, which the same pattern matches —
`CsvReader.FromFile("/path/to/data.csv")` and `2>/dev/null` must be left alone.

**Hit 3 — anything generating a command line.** Emit `--name=value` rather than `--name value`
when the result is passed as a single element of an argument array, so the pair survives as one
token:

```csharp
// before
psi.ArgumentList.Add($"/{name}:{value}");

// after
psi.ArgumentList.Add($"--{name}={value}");
```

If you are driving *another* tool rather than your own, read its syntax from its schema rather
than assuming — see [Schema consumers](#schema-consumers) below.

### Adopting it fully

Optional, and at your own pace:

1. Update the tool's docs and scripts to `--name value` (hit 2 above).
2. Leave `ArgumentSyntax` at `Both` while both forms are in circulation.
3. Set `ArgumentSyntax = ArgumentSyntax.Posix` once you are ready to stop accepting slash.

To keep existing scripts quiet while you migrate them:

```csharp
options.WarnOnDeprecatedArgumentSyntax = false;
```

To opt out of the whole change and keep the v4 syntax:

```csharp
options.ArgumentSyntax = ArgumentSyntax.Slash;
```

That also turns the deprecation warning off — a program that has said what it wants does not
need to be told.

### Why you would want `Posix` rather than `Both`

The slash syntax cannot tell a switch from a single-segment absolute path. `/tmp` has one slash
and no colon, so it has always been read as a flag named `tmp` rather than as the path it plainly
is — the parser guesses from the slash count, and one slash means switch:

```bash
mytool build /tmp                # v4 and Both: a flag named 'tmp'
mytool build /tmp                # Posix: the positional value '/tmp'
```

Turning the slash syntax off is what removes the guess. Until then, a leading `./tmp` avoids it.

### Gotcha: an unrecognized argument now eats the token after it

`--name value` is two tokens, so the parser has to decide whether `value` belongs to `--name`.
It decides from the argument definitions: a Boolean with `AllowEmptyValue` is a flag and takes
nothing, everything else takes the next token — **including an argument the command does not
define.** So a typo'd `--nmae value` swallows `value` instead of letting it land in a positional
slot, and with `StrictArgumentValidation` off nothing says so.

```csharp
options.StrictArgumentValidation = true;
```

That is what turns the typo into a validation failure. It is worth turning on regardless, and
this entry makes it worth more.

### Schema consumers

The `--json` schema is version **3** and carries `ArgumentSyntax`. Anything that builds a command
line for a tool it does not own must read it: a version 2 tool accepts only slash, and a version 3
tool may accept only POSIX. **The property's absence means slash**, not the framework's own
default — a schema without it came from a tool that predates this change.

```csharp
var syntax = document.SchemaVersion < 3
    ? "Slash"                       // predates the property
    : document.ArgumentSyntax;
```

cmdui does this already. Update it alongside the tool:

```bash
dotnet tool update -g Benday.CommandsFramework.CmdUi
```

---

## What has not landed yet

Planned, not built. Do not act on these; they will get entries here when they land.

- **The redefinition of quiet mode** — result never suppressed, status and progress
  suppressed, errors never suppressed. Today `quiet` still suppresses `WriteLine()`, which is
  the v4 behavior. The new `WriteStatus()` and `WriteError()` channels are already in place, so
  moving a command's chatter onto `WriteStatus()` now is a safe step in the right direction.

## Things that changed without breaking anything

No action needed. Listed so that a diff of behavior does not look like a bug.

- Usage output lists the framework's reserved names (`--help`, `--json`, `gui`, `completion`,
  `quiet`) in an `** ALSO AVAILABLE **` section. Before, nothing mentioned them anywhere.
- The command list gains commands the tool did not declare. `check-configuration` is new in v5
  and, like `get-configuration`, `set-configuration` and `remove-configuration`, is registered
  automatically whenever the program sets `UsesConfiguration = true`. A tool that diffs its
  command-list output against v4 sees several entries it did not add. They are built-in
  registrations, not a scan picking up something it should not have.
- Usage output no longer prints a blank line for an application name, version or website that
  was never set.
- Usage text wraps against `ITextOutputProvider.Width` rather than `Console.WindowWidth`. The
  redirected fallback is 60 everywhere; the program's command list used to use 80.
- A `[Command]` attribute on a class the framework cannot run is skipped and reported rather
  than crashing `--json`.
- `AllowedValues` on a non-string argument now throws instead of being silently ignored.
- The `--json` schema gained `PathType`, `MustExist`, `Group`, `Rules`, `DiscoveryPattern`,
  `DiscoveryDirectory`, `DiscoveryIsRecursive` and `ArgumentSyntax`, and lost `IsAsync`.
- Shell completion offers `--name` rather than `/name:`. The value is the next word, so a second
  TAB completes it — which also avoids putting an `=` inside the word being completed, something
  bash splits on by default.

---

## Verification

Run both, in this order, from the root of the tool being upgraded:

```bash
dotnet build
dotnet test
```

Then run the tool itself, which catches things a build cannot:

```bash
mytool                      # the command list, with any groups
mytool --json | head        # an object with SchemaVersion, not a bare array
mytool somecommand --help   # usage, with ** ALSO AVAILABLE ** at the end
```

Usage output must now show `--somearg <String>` rather than `/somearg:String`, and must list
`--quiet` in the `** ALSO AVAILABLE **` section. If it still shows the slash form, something is
setting `ArgumentSyntax` to `Slash`.

Then confirm both syntaxes reach the same argument, which is what says
[entry 14](#14-the-posix-argument-syntax-and-argvalue-is-deprecated) landed without breaking
anything that was already working:

```bash
mytool somecommand --somearg value      # must work
mytool somecommand --somearg=value      # must work
mytool somecommand /somearg:value       # must work, and warn on stderr
mytool somecommand /somearg:value 2>/dev/null   # the warning must not appear on stdout
```

The last one matters more than it looks: a deprecation warning written to stdout lands inside
the output of any command being piped or redirected.

And check the exit codes, which is the only way to confirm
[entry 9](#9-programcs-returns-the-exit-code) actually landed — a `Program.cs` that still
discards the result builds clean and passes every check above:

```bash
mytool no-such-command > /dev/null 2>&1; echo $?   # must be 1
mytool --help          > /dev/null 2>&1; echo $?   # must be 0
mytool                 > /dev/null 2>&1; echo $?   # must be 1 -- no command named
```

If all three print 0, the exit code is not being returned.

And add these two tests, which turn the framework's own build-time checks into part of the
suite. Both were available in v4 and called by nothing:

```csharp
/// <summary>
/// Mirrors what Program.cs configures, so the registry under test is the one the tool
/// actually builds at run time. UsesConfiguration in particular changes which commands
/// are registered -- a test that leaves it at the default is checking a different registry
/// than the tool has.
/// </summary>
private static DefaultProgramOptions GetProgramOptions()
{
    return new DefaultProgramOptions
    {
        ApplicationName = "My CLI Tool",
        UsesConfiguration = true,
        StrictArgumentValidation = true,
        ConfigurationFolderName = "mytool"
    };
}

[Fact]
public void CommandsHaveNoRegistryProblems()
{
    var registry = CommandRegistry.Build(
        GetProgramOptions(), typeof(SomeCommand).Assembly);

    // print them -- "Assert.Empty failed, collection had 3 items" does not say which
    foreach (var problem in registry.Problems)
    {
        Console.WriteLine(problem);
    }

    Assert.Empty(registry.Problems);
}

[Fact]
public void CommandsHaveNoArgumentProblems()
{
    var utility = new CommandAttributeUtility(GetProgramOptions());

    var problems = utility.GetArgumentProblems(typeof(SomeCommand).Assembly);

    foreach (var problem in problems)
    {
        Console.WriteLine(problem);
    }

    Assert.Empty(problems);
}
```

Point them at a type in the **tool's** command assembly, not the test assembly —
`typeof(SomeCommand).Assembly` has to resolve to the assembly the tool scans at run time.

`dotnet build` must report **0 errors**. Warnings that existed before the upgrade are fine;
new ones are not, and `CS0108` in particular means something in the tool is now hiding a
framework member that did not exist in v4.

For `dotnet test`, state the pass count from **before** the upgrade and compare. Exit codes
from the Microsoft.Testing.Platform runner:

| Exit code | Meaning |
|---|---|
| 0 | Everything passed — **only if the run also reported a test total** |
| 2 | A test failed |
| 5 | **Zero tests ran** — treat this as a failure, not a pass |

Exit code 5 is the one that catches people out: a run that discovers no tests is not a green
run.

**A green exit code is not enough on its own.** Depending on the runner and how the test
project is configured, `dotnet test` can run nothing, print nothing beyond
`Determining projects to restore`, and exit **0** — a worse version of exit code 5, because
it looks like success. Confirm you can see the count:

```bash
dotnet test 2>&1 | grep -i 'total\|passed\|failed'
```

If that prints nothing, `dotnet test` is not running the suite and the comparison against your
baseline is meaningless. Run the test project's own executable instead, which always reports:

```bash
dotnet build
./test/MyTool.UnitTests/bin/Debug/net10.0/MyTool.UnitTests
```

Whichever you use, use the **same one** for the before and after counts.
