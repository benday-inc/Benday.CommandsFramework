# Design: in-process TUI for Benday.CommandsFramework

Status: **in progress**. Phases 0, 1, 2 and 3 are implemented; 4 onwards are not.

A terminal UI that any CommandsFramework tool can launch with `mytool tui` — browse the
commands, fill in a form, watch the output stream, cancel a long one. The same idea as
`cmdui`, except it runs inside the tool's own process and inside the terminal the user is
already in.

## Why this is worth doing now

The v5 work on this branch removed every obstacle. That was not a coincidence — a TUI is
named in the rationale for several of those changes — but it means the framework side is
essentially finished and what is left is the UI itself.

| What a TUI needs | What v5 already provides |
|---|---|
| Output that is not the console | `ITextOutputProvider` with `WriteLine` / `WriteStatus` / `WriteError` |
| Correct wrapping inside a pane | `ITextOutputProvider.Width` |
| Input that is not `Console.ReadLine()` | `ITextInputProvider`, `ICommandProgramOptions.InputProvider` |
| Cancel one command, not the process | `Command.ExecuteAsync(CancellationToken)` |
| Failure that is not `Environment.ExitCode` | `CommandResult` with `Status` / `ValidationFailures` |
| A progress bar | `CommandProgress`, `ITextOutputProvider.ReportProgress` |
| Enumerate commands cheaply | `CommandRegistry.Registrations` — no instantiation |
| Autocomplete | `CompletionEngine.GetCandidates(commandLine)` |
| Tell a path argument from a string | `IArgument.PathType`, `IArgument.MustExist` |
| Validate combinations as a form is filled | `ArgumentRule`, `CommandInfo.Rules` |
| Render a command line the parser will accept | `ArgumentSyntaxFormatter` |

The remaining work is a UI package, not framework surgery. The one genuinely new piece of
framework surface this proposes is `ITuiHost` plus a `tui` keyword — about 30 lines.

## Decision: in-process, not a second global tool

`cmdui` shells out: it runs `mytool --json`, renders a form, then spawns `mytool <command>
...` per run. That is the right design for a web UI that has to drive *other* tools, and the
wrong one here.

In-process wins because:

- **The schema is lossy; the objects are not.** cmdui reads `CommandInfo` mirrors and has to
  version-negotiate (`CommandSchema.SchemaVersion`, currently 3). A TUI hosted in the tool
  holds the real `IArgument` instances, calls `Validate()` directly, and can never be out of
  sync with the tool it is showing. It reads no JSON at all.
- **The process stays warm.** `ICommandProgramOptions.ServiceProvider` is built once and
  cached, so a registered singleton — a loaded solution file, an authenticated client, an
  open connection — survives across dozens of command runs in one TUI session. A CLI cannot
  do this and cmdui gets it only per spawned process.
- **Output streams.** No stdout capture, no buffering until exit. The command writes into a
  provider the TUI owns, and the pane updates as it writes.
- **Cancellation works.** A spawned process can only be killed. An in-process command gets a
  `CancellationToken`, which is exactly what v5 added it for.

The tradeoff: a tool must reference the TUI package and be rebuilt. An out-of-process
`cmdtui` would work against already-shipped tools. That is a reasonable *later* addition —
it can reuse this package's rendering over a schema-backed model — but it should not be
first, because it would throw away every advantage above.

## Project layout

```
src/Benday.CommandsFramework.Tui/          # new: Benday.CommandsFramework.Tui (NuGet library)
    ITuiHost implementation (SpectreTuiHost)
    Providers/TuiTextOutputProvider.cs
    Providers/TuiTextInputProvider.cs
    Screens/CommandBrowserScreen.cs
    Screens/CommandFormScreen.cs
    Screens/OutputScreen.cs
    Model/                                 # non-rendering view-models, unit testable
src/Benday.CommandsFramework/              # small additions only, listed below
test/Benday.CommandsFramework.Tui.Tests/   # new test project
```

A **library**, not a `PackAsTool` global tool — the opposite of `cmdui`. The tool references
it and opts in; there is nothing to install separately.

Target frameworks: `net8.0;net9.0;net10.0`, matching the core framework rather than CmdUi's
`net10.0`. A TUI is useful to every tool the framework supports, and Spectre.Console targets
netstandard2.0, so nothing forces net10.

**Spectre.Console must not become a dependency of the core package.** Core currently depends
only on `Microsoft.Extensions.*` and that is worth protecting.

## Framework additions

Four small things in `src/Benday.CommandsFramework/`:

1. **`ITuiHost`** — one method, so core can launch a TUI it does not reference:
   ```csharp
   public interface ITuiHost
   {
       Task<int> RunAsync(ICommandProgram program, CancellationToken cancellationToken = default);
   }
   ```

2. **`ICommandProgramOptions.TuiHost`** — `ITuiHost? TuiHost => null;`, a default interface
   member for the same reason `InputProvider` and `ArgumentSyntax` are: adding it breaks no
   existing implementor. `DefaultProgramOptions` declares it settable.

3. **`ArgumentFrameworkConstants.ArgumentTui = "tui"`**, and a matching entry in
   `ReservedKeywords.ForPrograms` so it is listed in usage output and, more importantly, so
   the registry's reserved-name collision check sees it. `ReservedKeywords.AllNames` picks it
   up automatically. Skipping this would let a tool define a command named `tui` that could
   never run.

4. **Dispatch in `DefaultProgram.RunAsync`**, next to `gui`:
   ```csharp
   if (args[0] == ArgumentFrameworkConstants.ArgumentTui)
   {
       if (Options.TuiHost is null)
       {
           WriteError("This tool was not built with TUI support. Add a reference to " +
                      "Benday.CommandsFramework.Tui and call .WithTui() when configuring the app.");
           return CommandFrameworkConstants.ExitCode_Failure;
       }
       return await Options.TuiHost.RunAsync(this, cancellationToken);
   }
   ```
   Deliberately **not** the `gui` behavior of offering to `dotnet tool install`. `gui` can do
   that because cmdui is a separate executable; TUI support is a compile-time reference and
   no runtime install can supply it.

The tool opts in with one line, via an extension method that lives in the TUI package:

```csharp
await CommandsApp.Create<SomeCommand>(args)
    .WithAppInfoFromAssembly()
    .WithTui()          // sets Options.TuiHost = new SpectreTuiHost()
    .RunAsync();
```

## Library: Spectre.Console

Recommended over Terminal.Gui. It maps almost one-to-one onto what the framework already
exposes — `SelectionPrompt<T>` for `AllowedValues`, `TextPrompt<T>.DefaultValue` for
`HasDefaultValue`, `ConfirmationPrompt` for booleans, `Tree` for grouped commands, `Progress`
for `CommandProgress`, `Live` for the output pane — and `Spectre.Console.Testing.TestConsole`
makes the rendering layer testable without a terminal.

Terminal.Gui buys real overlapping windows and mouse support at the cost of a much heavier
programming model. If the TUI later grows a genuinely multi-pane layout, revisit; it is the
wrong place to start.

## Architecture

### Output

```csharp
internal sealed class TuiTextOutputProvider : ITextOutputProvider
{
    public void WriteLine(string line);   // result  -> output pane
    public void WriteStatus(string line); // status  -> output pane, dimmed
    public void WriteError(string line);  // error   -> output pane, red
    public int Width { get; }             // pane width, NOT Console.WindowWidth
    public void ReportProgress(CommandProgress progress);  // -> progress bar
}
```

`Width` is the whole reason `ITextOutputProvider.Width` exists — inside a bordered pane the
console window width is wrong by however much the chrome takes.

`ReportProgress` uses `CommandProgress.IsMeasured` / `Fraction` to choose between a real bar
and an indeterminate spinner. The framework already made this decision correctly for the
console; the TUI just renders it differently.

The three channels stay visually distinct but land in one pane in write order — a user
watching a command wants chronology, not two scrolling regions.

### Input

`TuiTextInputProvider : ITextInputProvider` renders a modal prompt. This is what makes an
interactive command work inside the TUI at all: because v5 routes `CommandBase.Prompt()` and
`PromptForYesNo()` through the provider, a command that asks questions works with no
knowledge that it is inside a TUI. Under v4 it would have called `Console.ReadLine()` and
fought the renderer for the terminal.

### Running a command

```csharp
using var command = utility.GetCommand(args, assembly);   // 'using' matters: owns the DI scope
if (command is not Command runnable) { /* ... */ }

using var cts = CancellationTokenSource.CreateLinkedTokenSource(appToken);
var result = await runnable.ExecuteAsync(cts.Token);

switch (result.Status) { /* Success / ValidationFailed / UsageDisplayed / Failed / Cancelled */ }
```

Notes that are easy to get wrong:

- **Dispose the command.** It owns a DI scope. A TUI runs many commands in one process, which
  is precisely the case where the old never-disposed behavior became a real leak.
- **Never set `Environment.ExitCode`.** Only `CommandsApp.Run/RunAsync` does that. The TUI
  reads `result.Status` and shows it.
- **Never set `quiet`.** It suppresses `WriteLine()`, which is the output the TUI exists to
  display.
- Ctrl-C cancels the *command*, not the app, via a per-run `CancellationTokenSource`. Second
  press within the same run exits the TUI.

## Screens

### 1. Command browser

Built from `CommandRegistry.Registrations` — no command is instantiated, so opening the TUI
is as cheap as `--complete` (~100ms) rather than as expensive as `--json` (~300ms, which
instantiates everything).

- Group by `CommandRegistration.Category`; nest by `Path` for multi-level commands, so
  `widget list` and `widget show` appear under `widget`.
- Show `Name (alias1, alias2)` from `Aliases`.
- Fuzzy filter across name, path, aliases, category and description.
- `[CommandAlias]` presets get their own section and jump straight to a pre-filled form.
- Surface `CommandRegistry.Problems` in a corner if non-empty. It is exactly the diagnostic a
  tool author wants and today it is only visible from a unit test.

### 2. Argument form

Only here is the selected command instantiated, to call `GetArguments()`.

Widget selection, in precedence order:

| Condition | Widget |
|---|---|
| `AllowedValues.Length > 0` | selection list |
| `PathType == File` / `Directory` | path field with completion (see below) |
| `DataType == Boolean` | toggle |
| `DataType == Int32` | numeric field |
| `DataType == DateTime` | date field |
| otherwise | text field |

Check `AllowedValues` **before** `DataType`, since a file argument derives from
`StringArgument` and can carry one.

Per-field decoration: `FriendlyName` as the label falling back to `Name`; `Description` as
help text; `IsRequired && !AllowEmptyValue` as the required marker; `DefaultValue` prefilled
when `HasDefaultValue`; a provenance note when `IsFromConfig`; and when `IsDiscoverable`, a
hint that leaving it blank will search `DiscoveryPattern`.

**Live validation.** In-process, `IArgument.TrySetValue()` and `Validate()` are a direct
call, so each field validates as it is typed with no round trip. Whole-form validation calls
`Validate()` and renders `List<ValidationFailure>`, switching on `Kind` — `InvalidArgument`,
`UnknownArgument`, `RuleViolated`, `MissingConfiguration`, and the discovery failures — each
of which already carries a written message worth showing verbatim.

**Rules drive the form.** This is the feature that most justifies the whole project.
`ExactlyOneOf` renders as a radio group rather than as an error after the fact;
`MutuallyExclusive` greys out the counterpart; `RequiredTogether` marks both;
`When(x).Require(y).Forbid(z)` shows and hides fields as `x` changes. The declarative rules
exist so a form can apply them while it is being filled in — that was the stated reason for
making them declarative rather than a callback.

**Live command-line preview.** A always-visible line showing what is being built:

```
mytool widget list --environment production --verbose
```

Rendered through `ArgumentSyntaxFormatter.FormatNameValue(Options.ArgumentSyntax, name, value)`
— never by hand. CLAUDE.md is explicit that everything rendering an argument name goes through
the formatter, because a tool that tells people to type something its own parser rejects is
worse than one with no help at all. It also means the preview automatically respects a tool
configured for `Posix` or `Slash`.

Copy-to-clipboard on the preview. The point is that the TUI teaches the CLI: a user discovers
a command in the form and graduates to typing it.

### 3. Output

Streams while the command runs. Scrollback, search/filter, the `CommandResult` status on
completion, and cancel.

## Path completion: the one place the TUI does more work than a shell

`CompletionEngine.GetCandidates(commandLine)` is reusable as-is and gives the TUI command
names, argument names, and `AllowedValues` for free.

But it returns **directives** for paths — `:file:PATTERN` and `:dir` — because a shell
already knows how to complete paths and handles quoting correctly. A TUI is not a shell, so
it has to honor those directives itself: enumerate the directory, filter by the pattern, and
respect `MustExist`. That is a real chunk of work, and it is the only part of completion the
framework does not hand over.

Worth it: a fuzzy path completer inside the form is the single strongest argument for a TUI
over cmdui, since a browser file dialog cannot see the machine the tool runs on the way this
can.

## Testing

Keep every decision in a non-rendering model layer (`Model/`) and test that with the
framework's own doubles — `StringBuilderTextOutputProvider` (which separates
`GetResultOutput()` / `GetStatusOutput()` / `GetErrorOutput()` and records
`ProgressReports`) and `QueuedTextInputProvider` (whose `ReadCount` asserts how many times a
command prompted). Use `Spectre.Console.Testing.TestConsole` for the thin rendering layer.

If a test needs a terminal, the logic is in the wrong layer.

Add to the existing suite: a test asserting `tui` is in `ReservedKeywords.AllNames`, and one
asserting `CommandRegistry.Problems` flags a command that tries to claim the name.

## Phasing

Each phase is independently useful and independently shippable.

0. **Skeleton** — done. `ITuiHost`, the `tui` keyword, reserved-keyword registration,
   `.WithTui()`, a TUI that opens and exits cleanly.
1. **Command browser** — done. `TuiCommandBrowser` over `CommandRegistry.Registrations`,
   grouped by `Category` and nested by `Group`, fuzzy filter across name, path, aliases,
   category and description, `[CommandAlias]` presets in their own section, `Problems`
   surfaced. Instantiates nothing.
2. **Form + preview** — done. `TuiField` widget mapping, per-field validation that never
   commits a value the argument refuses, whole-form validation through the command's own
   `Validate()`, and a live command-line preview rendered through `ArgumentSyntaxFormatter`
   with copy-to-clipboard.
3. **Execute** — done. `TuiCommandRunner` builds the command from the command line the
   preview shows and runs it in process; `TuiTextOutputProvider` collects the three output
   channels in write order and announces each line as it is written; `TuiTextInputProvider`
   turns a command's question into a prompt; progress is redrawn in place on a terminal;
   Ctrl-C cancels the command rather than the interface; the run ends in a `TuiRunResult`
   and the interface goes back to the form.
4. **Completion** — wire `CompletionEngine` into the form, implement the path directives.
5. **Rules** — radio groups, conditional fields, greying out.

Later, unranked: generate a `[CommandAlias]` snippet from a filled form (closing the loop on
a feature that already exists); a config screen backed by `check-configuration`, which already
reports `IsComplete` and `Requirements` when run in process; run history; queueing several
commands into one log.

## Decisions

These were open questions while this was a proposal. All five are settled.

1. **Spectre.Console**, as recommended. `Spectre.Console.Testing.TestConsole` is what the
   rendering tests run against.
2. **TFMs are `net8.0;net9.0;net10.0`**, matching the core framework rather than CmdUi.
3. **Package name is `Benday.CommandsFramework.Tui`.**
4. **`tui` ships in v5.1**, the version already on this branch. Core's release notes carry it
   and the TUI package ships at the same version.
5. **In-process only. There will be no out-of-process `cmdtui`.** So the rendering layer sits
   directly on `IArgument` and `CommandRegistry` rather than behind a schema-shaped model —
   which is what makes live per-field validation a direct `TrySetValue()` / `Validate()` call
   with nothing in between. `Model/` still exists and still holds every decision, but it holds
   view-models over the real objects, not a mirror of the JSON schema.

## Notes for whoever implements this

- Build and test with `dotnet build` / `dotnet test`. Do not delete `global.json` — without
  it `dotnet test` silently runs zero tests and still exits 0.
- `--nologo` is rejected in MTP mode. `--configuration`, `--no-build`, `--verbosity`,
  `--framework` are fine. Exit codes: 0 pass, 2 failure, 5 zero tests ran.
- Adding anything to `IArgument` or `CommandInfo` means mirroring it into
  `src/Benday.CommandsFramework.CmdUi/Models/`. This design deliberately adds nothing to
  either, so cmdui should need no changes.
- Small doc drift found while writing this: CLAUDE.md's "Execution Contract" section lists
  `CommandResult.InvalidArguments`; the actual property is `ValidationFailures`. Fixed in
  CLAUDE.md.

## Corrections found while implementing

Everything above was written without a working .NET SDK, so none of it had been compiled.
Almost all of it held. What did not:

- **`CommandBase.Validate()` is `protected`.** The design has the form "call `Validate()`
  directly", which does not compile from outside the command. Widening it would break any
  tool overriding it as `protected override`, so v5.1 adds a public
  `CommandBase.ValidateArguments()` that calls it. Calling it repeatedly is safe and is the
  point: the first call applies configuration and command line values and then
  `SetValuesFromExecutionInfo()` is a no-op, so a value typed into a field is never
  overwritten by a later validation.
- **A command's `Arguments` are empty until it is validated.** `GetCommand()` parses the
  command line onto `ExecutionInfo.Request`; the values only reach the argument definitions
  in `Validate()`. So a form has to validate once when it opens, before anything reads a
  field's value.
- **`CommandRegistry.BuildFromTypes` takes no options.** It is
  `BuildFromTypes(IEnumerable<Type>, Assembly? builtInAssembly, Assembly? primaryAssembly)`.
- **`ArgumentSyntaxFormatter` is a class of extension methods on `ArgumentSyntax`.** The
  design's `ArgumentSyntaxFormatter.FormatNameValue(syntax, name, value)` compiles, but every
  caller in the framework writes `syntax.FormatNameValue(name, value)`.
- **The `TuiTextOutputProvider` sketch is missing two members.** `WriteLine()` with no
  arguments and `Write(string)` are abstract on `ITextOutputProvider`; only `WriteStatus`,
  `WriteError`, `Width` and `ReportProgress` are defaulted. Phase 3 needs all six.
- **`ArgumentRule` has `Describe()`, not a `Description` property.**
- Separately, adding `tui` to the reserved keywords turned up a real defect that had nothing
  to do with the TUI: `CommandAttributeUtility.GetCommandNameProblems()` carried its own hand
  written list of three reserved names instead of reading `ReservedKeywords.AllNames`, so it
  was already blind to `completion`, `quiet` and `--complete`, and it checked aliases against
  reserved names but never command names. `CommandRegistry.GetProblems()` had always done
  both. Fixed.

## Corrections found while implementing phase 3

- **A command's output provider comes from the options it is built with**, not from anything
  settable afterwards, so running inside the interface means building the command with a
  different set of options. That is `TuiProgramOptions`, which forwards everything to the
  tool's real options except the two providers. Forwarding rather than copying is the point:
  the registry and the service provider are caches, and a copy would quietly build a second
  of each — and a second service provider means singletons that are not.
- **The design's `TuiTextOutputProvider` sketch is missing the case that makes prompting
  work.** `CommandBase.Prompt()` writes the question through `Write()`, with no line ending,
  and then reads. A provider that only handles whole lines strands the question as half a
  line above an unlabelled prompt. So the provider holds the partial line and the input
  provider takes it as the prompt's label — which is the whole of what makes an interactive
  command work in here.
- **A partial line is only absorbed by a result line.** Anything on another channel ends it
  first and stays on its own channel, the same way separate streams behave for a console: an
  error is not part of the sentence the result was half way through.
- **The runner catches every exception, which the console entry point deliberately does
  not.** `DefaultProgram` lets an unexpected exception end the process; that is correct when
  the process runs one command and exits, and wrong when the process is an interface with a
  screen full of work in it. A command that throws costs the user that command.
- **A fresh command is built for each run**, from `TuiCommandForm.GetCommandLineTokens()`,
  rather than running the instance the form edits. It costs one instantiation and buys two
  things: each run gets its own dependency injection scope, disposed when it ends, and the
  preview is verifiable rather than decorative — what runs is what the preview says, parsed
  by the parser that would have parsed it had it been typed.
- **`Environment.ExitCode` is never assigned.** Asserted by a test, because the failure it
  guards against is silent: the interface would exit with the code of whichever command
  happened to fail in it.
