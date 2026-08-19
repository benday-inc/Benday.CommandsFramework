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

## What has not landed yet

These are planned for v5 and will get entries here as they land. Do not act on them yet.

- `CommandCallRequest`, splitting the request from ambient services and bookkeeping.
- Commands activated through `ActivatorUtilities`, so they declare dependencies in their
  constructor; `DependencyInjectionCommand` becomes obsolete.
- Commands returning a result instead of setting `Environment.ExitCode`.
- One command base class, `Command`, with `SynchronousCommand` dropped and `IsAsync`
  retired.
- A `CancellationToken` threaded through the execution path.
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
