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

## What has not landed yet

These are planned for v5 and will get entries here as they land. Do not act on them yet.

- A command registry replacing the seven separate assembly scans, which also makes command
  names case-insensitive.
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
