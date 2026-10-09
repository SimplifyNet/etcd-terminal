# SRP And Responsibility-Boundary Refactor Plan

## For The Implementing Agent

You are a junior coding agent. Make only the focused changes described below. Do not redesign the application, do not move code between layers without the specific reason given here, and do not split a class merely because it is long. The goal is to separate *decisions* (policy, product behavior) from *mechanisms* (I/O, rendering, persistence) while keeping behavior identical.

Before editing:

1. Read `AGENTS.md` completely and follow it. Especially: max 4 constructor parameters, primary constructors with underscored parameters, one public type per file, no Spectre/ANSI/`System.Console` outside Infrastructure, `ILocalization` is never injected (inject `ILocalizationCatalog` and read `.Current`).
2. Read the current implementation and tests named in each task before changing anything.
3. Keep the worktree's existing uncommitted changes. Do not revert or reformat unrelated files.
4. Do one task at a time. Run `dotnet build src` and `dotnet test src` after each task. Both must be green before the next task.

## Audit Result

Two independent audits were run. Both agree:

- **`src/EtcdTerminal` (domain/core)** — no SRP issue found. `ConnectionWorkflow`, `UserCapabilitiesProvider`, `ReadableKeysProvider`, `JsonKeyFlattener`, `PermissionRange` are each single-purpose. Do not touch.
- **`src/EtcdTerminal.App`** — the screens are already decomposed well: forms (`KeyCreateForm`, `PermissionTargetPrompt`, `ImportSourceReader`), commands (`IMenuCommand<T>`), loaders (`PermissionSourcesLoader`, `CancellableLoad`), reports (`ImportReport`), and orchestrating screens are separate. `KeyBrowseControl` (input → state) / `KeyBrowseList` (data + paging) / `KeyBrowseScreen` (loop) is a clean split. One confirmed violation remains: `ImportEntriesParser` (Task 1).
- **`src/EtcdTerminal.Infrastructure`** — adapters for etcd and JSON persistence are cohesive. Confirmed issues: `StatusBarRenderer` owns a content policy (Task 2), `IConnectionConfigRepository.IsNameTaken` places a uniqueness rule in a persistence port (Task 3). Custom geometry in `SpectreScreenCanvas`, `BackgroundBand`, `BlockRenderer.Halved` is an *architecture exception* that needs documentation, not a class split (Task 4).

Tasks are ordered by confidence and value. Tasks 1–3 are mechanical and safe. Task 4 is documentation first; code changes only if a Spectre-native replacement is proven.

---

## Task 1: `ImportEntriesParser` Must Only Parse

**File:** `src/EtcdTerminal.App/Screens/Keys/Import/ImportEntriesParser.cs`
**Tests:** `src/EtcdTerminal.Tests/ImportEntriesParserTests.cs`
**Caller:** `src/EtcdTerminal.App/Screens/Keys/KeyImportJsonScreen.cs`

### Problem

The parser calls `JsonKeyFlattener.Flatten`, catches exceptions, and then itself calls `Message.ShowWarning`/`ShowError` with localized text. Two reasons to change: the parsing rules, and how/where parse failures are shown. The test harness needs a `Screen`, `StatusBar`, `Header`, `FakeKeyReader` and `FakeScreenCanvas` just to test parsing — that is the smell.

### Change

1. Create `ImportParseResult.cs` in the same folder — a data-only `sealed record`:
   ```csharp
   public sealed record ImportParseResult(IReadOnlyList<KeyValuePair<string, string>> Entries, ImportParseFailure? Failure);
   ```
   and `ImportParseFailure.cs`:
   ```csharp
   public sealed record ImportParseFailure(ImportParseFailureKind Kind, string? Detail);
   ```
   and `ImportParseFailureKind.cs` with values `InvalidJson`, `NoKeys`. (Three files — one public type per file.)
   Alternative: a single `ImportParseResult` with `Entries` and `Kind`/`Detail` nullable fields is acceptable if it is simpler; keep it data-only either way.
2. Change `ImportEntriesParser` to take **no constructor dependencies** and return `ImportParseResult`. Keep the same exception mapping: `ArgumentException` → `NoKeys`; `JsonException`/`InvalidOperationException` → `InvalidJson` with `ex.Message` as `Detail`; zero entries → `NoKeys`. Do not catch anything else.
3. In `KeyImportJsonScreen.ShowAsync`, after `_parser.Parse(source)`:
   - on `NoKeys` → `_message.ShowWarning(_localizations.Current.NoKeysInJson)` and return;
   - on `InvalidJson` → `_message.ShowError(string.Format(_localizations.Current.InvalidJson, detail))` and return.
   This adds `Message` and `ILocalizationCatalog` to the screen. The screen currently has 4 dependencies (`ImportSourceReader`, `ImportEntriesParser`, `ImportPreview`, `ImportRunner`). Since the parser no longer needs DI, **make it a `static` class** with a static `Parse`, remove it from `ScreensRegistrations.cs`, and use the freed slot for `Message`. Then add `ILocalizationCatalog` — that is 5. To stay at 4, move the failure → message mapping into a tiny `ImportParseFailureNotice(Message _message, ILocalizationCatalog _localizations)` with one method `Show(ImportParseFailure failure)`. Register it as Transient. Final `KeyImportJsonScreen` ctor: `(ImportSourceReader, ImportPreview, ImportRunner, ImportParseFailureNotice)`.
4. Rewrite `ImportEntriesParserTests` as pure unit tests with no harness: assert on `Entries`/`Failure`. Add one test for a valid document. Move the "message text appears on canvas" assertions to a new small `ImportParseFailureNoticeTests` or into an existing import flow test.

### Acceptance

- `ImportEntriesParser` has no fields, no constructor, no `using EtcdTerminal.App.Components`.
- Same localized texts appear for the same inputs as before (verify with the existing strings `"Invalid JSON:"`, `"No keys found in JSON."`).
- `dotnet test src` green.

---

## Task 2: Move Status-Bar Content Policy Out Of Infrastructure

**File:** `src/EtcdTerminal.Infrastructure/Terminal/StatusBarRenderer.cs`
**Model:** `src/EtcdTerminal.Presentation/StatusBarModel.cs`
**Component:** `src/EtcdTerminal.App/Components/StatusBar.cs`
**Tests:** `src/EtcdTerminal.Tests/SpectreRenderingTests.cs` (`StatusBarRenderer_*`)

### Problem

`StatusBarRenderer.Candidates`, `Shorter`, `Fit` and the constants `EndpointTruncationWidth`, `TruncationMark` decide *which session information the user loses* on a narrow terminal (shorten endpoint → drop username → drop endpoint → drop hints → drop name). That is a product/presentation decision. The `StatusBar` component's own doc-comment says it "does not decide what to drop" — meaning nobody in Presentation owns this policy; it leaked into the renderer. `AGENTS.md` says components "choose available actions" and Infrastructure only maps models to Spectre.

Measuring whether a candidate fits (`Fits`) is correctly Infrastructure: it needs the console width and Spectre's measurer.

### Change

1. Create `src/EtcdTerminal.Presentation/StatusBarFallbacks.cs` — a **static** class in the Presentation project with:
   ```csharp
   public static IEnumerable<StatusBarModel> InPriorityOrder(StatusBarModel model)
   ```
   Move `Candidates`, `Shorter`, `EndpointTruncationWidth` and `TruncationMark` there verbatim. This is pure model → model transformation with no terminal or Spectre dependency, so it belongs in Presentation contracts. (If you prefer it in `App/Components` next to `StatusBar`, that is also acceptable — but then the renderer in Infrastructure cannot call it, so Presentation is the right home.)
2. `StatusBarRenderer.Fit` becomes:
   ```csharp
   private StatusBarModel Fit(StatusBarModel model)
   {
       var candidates = StatusBarFallbacks.InPriorityOrder(model).ToList();
       foreach (var candidate in candidates) if (Fits(candidate)) return candidate;
       return candidates[^1];
   }
   ```
   Everything else in the renderer stays.
3. Add `src/EtcdTerminal.Tests/StatusBarFallbacksTests.cs`: assert the order of candidates for a full model (first = unchanged, last = version only), that the surrogate-pair cut in `Shorter` never splits a pair, and that nothing shorter than `EndpointTruncationWidth` is produced. These are cheap, width-free tests — exactly what `AGENTS.md` asks to keep.
4. Keep the existing `StatusBarRenderer_DropsSessionFieldsInsteadOfWrapping` test as the one rendering test proving the renderer still consults the fallbacks.
5. Update the `StatusBar` doc-comment: it still does not decide what to drop; `StatusBarFallbacks` does.

### Acceptance

- `StatusBarRenderer` has no `Candidates`, `Shorter`, or truncation constants.
- `StatusBarFallbacks` has no `using Spectre.*`.
- Existing rendering tests unchanged and green; new fallback tests green.

---

## Task 3: Remove `IsNameTaken` From The Repository Port

**Port:** `src/EtcdTerminal/Configuration/IConnectionConfigRepository.cs`
**Implementations:** `src/EtcdTerminal.Infrastructure/Configuration/JsonBasedConnectionConfigRepository.cs`, `ProtectedConfigRepository.cs`
**Caller:** `src/EtcdTerminal.App/Screens/Connections/ConnectionEditor.cs:22`
**Tests:** `src/EtcdTerminal.Tests/ManageConnectionsScreenTests.cs`, `ConnectionWorkflowTests.cs`, `SessionFlowTests.cs`, any fake implementing the port.

### Problem

"A connection name must be unique, except for the one being edited" is a business rule. It lives in a persistence interface and is implemented as `LoadInstances().Any(...)` in the JSON repository — which already *also* enforces uniqueness on write (`UpdateInstance` throws on duplicate rename; `ParseInstancesStrict` throws on duplicates). The rule is in two places with two behaviors (silent `bool` vs `InvalidOperationException`). The port has a second reason to change.

### Change

1. Delete `IsNameTaken` from `IConnectionConfigRepository`, `JsonBasedConnectionConfigRepository` and `ProtectedConfigRepository`.
2. `ConnectionEditor.SaveInstanceInteractive` already receives nothing about other instances, but its caller `ManageConnectionsScreen` holds `IReadOnlyList<EtcdConnectionConfig> instances`. Two options — pick the one with the smaller diff:
   - **(a)** Pass `instances` into `ConnectionEditor.Add(instances)` / `Edit(existing, instances)` and check `instances.Any(i => i.Name == name && i.Name != existing?.Name)` inline in the editor; or
   - **(b)** Add a static helper `ConnectionNames.IsTaken(IReadOnlyList<EtcdConnectionConfig> instances, string name, string? exceptName)` in `src/EtcdTerminal/Configuration/` (domain, pure function) and call it from the editor.
   Option (b) is preferred: the rule becomes a testable domain function and the repository stays a repository.
3. Update test fakes that implement `IConnectionConfigRepository` (search for `: IConnectionConfigRepository` in `src/EtcdTerminal.Tests`).
4. Add `ConnectionNamesTests` with three cases: free name, taken name, taken-by-self-while-editing is allowed.

### Acceptance

- `IConnectionConfigRepository` has only load/add/update/remove/move members.
- `ConnectionEditor` still shows `InstanceNameTaken` for a duplicate and still lets the user keep their own name on edit.
- `dotnet test src` green.

---

## Task 4: Document (Do Not Blindly Refactor) The Custom Terminal Geometry

**Files:** `src/EtcdTerminal.Infrastructure/Terminal/SpectreScreenCanvas.cs` (nested `Pinned`, `LineWindow`, `Lines`), `BackgroundBand.cs`, `BlockRenderer.cs` (nested `Halved`), `SpectreSelectionPrompt.cs` (row composition), `ConsoleTerminalSession.cs` (raw escape sequences).

### Problem

`AGENTS.md` targets "zero application-owned ANSI generation, cursor-based drawing, display-width calculation and space-based background filling" and says any exception "needs explicit approval; do not silently weaken". These five files contain exactly those things. Each has a doc-comment explaining a Spectre gap, but there is no single place that lists the exceptions, the Spectre version they were verified against, and what would allow removing them. That is a *governance* gap, not an SRP violation — the code is in the right layer and each class is cohesive. **Do not split `SpectreScreenCanvas` into more classes**; it is the single output owner by design.

### Change

1. Read the pinned Spectre.Console version from `src/Directory.Packages.props` (or the Infrastructure `.csproj`).
2. Create `docs/rendering-exceptions.md` with one section per mechanism:
   - `BackgroundBand` — `Padder` emits unstyled padding and a trailing break (per the doc-comment). Record the Spectre type/method that was checked.
   - `SpectreScreenCanvas.LineWindow` / `Pinned` — `Live` crops to the screen but cannot show a taller body at an offset; the footer must be written at an absolute row.
   - `BlockRenderer.Halved` — equal-share column widths; `Table` gives spare width to the widest measured column.
   - `SpectreSelectionPrompt` — `SelectionPrompt` has a hardcoded `>` pointer and cannot indent rows (comment says "documented in 0.57.2").
   - `ConsoleTerminalSession` — scroll region (`CSI r`), OSC 11 background, DECSET 1007: no Spectre API.
   For each: the version checked, a one-line justification, and the condition under which it can be deleted (e.g. "Spectre adds X").
3. Add a one-line reference to that document in `AGENTS.md` under *UI Rendering* ("Approved exceptions are listed in `docs/rendering-exceptions.md`").
4. Extend `ArchitectureTests` only if trivial: there is already `InfrastructureEscapeSequencesLiveOnlyInTerminalSession`. Add a test that the allowed set of files containing `Segment.CellCount` or `new string(' '` in Infrastructure is exactly `{ BackgroundBand.cs }`, so a new file cannot silently add fill logic.
5. **Do not** change rendering code in this task. If, while reading, you find a Spectre-native replacement, write it as a proposal at the bottom of the document — it must be verified in a real terminal/PTY before any code changes, per `AGENTS.md`.

### Acceptance

- `docs/rendering-exceptions.md` exists and lists all five mechanisms with version + justification + removal condition.
- `AGENTS.md` references it.
- Architecture test pins the allow-list.
- No rendering behavior changed.

---

## Explicitly Not Tasks (Checked And Rejected)

These were examined and found acceptable. Do not change them under this plan:

- **`ConnectionEditor`** — form + validation + save + message is one use case ("add/edit a connection"). After Task 3 its only rule is a one-line call. Fine.
- **`KeyBrowseControl` / `KeyBrowseList` / `KeyBrowseScreen`** — input→state, data+paging, loop. Correct split.
- **`ListBrowser`** — key handling and paging for a view-only list, one `Show`. The `switch` is the entire feature; a split would create forwarding types, which `AGENTS.md` forbids.
- **`MultiLinePasteReader`** — paste-burst detection and line counting are both "how to read a paste". `CountLines` being `internal static` and reused by `ImportPreview` is acceptable.
- **`Spinner` / `CancellableLoad`** — cancel policy vs. "warn on cancel" are already two classes.
- **`JsonBasedConnectionConfigRepository`** — JSON mapping + strict/lenient parsing + ordering ops are all "persist the Instances section". Lenient `LoadInstances` vs strict write-path is intentional (never lose the file on a bad read, never write a bad file).
- **`ProtectedConfigRepository`** — decorator for encryption + tracking decrypt failures. The failure list is a by-product of decryption; a separate collector would only forward.
- **`EtcdConnectionHandle.ConcreteClient`** — Infrastructure-internal leak of the concrete `EtcdClient` for the auth probe. Not a layer violation; only matters if a second transport is ever added.
- **`AppRunner` resolving from `DIContainer.Current`** — it is the per-iteration scope owner and is in the composition-root neighborhood; `AGENTS.md` forbids *screens and components* from resolving, not the runner.
- **`ConsoleTerminalSession`** — already refactored; cohesive lifecycle owner.
- **`PermissionDisplay` living in `Screens/Roles` but used by `Screens/Permissions`** — ownership nit, not SRP. Move only if a third consumer appears.

---

## Verification Checklist

After all tasks:

1. `dotnet build src` — 0 errors, 0 new warnings.
2. `dotnet test src` — all green (baseline: 280 unit + 17 integration).
3. `git diff --stat` — only the files named above plus their tests/registrations.
4. Grep sanity:
   - `rg "Message|ILocalization" src/EtcdTerminal.App/Screens/Keys/Import/ImportEntriesParser.cs` → no matches.
   - `rg "Candidates|Shorter|TruncationMark" src/EtcdTerminal.Infrastructure` → no matches.
   - `rg "IsNameTaken" src` → no matches.
   - `rg "using Spectre" src/EtcdTerminal.Presentation` → no matches.
5. Report: files changed, test counts before/after, and the list of terminal/PTY scenarios *not* verified (there will be none for Tasks 1–3; Task 4 changes no rendering).

## Scope Guardrails

- Do not touch `src/EtcdTerminal` except for the `ConnectionNames` helper in Task 3.
- Do not introduce any type whose only job is to forward dependencies.
- Do not exceed 4 constructor parameters anywhere.
- Do not delete a failing test to make a task pass; fix the code or the test's harness.
- Do not claim terminal parity from `TestConsole` output.
- Do not reformat or re-order members in files you are not otherwise changing.
