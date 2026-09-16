# Architecture Review — SOLID Todo List

Review date: 2026-09-16. Baseline: `dotnet build src/EtcdTerminal.slnx` = 0 warnings, `dotnet test` = 7/7 passing.

## How to use this file (instructions for the implementing agent)

- Read `AGENTS.md` first and follow every rule in it (member ordering, blank-line rules, brace rules, primary-constructor `_underscore` parameters, one type per file, file-scoped namespaces, collection expressions).
- Work through tasks **in order**. Tasks in later phases assume earlier ones are done.
- Do exactly one task per commit. Do not start the next task until the current one passes verification.
- After every task run, from repo root:
  ```
  dotnet build src/EtcdTerminal.slnx -v q
  dotnet test src/EtcdTerminal.Tests/EtcdTerminal.Tests.csproj -v q
  ```
  Both must succeed with 0 warnings and 0 failures.
- Never change user-visible behaviour unless the task says so. If a task says "behaviour must be identical", it means the rendered screen output and key handling must not change.
- Tasks marked **DECISION** must not be implemented without explicit approval from the repo owner; they are listed for completeness.
- When a task says "register in IoC", edit `src/EtcdTerminal.App/Setup/IocRegistrations.cs`.
- When a task says "update AGENTS.md", keep edits minimal and factual.
s
---

## Progress

- [x] T0.1 Add a `FakeTerminal` test double
- [x] T0.2 Strengthen architecture tests
- [x] T1.1 `Menu` must select by index, not by label
- [x] T1.2 Remove `Menu.StripMarkup`
- [x] T1.3 Stop mixing instance names and fixed actions in one `string` id
- [x] T1.4 Unify error contracts on `IEtcdUserAdmin` / `IEtcdRoleAdmin`
- [x] T1.5 Narrow the bare `catch` blocks in `SettingsScreen`
- [x] T2.1 Extract Spectre prompting out of `Engine/Prompt.cs`
- [x] T2.2 Move `ReadMultiLineAsync` out of `Prompt` into a Component
- [x] T2.3 Replace `Simplify.System.AssemblyInfo` in `StatusBar` with an injected `IAppInfo`
- [x] T2.4 `ConsoleTerminal.WriteBanner` must use the theme
- [x] T2.5 Localize the hard-coded `StatusBar` hint text
- [ ] T3.1 Introduce `IConnectionSession` and stop passing `EtcdConnectionConfig` through the UI
- [ ] T3.2 Use `ScreenLayout.RenderHeader()` everywhere instead of `Clear(); Header.Render(...)`
- [ ] T4.1 Extract key-visibility logic from `KeyBrowseScreen` into a domain service
- [ ] T4.2 Extract key filtering and pagination from `KeyBrowseScreen`
- [ ] T4.3 Extract JSON flattening from `KeyImportJsonScreen`
- [ ] T4.4 Extract the import loop into a domain service
- [ ] T4.5 Split `InstanceSelectionScreen` into selection and connection management
- [ ] T4.6 Share config-file JSON access between the two repositories
- [ ] T5.1 Remove `TakeDecryptFailures` from `IConnectionConfigRepository`
- [ ] T5.2 Delete unused interface members
- [ ] T5.3 Remove duplicate members from `ITerminal`
- [ ] T5.4 Split `ITerminal` by client role
- [ ] T5.5 Fix `Spinner` cross-thread key reading
- [ ] T6.1 DECISION — Replace ambient static stores with injected services
- [ ] T6.2 DECISION — Theme colours by role instead of hue
- [ ] T6.3 DECISION — Group `ILocalization`
- [ ] T6.4 DECISION — `MainScreen` navigation table

---

## Summary of findings

| Principle | Main issues |
|---|---|
| **SRP** | `ITerminal` is a ~50-member god interface. `KeyBrowseScreen` contains etcd permission/visibility logic. `KeyImportJsonScreen` contains JSON sanitising/flattening. `InstanceSelectionScreen` is both "pick instance" and "CRUD connections". Two JSON repositories duplicate config-file read/write. |
| **OCP** | Components build ANSI strings by concatenating `_terminal.Grey + text + _terminal.Reset` — the ANSI concept leaks out of the Terminal layer as opaque strings. Theme colours are named by hue (`Teal`, `Yellow`) not by role, so a theme cannot re-purpose semantics. `TerminalColor` enum + `switch` must be edited in two places to add a role. |
| **LSP / ISP** | `IConnectionConfigRepository.TakeDecryptFailures()` is a decorator-only concern; the base repository returns `[]` (degenerate implementation). Dead interface members (`PingAsync`, `EnableAuthenticationAsync`, `DisableAuthenticationAsync`, `IsConnected`). Inconsistent result contracts across `IEtcd*` interfaces (`bool` vs `EtcdOperationResult` vs throw). |
| **DIP** | Documented exception: `Engine/Prompt.cs` depends on `Spectre.Console` and `EtcdTerminal.Infrastructure`. `StatusBar` depends on `Simplify.System.AssemblyInfo`. `Menu` (Engine) and `StatusBar` (Component) depend on the domain type `EtcdConnectionConfig` only to display it; `config` is threaded through every `ShowAsync(config)`. Ambient static stores (`ThemeStore`, `LocalizationStore`, `AppSettingsStore`) are a documented service-locator choice. `ConsoleTerminal.WriteBanner` hard-codes a colour, bypassing `ITheme`. |
| **Bugs / risks found during review** | `Menu.Show` resolves the chosen item by label — duplicate labels pick the wrong item. `Menu.StripMarkup` strips any `[...]` from labels, corrupting IPv6 connection strings like `http://[::1]:2379`. `InstanceSelectionScreen` mixes instance names and `InstanceFixedAction` names in one string id — an instance named `Exit` or `Settings` collides. Bare `catch` in several screens swallows `OperationCanceledException` and loses error detail. |
| **Testability** | Pure logic (filtering, pagination, JSON flattening, key visibility, menu navigation) is embedded in screens; no `ITerminal` test double exists. |

---

## Phase 0 — Safety net

### T0.1 Add a `FakeTerminal` test double

**Why:** Nothing in `Components/`, `Engine/` or `Screens/` is unit-testable today because there is no in-memory `ITerminal`. Later tasks need it.

**Files:** create `src/EtcdTerminal.Tests/Fakes/FakeTerminal.cs`.

**Steps:**
1. Create `public sealed class FakeTerminal : ITerminal`.
2. Implement all members. Output methods append to a `public StringBuilder Output { get; } = new();` (for `Write*` methods, `WriteTable` may append `"[table]"`, `WriteBanner` may append `"[banner]"`). Colour properties return short tags like `"<grey>"`, `Reset` returns `"</>"`. `WindowWidth = 120`, `WindowHeight = 40`, cursor properties are settable fields.
3. Add `public Queue<ConsoleKeyInfo> Keys { get; } = new();`; `ReadKey()` dequeues, throws `InvalidOperationException("No more keys")` if empty; `KeyAvailable => Keys.Count > 0`.
4. Add a helper `public void Press(params ConsoleKey[] keys)` that enqueues `new ConsoleKeyInfo('\0', key, false, false, false)` for each.
5. Add a smoke test `FakeTerminalTests.WriteLine_AppendsText` to prove it compiles and works.

**Acceptance:** build + tests green. No production code changed.

### T0.2 Strengthen architecture tests

**Why:** The layering rules in `AGENTS.md` are only partially enforced.

**Files:** `src/EtcdTerminal.Tests/ArchitectureTests.cs`.

**Steps:** add these tests, using the existing `GetReferencedTypes` helper. Filter by `type.Namespace` prefixes.
1. `ScreensDoNotReferenceInfrastructure` — no type in namespace `EtcdTerminal.App.Screens*` references a type in namespace `EtcdTerminal.Infrastructure*`.
2. `ComponentsAndEngineDoNotReferenceScreens` — no type in `EtcdTerminal.App.Components*` or `EtcdTerminal.App.Engine*` references a type in `EtcdTerminal.App.Screens*`.
3. `OnlySetupReferencesInfrastructure` — the only App types allowed to reference `EtcdTerminal.Infrastructure*` are in namespace `EtcdTerminal.App.Setup` **plus** the existing `_promptException` list (this list gets emptied in T2.1).
4. Keep the existing tests.

**Acceptance:** all new tests pass against the current code (they should — if one fails, report the violation rather than weakening the test).

---

## Phase 1 — Correctness fixes (small, low-risk)

### T1.1 `Menu` must select by index, not by label

**Why:** `Menu.Show` (`src/EtcdTerminal.App/Engine/Menu.cs:13-18`) calls `ShowLabels` and then `items.First(i => i.Label == selected)`. Two items with the same label return the first one.

**Steps:**
1. Change `ShowLabels` to return `int?` (selected index, or `null` on Escape). Rename it to `ShowAndGetIndex`.
2. `Show<TId>` returns `items[index]` when index is not null.
3. Add test `MenuTests.Show_WithDuplicateLabels_ReturnsChosenItem` using `FakeTerminal` (press `DownArrow`, `Enter`; assert the second item is returned). `Menu` needs a `StatusBar`, construct it with the same `FakeTerminal`.

**Acceptance:** behaviour identical for unique labels; duplicate labels now resolve correctly.

### T1.2 Remove `Menu.StripMarkup`

**Why:** `Menu.cs:112-115` strips `\[/?[^\]]*\]` from every label. This is leftover Spectre markup handling. No label contains Spectre markup any more (the only bracketed localisation string, `PastedLines`, is never shown in a menu), but IPv6 connection strings like `http://[::1]:2379` **are** shown as labels by `InstanceSelectionScreen.PromptForChoice` and get corrupted.

**Steps:**
1. Delete `StripMarkup`, `MarkupPattern`, the `partial` modifier and `using System.Text.RegularExpressions;`.
2. In `ShowAndGetIndex`, `plain` becomes just `displayConverter?.Invoke(c) ?? c`.
3. Add test `MenuTests.Show_LabelWithBrackets_RendersUnchanged` asserting `FakeTerminal.Output` contains `[::1]`.

### T1.3 Stop mixing instance names and fixed actions in one `string` id

**Why:** `InstanceSelectionScreen.PromptForChoice` (`InstanceSelectionScreen.cs:82-105`) uses `MenuItem<string>` where the id is either an instance `Name` or `nameof(InstanceFixedAction.X)`. `ShowAsync` then does `Enum.TryParse<InstanceFixedAction>(choice, ...)`. An instance named `Exit`, `Settings` or `ManageConnections` is unreachable.

**Steps:**
1. Create `src/EtcdTerminal.App/Screens/InstanceMenuChoice.cs`:
   ```csharp
   public sealed record InstanceMenuChoice(InstanceFixedAction? Action, EtcdConnectionConfig? Instance);
   ```
2. `PromptForChoice` returns `InstanceMenuChoice?` and builds `MenuItem<InstanceMenuChoice>` items: instances get `new(null, instance)`, fixed actions get `new(action, null)`.
3. `ShowAsync` switches on `choice.Action` when it is not null, otherwise uses `choice.Instance!`.
4. Delete the `Enum.TryParse` and the `instances.First(i => i.Name == choice)` lookup.

**Acceptance:** an instance named `Exit` connects instead of exiting. Rendering unchanged.

### T1.4 Unify error contracts on `IEtcdUserAdmin` / `IEtcdRoleAdmin`

**Why:** Within the same interface some mutations return `EtcdOperationResult` and some return bare `Task` and throw `RpcException`. Callers (`UserManagementScreen.AssignRoleAsync`, `RevokeRoleAsync`, `RoleManagementScreen.GrantOrRevokeAsync`) wrap them in bare `catch { }`, which swallows `OperationCanceledException` and discards the etcd error text that the other paths display.

**Files:** `src/EtcdTerminal/Users/IEtcdUserAdmin.cs`, `src/EtcdTerminal/Roles/IEtcdRoleAdmin.cs`, `src/EtcdTerminal.Infrastructure/DotnetEtcdBasedClient.cs`, `UserManagementScreen.cs`, `RoleManagementScreen.cs`.

**Steps:**
1. Change return type to `Task<EtcdOperationResult>` for `GrantRoleToUserAsync`, `RevokeRoleFromUserAsync`, `GrantPermissionAsync`, `RevokePermissionAsync`.
2. In `DotnetEtcdBasedClient`, wrap each of those four in the same `try { ...; return EtcdOperationResult.Ok(); } catch (RpcException ex) { return RpcFail(ex); }` pattern already used by `CreateUserAsync`.
3. In the two screens, replace `try/catch` with the `_message.ShowResult(result.Success, successMsg, result.ErrorMessage ?? failureMsg)` pattern already used by `CreateUserAsync`.
4. In `RoleManagementScreen.GrantOrRevokeAsync`, the delegate type becomes `Func<string, PermissionType, string, Task<EtcdOperationResult>>`.

**Acceptance:** no bare `catch` remains in `Screens/Users` or `Screens/Roles`; etcd error detail is shown for grant/revoke failures.

### T1.5 Narrow the bare `catch` blocks in `SettingsScreen`

**Why:** `SettingsScreen.cs:67,90` — `catch { }` around `_repository.Save`. Catch `IOException` and `UnauthorizedAccessException` only; anything else should propagate to the top-level handler in `Program.cs`, which already shows the exception.

**Steps:** replace both `catch` with `catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)`.

---

## Phase 2 — Dependency Inversion

### T2.1 Extract Spectre prompting out of `Engine/Prompt.cs` (remove the documented exception)

**Why:** `Prompt` references `Spectre.Console` and `EtcdTerminal.Infrastructure.Terminal.EscapableConsole`. `AGENTS.md` lists it as a "known exception pending extraction". This task performs that extraction.

**Files:**
- create `src/EtcdTerminal/Terminal/ITextInput.cs`
- create `src/EtcdTerminal.Infrastructure/Terminal/SpectreTextInput.cs`
- edit `src/EtcdTerminal.App/Engine/Prompt.cs`, `IocRegistrations.cs`, `ArchitectureTests.cs`, `AGENTS.md`

**Steps:**
1. Domain interface:
   ```csharp
   namespace EtcdTerminal.Terminal;

   public interface ITextInput
   {
       /// Returns null when the user cancels (Escape).
       string? ReadLine(string prompt, string? defaultValue = null);

       /// Returns null when the user cancels (Escape).
       string? ReadSecret(string prompt);
   }
   ```
2. `SpectreTextInput : ITextInput` in Infrastructure. Move into it: the `_promptStyle` field, the `_console = new EscapableConsole(AnsiConsole.Console)` field, and the three `_console.Prompt(new TextPrompt<string>(...))` calls. `ReadLine` with `defaultValue == null` uses `.AllowEmpty()`; with a default uses `.DefaultValue(defaultValue).EditableDefaultValue(true).ShowDefaultValue(false)`. Catch `OperationCanceledException` and return `null`. Do **not** call `SetCursorVisible` or write the indent here — that stays in `Prompt`.
3. `Prompt(ITerminal _terminal, ITextInput _textInput)`. `Ask`/`Ask(default)`/`Secret` keep their signatures and their cursor/indent/trim/empty-handling logic, but delegate the actual reading to `_textInput`. Remove `using Spectre.Console;` and `using EtcdTerminal.Infrastructure.Terminal;`.
4. Register `.Register<ITextInput, SpectreTextInput>(LifetimeType.Singleton)` in `RegisterInfrastructure`.
5. In `ArchitectureTests`, set `_promptException = []` (or delete the field and its `Contains` check).
6. In `AGENTS.md`, remove the three "(known exception: `Engine/Prompt.cs` …)" notes and the "(same `Prompt` exception as above)" notes. State that `ConsoleTerminal` and `SpectreTextInput` are the only Spectre touchpoints.

**Acceptance:** `AppTypesDoNotReferenceSpectre` passes with no exceptions list; prompts behave identically (Escape cancels, default values editable, secret masked).

### T2.2 Move `ReadMultiLineAsync` out of `Prompt` into a Component

**Why:** After T2.1, `Prompt` is a thin wrapper around `ITextInput`, while `ReadMultiLineAsync` (paste detection, line counting, status rendering) is a self-contained 100-line feature that depends only on `ITerminal`. SRP.

**Files:** create `src/EtcdTerminal.App/Components/MultiLinePasteReader.cs`; edit `Prompt.cs`, `KeyImportJsonScreen.cs`, `IocRegistrations.cs`.

**Steps:**
1. Create `public sealed class MultiLinePasteReader(ITerminal _terminal)` with `public async Task<string?> ReadAsync(string prompt)` and move `ReadMultiLineAsync`, `CountLines`, `IsPastedNewLineAsync`, `RenderPasteStatus`, and `_pasteBurstThresholdMs` into it verbatim.
2. `KeyImportJsonScreen` takes `MultiLinePasteReader _pasteReader` and calls `_pasteReader.ReadAsync(...)`.
3. Register `MultiLinePasteReader` as Transient in `RegisterComponents`.
4. Add test `MultiLinePasteReaderTests.CountLines_IgnoresBlankLines` (make `CountLines` `internal static` and add `[assembly: InternalsVisibleTo("EtcdTerminal.Tests")]` in the App project if not present — or keep it private and test via `ReadAsync` with `FakeTerminal`).

### T2.3 Replace `Simplify.System.AssemblyInfo` in `StatusBar` with an injected `IAppInfo`

**Why:** `StatusBar.cs:3,50` — a Component reaches into a static infrastructure helper. Also lets tests render a status bar with a fixed version.

**Files:** create `src/EtcdTerminal/Environment/IAppInfo.cs`, `src/EtcdTerminal.Infrastructure/Environment/AppInfo.cs`; edit `StatusBar.cs`, `IocRegistrations.cs`.

**Steps:**
1. `public interface IAppInfo { string Version { get; } }` in domain namespace `EtcdTerminal.Environment`.
2. `AppInfo : IAppInfo` in Infrastructure; move `GetVersion()` logic into it (use `Assembly.GetEntryAssembly()?.GetName().Version` — or keep `Simplify.System` but then move the `PackageReference` from `EtcdTerminal.App.csproj` to `EtcdTerminal.Infrastructure.csproj`).
3. `StatusBar(ITerminal _terminal, IAppInfo _appInfo)`; delete `GetVersion` and the `using`.
4. Register `IAppInfo → AppInfo` Singleton.
5. If `Simplify.System` is no longer used in App, remove its `PackageReference` from `EtcdTerminal.App.csproj`.

### T2.4 `ConsoleTerminal.WriteBanner` must use the theme

**Why:** `ConsoleTerminal.cs:143` hard-codes `Color.OrangeRed1`, bypassing `ITheme.Accent`.

**Steps:** replace with `new Color(accent.R, accent.G, accent.B)` where `accent = ThemeStore.Current.Accent` (after T6.1, if approved, this becomes the injected `ITheme`).

### T2.5 Localise the hard-coded `StatusBar` hint text

**Why:** `StatusBar.cs:11` embeds English (`navigate`, `confirm/select`, `back`) while everything else goes through `ILocalization`.

**Steps:** add `string StatusNavigate { get; }`, `string StatusConfirm { get; }`, `string StatusBack { get; }` to `ILocalization`, implement in `EnglishLocalization`, use them in `StatusBar`.

---

## Phase 3 — Session context (remove `config` threading)

### T3.1 Introduce `IConnectionSession` and stop passing `EtcdConnectionConfig` through the UI

**Why:** `Menu.Show(..., EtcdConnectionConfig? config)` and `StatusBar.Render(EtcdConnectionConfig?)` exist only so the status bar can display the active connection. This makes a generic Engine primitive depend on a domain type, and every screen signature is `ShowAsync(EtcdConnectionConfig config)` purely to forward it. `KeyBrowseScreen` even stores it in a mutable field `_config`.

**Files:**
- create `src/EtcdTerminal/Configuration/IConnectionSession.cs`
- create `src/EtcdTerminal/Configuration/ConnectionSession.cs`
- edit: `Menu.cs`, `MenuScreen.cs`, `StatusBar.cs`, `ScreenLayout.cs`, `KeyBrowseControl.cs`, every `*Screen.cs`, `Program.cs`, `IocRegistrations.cs`

**Steps:**
1. Domain:
   ```csharp
   public interface IConnectionSession
   {
       EtcdConnectionConfig? Active { get; }
       void Start(EtcdConnectionConfig config);
       void End();
   }
   ```
   `ConnectionSession : IConnectionSession` — trivial in-memory implementation (domain-only, no infrastructure).
2. Register `IConnectionSession → ConnectionSession` as **Singleton**.
3. `StatusBar(ITerminal _terminal, IAppInfo _appInfo, IConnectionSession _session)`; `Render()` takes no parameters and reads `_session.Active`.
4. Remove the `config` parameter from `Menu.Show`, `MenuScreen.RunAsync`, `ScreenLayout.RenderHeader`, `KeyBrowseControl.Render`.
5. Change every screen `ShowAsync(EtcdConnectionConfig config)` to `ShowAsync()`. Where a screen genuinely needs the config (only `KeyBrowseScreen.LoadKeysAsync` reads `Username`), inject `IConnectionSession` and read `_session.Active!.Username`. Remove the `_config` field from `KeyBrowseScreen`.
6. `InstanceSelectionScreen.ShowAsync` — after a successful connect, call `_session.Start(selected)` before returning. `MainScreen` — call `_session.End()` wherever `DisconnectAsync` is called (both places). Keep `ShowAsync` return type `EtcdConnectionConfig?` in `InstanceSelectionScreen` so `Program.cs` still knows whether to exit; `Program.cs` calls `mainScreen.ShowAsync()` without arguments.
7. Update `AGENTS.md` Engine description: "Engine depends on `ITerminal` only" now holds literally.

**Acceptance:** `rg "EtcdConnectionConfig" src/EtcdTerminal.App/Engine src/EtcdTerminal.App/Components` returns nothing. Status bar still shows name/connection string/username while connected and only the version otherwise.

### T3.2 Use `ScreenLayout.RenderHeader()` everywhere instead of `Clear(); Header.Render(...)`

**Why:** The pair `_terminal.Clear(); Header.Render(_terminal);` appears 9 times (`MainScreen`, `SettingsScreen`, `MenuScreen`, `KeyBrowseControl`, `InstanceSelectionScreen` ×4, `ScreenLayout`). `ScreenLayout.RenderHeader` already encapsulates it and also paints the status bar.

**Steps:**
1. Replace each occurrence with `_screenLayout.RenderHeader()` (inject `ScreenLayout` where missing).
2. `Menu` currently calls `_statusBar.Render()` itself. Keep that (Menu may be shown without a header) — double-painting the status bar is harmless but if it flickers, remove the call in `Menu` and rely on `ScreenLayout`.
3. Convert `Header` from a static class to `public sealed class Header(ITerminal _terminal)` with `public void Render()`; register Transient; `ScreenLayout` becomes the only consumer. This aligns with the primary-constructor convention used by every other Component.

---

## Phase 4 — Single Responsibility extractions

### T4.1 Extract key-visibility logic from `KeyBrowseScreen` into a domain service

**Why:** `KeyBrowseScreen.LoadKeysAsync` (`KeyBrowseScreen.cs:54-118`) decides which keys the current user may see: auth-enabled check, `root` role special-case, `"\0"` prefix normalisation, Read/ReadWrite permission filtering, `DistinctBy`. This is etcd domain knowledge inside a screen, untestable without a terminal, and the reason the screen needs four `IEtcd*` dependencies.

**Files:** create `src/EtcdTerminal/Keys/IReadableKeysProvider.cs`, `src/EtcdTerminal/Keys/ReadableKeysProvider.cs`; edit `KeyBrowseScreen.cs`, `IocRegistrations.cs`; create `src/EtcdTerminal.Tests/ReadableKeysProviderTests.cs`.

**Steps:**
1. Interface:
   ```csharp
   public interface IReadableKeysProvider
   {
       Task<IReadOnlyList<EtcdKeyValue>> GetReadableKeysAsync(string? username, CancellationToken ct = default);
   }
   ```
2. `ReadableKeysProvider(IEtcdKeyStore _keyStore, IEtcdUserAdmin _userAdmin, IEtcdRoleAdmin _roleAdmin, IEtcdAuthAdmin _authAdmin)` — move the body of `LoadKeysAsync` (including the local `LoadAllKeysAsync` fallback from `""` to `"/"`) into it verbatim, returning the list instead of assigning fields. Lives in the **domain** project (it only depends on domain interfaces).
3. `KeyBrowseScreen(ITerminal _terminal, IEtcdKeyStore _keyStore, IReadableKeysProvider _readableKeys, IConnectionSession _session, ScreenLayout _screenLayout, KeyBrowseControl _control, Prompt _prompt, Message _message)`. `LoadKeysAsync` becomes `_allKeys = [.. await _readableKeys.GetReadableKeysAsync(_session.Active?.Username)]; _filteredKeys = [.. _allKeys];`.
4. Register `IReadableKeysProvider → ReadableKeysProvider` Transient.
5. Tests with hand-written stubs of the four interfaces: (a) auth disabled → all keys; (b) user has `root` → all keys; (c) user with one Read permission on `/a` → only `/a*` keys; (d) Write-only permission → nothing; (e) `"\0"` prefix → all keys.

### T4.2 Extract key filtering and pagination from `KeyBrowseScreen`

**Why:** `ApplyFilter`, `GetCurrentPageKeys`, `GetTotalPages` are pure functions over `List<EtcdKeyValue>` mixed into an orchestrator.

**Files:** create `src/EtcdTerminal.App/Screens/Keys/KeyPager.cs` (feature-local, no `ITerminal`); tests in `KeyPagerTests.cs`.

**Steps:**
1. `public sealed class KeyPager` with: `void SetSource(IReadOnlyList<EtcdKeyValue> keys)`, `void Filter(string query)` (case-insensitive `Contains` on key or value, empty query = all), `IReadOnlyList<EtcdKeyValue> GetPage(int page, int pageSize)`, `int GetTotalPages(int pageSize)`, `int FilteredCount`.
2. `KeyBrowseScreen` holds one `KeyPager` (plain `new()` — it has no dependencies) and delegates; delete `_allKeys`, `_filteredKeys`, `ApplyFilter`, `GetCurrentPageKeys`, `GetTotalPages`.
3. Tests: filter is case-insensitive; matches value as well as key; `GetTotalPages` returns 1 for empty; last partial page is returned.

### T4.3 Extract JSON flattening from `KeyImportJsonScreen`

**Why:** `SanitizeJson`, `FlattenJson`, `FlattenNode` (`KeyImportJsonScreen.cs:178-229`) are pure and are the riskiest logic in that screen; they are untested.

**Files:** create `src/EtcdTerminal/Keys/JsonKeyFlattener.cs` (domain — only `System.Text.Json`); tests `JsonKeyFlattenerTests.cs`.

**Steps:**
1. `public static class JsonKeyFlattener` with `public static IReadOnlyList<KeyValuePair<string, string>> Flatten(string json, string prefix, string separator)`. It performs sanitise → parse → flatten. Throws `JsonException` on invalid JSON. Throws `ArgumentException` when the root is an array and `prefix` is empty (the screen currently shows `NoKeysInJson` for that case — keep that message by catching `ArgumentException` in the screen). Returns an empty list when no leaf values exist.
2. Move the private static methods verbatim; make the anonymous `System.Text.RegularExpressions.Regex.Replace` a `[GeneratedRegex]` `partial` method.
3. `KeyImportJsonScreen` calls `JsonKeyFlattener.Flatten(json, prefix, separator)` inside the existing `try`.
4. Tests: nested object → `a:b:c`; array → `arr:0`, `arr:1`; trailing comma tolerated; bare `"a": 1` without braces is wrapped; array root without prefix throws.

### T4.4 Extract the import loop into a domain service

**Why:** The `foreach` inside `_spinner.RunAsync` in `KeyImportJsonScreen.ShowAsync` (lines 98-123) is a write-batch with created/overwritten/failed counters — business logic.

**Files:** create `src/EtcdTerminal/Keys/IKeyImporter.cs`, `src/EtcdTerminal/Keys/KeyImporter.cs`, `src/EtcdTerminal/Keys/KeyImportResult.cs`; edit `KeyImportJsonScreen.cs`, `IocRegistrations.cs`.

**Steps:**
1. `public readonly record struct KeyImportResult(int Created, int Overwritten, int Failed);`
2. `IKeyImporter { Task<KeyImportResult> ImportAsync(IReadOnlyList<KeyValuePair<string,string>> entries, CancellationToken ct); }`
3. `KeyImporter(IEtcdKeyStore _keyStore)` — move the loop; identical semantics (get → update or create).
4. Screen: `KeyImportResult result = default; var imported = await _spinner.RunAsync(msg, async ct => result = await _importer.ImportAsync(entries, ct));`.
5. Register Transient. Add a test with a stub `IEtcdKeyStore` counting created/overwritten/failed.

### T4.5 Split `InstanceSelectionScreen` into selection and connection management

**Why:** 255 lines, two responsibilities: (a) pick & connect, (b) add/edit/move/remove connections. Methods `ManageConfigs`, `EditInstanceInteractive`, `SaveInstanceInteractive`, `MoveInstanceInteractive`, `RemoveInstanceInteractive` (lines 107-254) belong to (b).

**Files:** create `src/EtcdTerminal.App/Screens/ManageConnectionsScreen.cs`; edit `InstanceSelectionScreen.cs`, `IocRegistrations.cs`.

**Steps:**
1. `ManageConnectionsScreen(ScreenLayout _screenLayout, IConnectionConfigRepository _configRepo, Menu _menu, Prompt _prompt, Message _message)` with `public void Show(IReadOnlyList<EtcdConnectionConfig> instances)`; move the five methods verbatim.
2. `InstanceSelectionScreen` takes `ManageConnectionsScreen _manageConnections` and calls `_manageConnections.Show(instances)` in the `ManageConnections` case. Remove `_prompt` from `InstanceSelectionScreen` if no longer used.
3. Register Transient.

### T4.6 Share config-file JSON access between the two repositories

**Why:** `JsonBasedConnectionConfigRepository` and `JsonBasedSettingsRepository` both: check `File.Exists(_configPath)`, `JsonNode.Parse(File.ReadAllText(...))`, cast to `JsonObject`, `PrivateFileSystem.CreateDirectory`, `WriteAllTextAtomic(... root.ToJsonString(_jsonOptions))`. Two copies with subtly different error handling (`LoadExistingRoot` throws on non-object root; the settings repo silently replaces it).

**Files:** create `src/EtcdTerminal.Infrastructure/Configuration/JsonConfigFile.cs`; edit both repositories.

**Steps:**
1. `public sealed class JsonConfigFile(IAppEnvironment _environment)` with `JsonObject? TryReadRoot()` (returns `null` when the file is missing or unparsable), `JsonObject ReadRootOrThrow()` (throws `JsonException` when file exists but root is not an object), `void WriteRoot(JsonObject root)` (creates directory, writes atomically, indented).
2. Both repositories take `JsonConfigFile` via primary constructor and use it. `JsonBasedConnectionConfigRepository.SaveInstances` uses `ReadRootOrThrow` (preserves current "don't overwrite a corrupt file" behaviour); `JsonBasedSettingsRepository.Save` keeps its lenient behaviour via `TryReadRoot() ?? []`.
3. Register `JsonConfigFile` Singleton and update the factory lambda for `IConnectionConfigRepository` in `RegisterConfiguration`.

---

## Phase 5 — Interface hygiene (ISP / LSP)

### T5.1 Remove `TakeDecryptFailures` from `IConnectionConfigRepository`

**Why:** It exists only for the `ProtectedConfigRepository` decorator; `JsonBasedConnectionConfigRepository` and the test stub return `[]` — a degenerate implementation forced by the interface (ISP/LSP smell). `InstanceSelectionScreen` calls `LoadInstances()` then `TakeDecryptFailures()` — a hidden temporal coupling.

**Files:** `IConnectionConfigRepository.cs`, `ProtectedConfigRepository.cs`, `JsonBasedConnectionConfigRepository.cs`, `InstanceSelectionScreen.cs`, `DecryptFailureTests.cs`; create `src/EtcdTerminal/Configuration/IDecryptFailureSource.cs`.

**Steps:**
1. Create `public interface IDecryptFailureSource { IReadOnlyList<string> TakeDecryptFailures(); }`.
2. Remove the member from `IConnectionConfigRepository`; `ProtectedConfigRepository : IConnectionConfigRepository, IDecryptFailureSource`; delete the `[]` implementations in `JsonBasedConnectionConfigRepository` and `StubRepository`.
3. In `RegisterConfiguration`, register the `ProtectedConfigRepository` instance once and expose it under both interfaces: `.Register<ProtectedConfigRepository>(c => new ProtectedConfigRepository(...), Singleton)`, `.Register<IConnectionConfigRepository>(c => c.Resolve<ProtectedConfigRepository>(), Singleton)`, `.Register<IDecryptFailureSource>(c => c.Resolve<ProtectedConfigRepository>(), Singleton)`.
4. `InstanceSelectionScreen` takes `IDecryptFailureSource _decryptFailures` and uses it in `ShowDecryptWarningIfNeeded`.
5. Update the tests to call `TakeDecryptFailures` on the concrete `ProtectedConfigRepository` (they already do).

### T5.2 Delete unused interface members

**Why:** YAGNI. Verified by `rg`: `IEtcdConnection.PingAsync`, `IEtcdConnection.IsConnected`, `IEtcdAuthAdmin.EnableAuthenticationAsync`, `IEtcdAuthAdmin.DisableAuthenticationAsync` have no callers outside their implementation.

**Steps:** remove the four members from the interfaces and from `DotnetEtcdBasedClient`. If the owner wants to keep auth enable/disable as a future feature, keep them but add a `// TODO(feature): not wired to UI` comment — ask before deleting those two; `PingAsync`/`IsConnected` may be deleted outright.

### T5.3 Remove duplicate members from `ITerminal`

**Why:** `Clear()` and `ClearScreen()` are identical; `Indent` is a default interface member that duplicates `SelectionPointerEmpty` and is also re-implemented in `ConsoleTerminal`.

**Steps:**
1. Delete `ClearScreen()`; `Program.cs:31` calls `Clear()`.
2. Keep `Indent` as the public name (it describes intent), delete `SelectionPointerEmpty`, and update the ~8 call sites (`Menu`, `Prompt`, `KeyBrowseLayout`). Make `Indent` a normal abstract property (remove the default-interface body) and keep the `ConsoleTerminal` implementation.

### T5.4 Split `ITerminal` by client role

**Why:** `ITerminal` has ~50 members spanning raw output, cursor control, key input, theme colour strings, row/border helpers, Spectre-rendered widgets (`WriteTable`, `WriteBanner`, `WriteException`) and process lifecycle (`OnInterrupt`, `Initialize`, `Flush`). `PressAnyKeyPrompt` needs 2 members; `Menu` needs ~10.

**Files:** `src/EtcdTerminal/Terminal/*.cs`, `ConsoleTerminal.cs`, `FakeTerminal.cs`.

**Steps (keep `ITerminal` as the aggregate so no call site changes):**
1. Create in `EtcdTerminal.Terminal`:
   - `ITerminalOutput` — `Write*`, `WriteIndentedLine`, `WriteLine()`, `Clear`, `ClearLine`, `ClearToEndOfScreen`, `FillRow`, `WriteFillRow`, `WriteRow`, `WriteBorderedFillRow`, `WriteBorderedRow`, `PadCurrentRow`, `GetVisibleLength`, `Flush`, `WindowWidth`, `WindowHeight`.
   - `ITerminalCursor` — `CursorLeft`, `CursorTop`, `SetCursorPosition`, `SetCursorVisible`.
   - `ITerminalInput` — `KeyAvailable`, `ReadKey`.
   - `ITerminalStyle` — the colour string properties, `Reset`, `SelectionPointer`, `Indent`, `SetBackground`, `ResetBackground`, `ResetColor`, `SetDarkBackground`.
   - `ITerminalWidgets` — `WriteTable`, `WriteBanner`, `WriteException`.
   - `ITerminalLifecycle` — `Initialize`, `OnInterrupt`.
2. `public interface ITerminal : ITerminalOutput, ITerminalCursor, ITerminalInput, ITerminalStyle, ITerminalWidgets, ITerminalLifecycle { }`.
3. Register each sub-interface in IoC as `c => c.Resolve<ITerminal>()` Singleton.
4. Then, per class, narrow the constructor parameter to the smallest interface(s) that compile: `PressAnyKeyPrompt(ITerminalOutput, ITerminalInput)`, `Message(ITerminalOutput, ...)`, `Spinner(ITerminalOutput, ITerminalCursor, ITerminalInput, ITerminalStyle)`, renderers → `ITerminalOutput, ITerminalWidgets`, `Program.cs` → `ITerminal`. If a class needs 4+ sub-interfaces, leave it on `ITerminal`.
5. `FakeTerminal` continues to implement `ITerminal`.

**Acceptance:** behaviour identical; each Component/Screen ctor lists only what it uses.

### T5.5 Fix `Spinner` cross-thread key reading

**Why:** `Spinner.RunAsync` reads keys on a `Task.Run` thread while `PollEscape` swallows `InvalidOperationException`. Not a SOLID issue but a latent race noticed during review; the fix is small.

**Steps:** poll `_terminal.KeyAvailable`/`ReadKey` on the awaiting thread instead: run the spinner animation in the background task **without** key polling, and in the foreground `while (!actionTask.IsCompleted) { if (PollEscape()) cts.Cancel(); await Task.WhenAny(actionTask, Task.Delay(50)); }`. Keep the public signature.

---

## Phase 6 — DECISION items (do not implement without owner approval)

### T6.1 DECISION — Replace ambient static stores with injected services

**Finding:** `ThemeStore.Current`, `LocalizationStore.Current`, `AppSettingsStore.Current` are service locators. They hide dependencies (a test of `PressAnyKeyPrompt` fails with `InvalidOperationException` unless a global is set first), and `ConsoleTerminal` re-reads `ThemeStore.Current` on every colour access. `AGENTS.md` explicitly sanctions them as "ambient contexts", so this is a documented trade-off, not an oversight.

**If approved:**
1. Register `ITheme → ReddyTheme` and `ILocalization → EnglishLocalization` as Singletons; inject into `ConsoleTerminal`, `SpectreTextInput`, every Component/Screen that uses `LocalizationStore.Current` (replace with `_localization.X`).
2. Replace static `AppSettingsStore` with `public interface IAppSettingsStore { IAppSettings Current { get; } void Update(IAppSettings settings); }` registered Singleton; `Program.cs` and `SettingsScreen` use it.
3. Delete the three static classes; update `AGENTS.md` "Ambient contexts" section.
4. Update `FakeTerminal`-based tests to construct components with a stub `ILocalization`.

**If rejected:** at minimum make `ConsoleTerminal` take `ITheme` via constructor (it's a Singleton created after `ThemeStore` is set) so the Infrastructure layer stops depending on an App-set global.

### T6.2 DECISION — Theme colours by role instead of hue

**Finding:** `ITheme` / `ITerminal` expose `Teal`, `Yellow`, `Green`, `Red`, `Grey`, `Dim`. Components pick hues directly (`_terminal.Teal` for the instance name, `_terminal.Yellow` for the username). A second theme cannot change *which* element is emphasised, only the exact shade of each hue. `TerminalColor` (`Success/Error/Warning/Muted`) is the role-based enum but covers only 4 roles and is only usable via `Write(text, color)`, so components fall back to hue strings and string concatenation.

**If approved:** rename `ITheme` members to roles (`Primary`, `Secondary`, `Success`, `Danger`, `Warning`, `Muted`, `Subtle`, `Accent`, `PanelBackground`, `PanelDarkerBackground`, `WindowBackground`), mirror the names on `ITerminalStyle`, extend `TerminalColor` to the same set, and make `ConsoleTerminal.GetColorEscape` a lookup table over the theme so adding a role touches `ITheme` + enum only.

### T6.3 DECISION — Group `ILocalization`

**Finding:** 150 flat string properties on one interface. Every new screen edits the interface and every implementation. This is ISP at the "one giant interface" level but the cost of change is mostly clerical.

**Option A (low churn):** leave flat; add a comment block per feature (already implicitly grouped).
**Option B:** split into `IInstanceTexts`, `IKeyTexts`, `IUserTexts`, `IRoleTexts`, `IPermissionTexts`, `ISettingsTexts`, `ICommonTexts`, with `ILocalization` exposing them as properties (`Localization.Keys.EnterKey`). Screens depend only on the group they use.

### T6.4 DECISION — `MainScreen` navigation table

**Finding:** `MainScreen` takes 6 concrete screens and switches on `MainMenuAction`. Adding a feature = new enum value + new ctor param + new `case`. Acceptable for a 7-item TUI; listed for completeness.

**If approved:** define `public interface IMainMenuEntry { MainMenuAction Action; string Label; Task ShowAsync(); }`, have each feature screen implement it, inject `IEnumerable<IMainMenuEntry>` (check Simplify.DI supports collection resolution; otherwise register a `MainMenuEntries` factory) and build the menu from it.

---

## Out of scope / explicitly not recommended

- Do **not** merge the five `IEtcd*` interfaces or split `DotnetEtcdBasedClient` into five classes: consumers already depend on the narrow interfaces (good ISP), and one gRPC client instance must be shared.
- Do **not** introduce MediatR/CQRS or an MVVM framework; the current Screen → Component → Terminal structure is appropriate for the app's size.
- Do **not** add `ConfigureAwait(false)` throughout; this is a console app with no synchronisation context.
