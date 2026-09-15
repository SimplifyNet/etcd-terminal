# Refactoring TODO — etcd-terminal

Generated from an architecture/SOLID review. Each task is self-contained: it states the
problem, the exact files involved, what to do, and how to verify.

## Rules for whoever executes this list

1. Read `AGENTS.md` before writing any code. All conventions there are mandatory
   (file-scoped namespaces, primary constructors, member ordering, no braces on
   single-statement bodies, blank line after declaration blocks, one public type per file).
2. Work on **one task at a time**, in the order given inside each phase. Do not batch.
3. After every task run:
   ```
   cd src && dotnet build
   ```
   The build must succeed with **0 warnings, 0 errors** before moving on.
4. Do not rename or reformat code that is unrelated to the task you are on.
5. If a task turns out to be larger than described, stop and report instead of
   improvising a different design.
6. Line numbers are from the time of review — verify by reading the file, do not
   trust them blindly.

## Progress

- [x] Phase 1 — Data loss and crashes
  - [x] T1. Stop destroying `config.json` when it fails to parse
  - [x] T2. Guard `JsonBasedSettingsRepository.Load` against a corrupt file
  - [x] T3. Fix the crash when a JSON **array** is pasted into Import JSON
  - [x] T4. Fix connection rename orphaning the old entry
  - [x] T5. Fix "keep existing password" being impossible when editing a connection
  - [x] T6. Make config writes atomic
- [ ] Phase 2 — Correctness of the etcd client
  - [x] T7. Make cancellation real
  - [x] T8. (Depends on T7) Allow Esc to abort a long-running operation
  - [x] T9. Replace `_client!` with an enforced precondition
  - [x] T10. Make `ConnectAsync` actually connect
  - [ ] T11. Replace the reflection-based auth detection
- [ ] Phase 3 — Layering
  - [ ] T12. Move all Spectre.Console usage out of Components and Screens
  - [ ] T13. Correct `AGENTS.md` so it describes the actual code
  - [ ] T14. Add an architecture test to prevent layering regressions
- [ ] Phase 4 — Structural design
  - [ ] T15. Stop keying control flow on localized display strings
  - [x] T16. Delete the duplicated spinner and fix its concurrency bugs
  - [ ] T17. Add the missing `Message` component and remove ~20 duplications
  - [ ] T18. Make `IAppSettings` immutable
  - [ ] T19. Split `IEtcdClient` by concern
  - [ ] T20. Preserve the cause of etcd failures
- [ ] Phase 5 — Polish
  - [ ] T21. Deduplicate connection-string validation
  - [ ] T22. Merge `AddInstanceInteractive` and `EditInstanceInteractive`
  - [ ] T23. Deduplicate the ANSI colour properties
  - [ ] T24. Replace `Thread.Sleep` in the paste-detection path
  - [ ] T25. Localize the remaining hardcoded English strings
  - [ ] T26. Fix the Ctrl+C double-dispose
  - [ ] T27. Fix the brace-style violation
  - [ ] T28. Document the encryption key's threat model (or fix it)
  - [ ] T29. Note the N+1 and full-scan query patterns

---

# Phase 1 — Data loss and crashes (do these first)

## [x] T1. Stop destroying `config.json` when it fails to parse

**Problem.** `JsonBasedConnectionConfigRepository.LoadExistingRoot` catches a parse
failure and returns an empty `JsonObject`. `SaveInstances` then writes that empty object
over the file, permanently erasing every saved connection *and* the settings section.

**Files.**
- `src/EtcdTerminal.Infrastructure/Configuration/JsonBasedConnectionConfigRepository.cs`
  (`LoadExistingRoot`, ~line 136; `SaveInstances`, ~line 117)

**Do.**
- Change `LoadExistingRoot` so it distinguishes three cases: file missing (return a new
  empty `JsonObject`), file present and parseable (return the parsed root), file present
  but unparseable (**throw**, do not return empty).
- Let the exception propagate out of `SaveInstances`. Never overwrite a file whose
  current contents could not be read.

**Verify.** Manually: put `{ not json` into `~/.config/etcd-terminal/config.json`,
start the app, try to add a connection — the file must still contain `{ not json`
afterwards.

---

## [x] T2. Guard `JsonBasedSettingsRepository.Load` against a corrupt file

**Problem.** `Load` has no `try`/`catch` (unlike its own `Save` and unlike the sibling
connection repository). A malformed `config.json` throws out of `Program.cs` line ~50;
the `while (true)` loop restarts, hits the same exception, and loops forever. The user
cannot exit except with Ctrl+C. A stored `PageSize` of the wrong JSON type
(`"10"` or `10.5`) throws `InvalidOperationException` from `GetValue<int>()` with the
same result.

**Files.**
- `src/EtcdTerminal.Infrastructure/Configuration/JsonBasedSettingsRepository.cs`
  (`Load`, ~lines 17-32)

**Do.**
- Wrap the parse in `try`/`catch (JsonException)` and return `new AppSettings()` (defaults)
  on failure.
- Read `PageSize` and `TrimInputValues` defensively: use
  `settings[PageSizeProperty] is JsonValue v && v.TryGetValue<int>(out var pageSize)`
  style checks instead of `GetValue<T>()`, so a wrong stored type falls back to the
  default instead of throwing.

**Verify.** Set `"settings": { "pageSize": "abc" }` in the config file — the app must
start with default settings and not crash.

---

## [x] T3. Fix the crash when a JSON **array** is pasted into Import JSON

**Problem.** `KeyImportJsonScreen.SanitizeJson` deliberately allows input starting with
`[`, but the caller does `JsonNode.Parse(sanitized)?.AsObject()` and catches only
`JsonException`. `AsObject()` on a `JsonArray` throws `InvalidOperationException`, which
is uncaught and tears down the screen.

**Files.**
- `src/EtcdTerminal.App/Screens/Keys/KeyImportJsonScreen.cs` (~lines 37-52)

**Do.**
- Parse into `JsonNode?` first, then branch:
  - `JsonObject o` → flatten as today via `FlattenJson`.
  - `JsonArray a` → flatten via `FlattenNode(a, prefix, separator, entries)` so array
    indices become key segments. If `prefix` is empty, show the existing
    `NoKeysInJson` warning instead (a bare array with no prefix produces meaningless
    numeric keys).
  - anything else → show `InvalidJson`.
- Also catch `InvalidOperationException` alongside `JsonException` as a safety net.

**Verify.** Paste `[1, 2, 3]` with prefix `test` — must import `test:0`, `test:1`,
`test:2`. Paste `[1,2,3]` with no prefix — must show "No keys found in JSON." and not crash.

---

## [x] T4. Fix connection rename orphaning the old entry

**Problem.** Editing a connection calls `_configRepo.AddInstance(config)` with the **new**
name. `AddInstance` does `RemoveAll(i => i.Name == config.Name)` then `Add`. Renaming
`prod` → `production` removes nothing, appends `production`, and leaves `prod` behind as
a duplicate. Even without a rename, the edited entry jumps to the end of the list,
silently undoing the user's MoveUp/MoveDown ordering.

**Files.**
- `src/EtcdTerminal/Configuration/IConnectionConfigRepository.cs`
- `src/EtcdTerminal.Infrastructure/Configuration/JsonBasedConnectionConfigRepository.cs`
- `src/EtcdTerminal.Infrastructure/Configuration/ProtectedConfigRepository.cs`
- `src/EtcdTerminal.App/Screens/InstanceSelectionScreen.cs` (`EditInstanceInteractive`, ~line 234)

**Do.**
- Add `void UpdateInstance(string originalName, EtcdConnectionConfig config);` to
  `IConnectionConfigRepository`.
- Implement it in `JsonBasedConnectionConfigRepository`: find the index of
  `originalName`, replace the element **in place** (preserving list position); if not
  found, append. Save.
- Forward it in `ProtectedConfigRepository` (encrypt exactly as `AddInstance` does).
- In `EditInstanceInteractive`, capture the original name before prompting and call
  `UpdateInstance(originalName, config)`.

**Verify.** Create A, B, C. Move C to the top. Rename C → Z. The list must read Z, A, B
with no duplicate C.

---

## [x] T5. Fix "keep existing password" being impossible when editing a connection

**Problem.** In `EditInstanceInteractive`, `password = existing.Password ?? string.Empty;`
is unconditionally overwritten a few lines later by `password = newPassword;`. Pressing
Enter on an empty secret prompt wipes the stored password instead of keeping it.

**Files.**
- `src/EtcdTerminal.App/Screens/InstanceSelectionScreen.cs` (~lines 212-224)

**Do.**
- Keep `existing.Password` when the secret prompt returns an empty string; only replace
  it when the user actually typed something. `null` (Esc) must still cancel the whole edit.
- Update the password prompt text to indicate that empty = keep current. Add a new
  `ILocalization` property for it (see T13 for the localization rules) rather than
  hardcoding English.

**Verify.** Edit a connection with a password, press Enter at the password prompt, save,
reopen the edit screen — the password must still be there.

---

## [x] T6. Make config writes atomic

**Problem.** Both repositories do `File.WriteAllText` directly on `config.json`. A crash
or `Environment.Exit` (the Ctrl+C handler calls it) mid-write truncates the file.

**Files.**
- `src/EtcdTerminal.Infrastructure/Configuration/JsonBasedConnectionConfigRepository.cs` (`SaveInstances`)
- `src/EtcdTerminal.Infrastructure/Configuration/JsonBasedSettingsRepository.cs` (`Save`)

**Do.**
- Add one shared private helper (put it in a new
  `src/EtcdTerminal.Infrastructure/Configuration/JsonConfigFile.cs` `internal static class`)
  that writes to `<path>.tmp` and then `File.Move(tmp, path, overwrite: true)`.
- Use it from both save paths.
- Preserve the existing Unix `0600` permission behaviour if any exists on the final file.

**Verify.** Build passes; saving settings and connections still works end to end.

---

# Phase 2 — Correctness of the etcd client

## [x] T7. Make cancellation real

**Problem.** Every `IEtcdClient` method takes `CancellationToken ct = default`, and
`ITerminal.ShowStatusAsync` / `Spinner.RunAsync` hand out a token — but the token handed
out is always `CancellationToken.None`, and callers drop it anyway. Nothing in the app
can be cancelled; an unreachable etcd hangs the spinner forever.

**Files.**
- `src/EtcdTerminal.Infrastructure/Terminal/ConsoleTerminal.cs` (`ShowStatusAsync`, ~line 146)
- `src/EtcdTerminal.App/Components/Spinner.cs` (~line 29)
- `src/EtcdTerminal.App/Screens/InstanceSelectionScreen.cs` (~lines 36-40)
- `src/EtcdTerminal.App/Screens/Permissions/PermissionViewScreen.cs` (~lines 19-23)

**Do.**
- In the spinner implementation, create a `CancellationTokenSource`, pass **its** token to
  `action`, and dispose it in the `finally`.
- Fix every call site that receives a `ct` parameter and ignores it — forward it to every
  `IEtcdClient` call inside the callback. Search the whole `Screens/` folder for
  `async ct =>` and check each one.
- Do **not** attempt to add Esc-to-cancel key handling in this task; that is T8.

**Verify.** Build passes; `grep -rn "CancellationToken.None" src/` returns nothing outside
of tests.

---

## [x] T8. (Depends on T7) Allow Esc to abort a long-running operation

**Do.** In the spinner loop, poll `ITerminal.KeyAvailable`; if a key is available and it is
`ConsoleKey.Escape`, call `Cancel()` on the CTS from T7. Catch `OperationCanceledException`
around the awaited action and return a value/flag indicating the operation was aborted, so
screens can show a "cancelled" message instead of an error.

**Verify.** Point a connection at an unreachable host, start connecting, press Esc — the
app must return to the instance list instead of hanging.

---

## [x] T9. Replace `_client!` with an enforced precondition

**Problem.** ~22 call sites in `DotnetEtcdBasedClient` dereference `_client!`. Calling any
method before `ConnectAsync` throws `NullReferenceException`, not a meaningful error.

**Files.**
- `src/EtcdTerminal.Infrastructure/DotnetEtcdBasedClient.cs`

**Do.**
- Add a private property/method, e.g.
  `private EtcdClient Client => _client ?? throw new InvalidOperationException("Not connected to etcd.");`
- Replace every `_client!` with `Client`.
- Do not change any public signatures in this task.

**Verify.** `grep -n "_client!" src/EtcdTerminal.Infrastructure/DotnetEtcdBasedClient.cs`
returns nothing.

---

## [x] T10. Make `ConnectAsync` actually connect

**Problem.** `ConnectAsync` constructs an `EtcdClient` (lazy channel) and returns
`Task.CompletedTask`. It cannot fail on a bad endpoint or bad credentials, so every caller
has to remember to call `PingAsync` separately. The method name lies.

**Files.**
- `src/EtcdTerminal.Infrastructure/DotnetEtcdBasedClient.cs` (`ConnectAsync`, ~lines 23-43)
- `src/EtcdTerminal.App/Screens/InstanceSelectionScreen.cs` (~lines 36-40)

**Do.**
- Have `ConnectAsync` construct the client and then `await` a real round-trip (the existing
  `PingAsync` logic) using the supplied `ct`. On failure, dispose the partially created
  client, null out `_client`, and let the exception propagate.
- Remove the now-redundant separate `PingAsync()` call from `InstanceSelectionScreen`.
- Keep `PingAsync` on the interface — it is still useful for health checks.

**Verify.** Connecting to a bogus endpoint must show the existing "Failed to connect"
error and return to the list.

---

## [ ] T11. Replace the reflection-based auth detection

**Problem.** `IsAuthenticationEnabledAsync` locates `AuthClient` and `AuthStatusAsync` by
reflection and reads `.Result` off the returned task by reflection, wrapped in
`catch { return false; }`. Any failure — including a network error — reports
"authentication is disabled", which sends `KeyBrowseScreen` down the wrong load path.

**Files.**
- `src/EtcdTerminal.Infrastructure/DotnetEtcdBasedClient.cs` (~lines 312-344)

**Do.**
- First check whether the pinned `dotnet-etcd` (9.1.0) exposes `AuthStatusAsync` (or an
  equivalent) as a normal public API. If it does, call it directly and delete all
  reflection.
- If it genuinely does not, keep reflection but: narrow the catch to
  `catch (Exception ex) when (ex is TargetInvocationException or MissingMethodException or AmbiguousMatchException)`,
  and let real RPC failures propagate instead of being reported as "auth disabled".
- Either way, the method must never turn a network failure into `false`.

**Verify.** Build passes; behaviour against a live etcd is unchanged for the happy path.

---

# Phase 3 — Layering (cheap, high value)

## [ ] T12. Move all Spectre.Console usage out of Components and Screens

**Problem.** `AGENTS.md` states Screens must not touch `Console.*` / `AnsiConsole.*` / ANSI,
and that all Spectre dependencies live in Infrastructure. Eight violations exist. They
compile only because Spectre leaks transitively through the project reference —
`EtcdTerminal.App.csproj` does not reference it.

**Violations.**
| File | What |
|---|---|
| `Components/Header.cs` (~1, 8) | `AnsiConsole.Write(new FigletText(...))` |
| `Screens/Users/UserListRenderer.cs` (~4, 33) | `AnsiConsole.Write(table)` |
| `Screens/Roles/RoleListRenderer.cs` (~4, 35) | `AnsiConsole.Write(table)` |
| `Screens/Permissions/PermissionViewRenderer.cs` (~2, 43) | `AnsiConsole.Write(table)` |
| `Engine/Prompt.cs` (~4, 7, 17) | uses `AnsiConsole.Console` and `Infrastructure.EscapableConsole` |
| `Components/Spinner.cs` (~23) | raw ANSI `"\r\x1b[2K"` |
| `Engine/Menu.cs` (~10) | raw ANSI `"\x1b[J"` |
| `Program.cs` (~34) | `Console.CancelKeyPress` |

**Do (in this order).**
1. Add to `ITerminal`: `void WriteTable(TableData table);`, `void WriteBanner(string text);`,
   `void ClearLine();`, `void ClearToEndOfScreen();`.
   `TableData` is a new **domain** type in `src/EtcdTerminal/Terminal/TableData.cs` —
   a plain record holding `IReadOnlyList<string> Columns` and
   `IReadOnlyList<IReadOnlyList<string>> Rows`. It must not reference Spectre.
2. Implement all four in `ConsoleTerminal` — this is the only place that may build a
   Spectre `Table`/`FigletText` or emit raw escape codes.
3. Rewrite the three renderers and `Header` to build a `TableData` / call `WriteBanner`.
4. Replace the raw ANSI in `Spinner` with `ClearLine()` and in `Menu` with
   `ClearToEndOfScreen()`.
5. Move the Ctrl+C handler registration behind a new
   `void OnInterrupt(Action handler);` on `ITerminal`, implemented in `ConsoleTerminal`.

**Do not** do the `Prompt`/Spectre-prompt extraction in this task — that is T16.

**Verify.**
`grep -rn "AnsiConsole\|Spectre\|\\\\x1b" src/EtcdTerminal.App/` must return nothing.
`grep -rn "Console\." src/EtcdTerminal.App/` must return nothing.
All screens must render identically to before.

---

## [ ] T13. Correct `AGENTS.md` so it describes the actual code

**Problem.** The guide names types that do not exist and are therefore unfollowable:
`Panel` (described as "the core design element"), `Message`, and `Palette`
("Colors only from `Palette`" — there is no `Palette` type anywhere).
It also places `Menu` and `Prompt` in `Components/` when they live in `Engine/`,
calls `PressAnyKeyPrompt` "PressAnyKey", and omits `Spinner` and `MenuScreen`.

**Files.** `AGENTS.md`s

**Do.**
- Replace the Components list with the real contents of `Components/` and `Engine/`,
  and explain what `Engine/` is for.
- Replace every mention of `Palette` with `ITheme` / `ThemeStore.Current`.
- Remove `Panel` and `Message`, or move them to an explicit "Planned, not yet implemented"
  section — do not leave them described as if they exist.
- Document the leading-underscore primary-constructor-parameter convention
  (e.g. `Menu(ITerminal _terminal, ...)`), which is used consistently but is unusual
  enough that a newcomer will "correct" it.

**Verify.** Every type named in `AGENTS.md` can be found with `grep` in `src/`.

---

## [ ] T14. Add an architecture test to prevent layering regressions

**Do.**
- Create `src/EtcdTerminal.Tests/` (xUnit) if no test project exists.
- Add tests asserting:
  - no type in `EtcdTerminal.App` references a `Spectre.Console` type,
  - no type in `EtcdTerminal` (domain) references `EtcdTerminal.Infrastructure`,
  - no source file under `EtcdTerminal.App/Screens/` contains the literal `\x1b`.
- Reflection over assembly references plus a file scan is sufficient; do not add a
  dependency on a heavyweight architecture-testing package unless it is already in use.

**Verify.** The tests pass after T12 and fail if a violation is reintroduced.

---

# Phase 4 — Structural design

## [ ] T15. Stop keying control flow on localized display strings

**Problem.** `Menu.Show` returns the selected **display string**, and every screen dispatches
by comparing it back against `LocalizationStore.Current.*`. Two translations that happen to
match silently break dispatch, and rewording a label becomes a behavioural change.

**Sites.** `MainScreen.cs` (~50-73), `UserManagementScreen.cs` (~16-36),
`RoleManagementScreen.cs` (~17-34), `InstanceSelectionScreen.cs` (~24-29, ~105-114),
`SettingsScreen.cs` (~26, 33-37), `Roles/PermissionTypeSelector.cs` (~15-21).

**Do.**
- Add `src/EtcdTerminal/Terminal/MenuItem.cs`:
  `public sealed record MenuItem<TId>(TId Id, string Label);`
- Add a generic overload `TId? Show<TId>(string title, IReadOnlyList<MenuItem<TId>> items, ...)`
  to `Menu`, returning `default` on Esc. Keep the existing `string` overload temporarily so
  the migration can be done screen by screen.
- Migrate screens one at a time. Each screen gets a private `enum` of its actions
  (e.g. `MainMenuAction`) in its own file per the one-type-per-file rule.
- Replace the `switch` **statements** with `switch` **expressions** where the result is a
  value; keep statements only where each arm is a distinct `await` call.
- Delete the `string` overload once the last screen is migrated.

**Model to follow.** `Screens/Keys/KeyBrowseCommand.cs` + `KeyBrowseAction.cs` already do
exactly this correctly — mirror that style.

**Verify.** `grep -rn "choice == LocalizationStore" src/` returns nothing. Every menu still
navigates correctly.

---

## [x] T16. Delete the duplicated spinner and fix its concurrency bugs

**Problem.** `ConsoleTerminal.ShowStatusAsync` (~123-154) and `Spinner.RunAsync` (~7-37) are
verbatim copies — same frames, same 100 ms delay, same `done` flag, same cleanup. Both are
live. Every bug exists twice:
- `await spinnerTask` inside `finally` replaces the caller's real exception with the
  spinner's if the spinner faulted,
- `done` is a plain captured `bool` shared across threads,
- the spinner writes multi-part escape sequences every 100 ms **while** the awaited action
  may also be writing, which tears output.

**Do.**
- Keep `Spinner` (Components) as the single implementation. Per `AGENTS.md`, an async
  progress widget does not belong on `ITerminal`.
- Fix in `Spinner`: use a `CancellationTokenSource` instead of the `bool` flag (this
  combines with T7); in the `finally`, `await` the spinner task inside its own
  `try { } catch { }` so it can never mask the action's exception.
- Migrate `InstanceSelectionScreen` and `PermissionViewScreen` from
  `_terminal.ShowStatusAsync(...)` to the injected `Spinner`.
- Remove `ShowStatusAsync` from `ITerminal` and `ConsoleTerminal`.

**Verify.** `grep -rn "ShowStatusAsync" src/` returns nothing. Connecting and viewing
permissions still show a spinner that disappears cleanly.

---

## [ ] T17. Add the missing `Message` component and remove ~20 duplications

**Problem.** This exact block appears 20+ times:
```csharp
_terminal.WriteLine();
if (ok) _terminal.WriteIndentedLine(success, TerminalColor.Success);
else    _terminal.WriteIndentedLine(failure, TerminalColor.Error);
_terminal.WriteLine();
_pressAnyKey.Show();
```
Sites include `KeyCreateScreen`, `UserManagementScreen` (×6), `RoleManagementScreen` (×3),
`SettingsScreen`, `InstanceSelectionScreen` (×5), `KeyBrowseScreen` (×2),
`KeyImportJsonScreen` (×5).

**Do.**
- Create `src/EtcdTerminal.App/Components/Message.cs` taking `ITerminal` and
  `PressAnyKeyPrompt` via primary constructor, exposing
  `void ShowSuccess(string text)`, `void ShowError(string text)`,
  `void ShowWarning(string text)`, and `void ShowResult(bool ok, string success, string failure)`.
  Each writes a blank line, the indented coloured line, a blank line, then press-any-key.
- Register it in `IocRegistrations` next to the other components.
- Replace every occurrence listed above. Inject `Message` and drop the now-unused
  `PressAnyKeyPrompt` constructor parameter where it becomes redundant.

**Verify.** `grep -rnc "_pressAnyKey.Show()" src/EtcdTerminal.App/Screens/` drops to near zero.

---

## [ ] T18. Make `IAppSettings` immutable

**Problem.** `IAppSettings` exposes setters and `SettingsScreen` mutates the process-global
`AppSettingsStore.Current` directly, then saves. If `Save` throws, memory and disk diverge
permanently. Any code anywhere can write `AppSettingsStore.Current.PageSize = -1`.

**Files.** `src/EtcdTerminal/Configuration/IAppSettings.cs`, `AppSettings.cs`,
`AppSettingsStore.cs`, `src/EtcdTerminal.App/Screens/SettingsScreen.cs`,
`src/EtcdTerminal.Infrastructure/Configuration/JsonBasedSettingsRepository.cs`

**Do.**
- Make `IAppSettings` getters-only; make `AppSettings` an immutable record with `init`
  accessors.
- In `SettingsScreen`, build a **new** settings instance, validate it (`PageSize >= 1`),
  call `_repository.Save(newSettings)`, and only assign `AppSettingsStore.Current` after
  the save succeeds. On failure, show an error via `Message` (T17) and leave the current
  settings untouched.

**Verify.** `grep -rn "AppSettingsStore.Current\.\w* =" src/` matches only the single
assignment in `SettingsScreen` (and `Program.cs` initialisation).

---

## [ ] T19. Split `IEtcdClient` by concern

**Problem.** 30 members across connection lifecycle, key CRUD, user admin, role admin,
grants, and auth toggles. `KeyCreateScreen` needs exactly one of them and depends on all 30.
Test doubles are impractical.

**Do.**
- Split into `IEtcdConnection`, `IEtcdKeyStore`, `IEtcdUserAdmin`, `IEtcdRoleAdmin`,
  `IEtcdAuthAdmin` (one file each, under the matching domain folder).
- Keep `IEtcdClient : IEtcdConnection, IEtcdKeyStore, IEtcdUserAdmin, IEtcdRoleAdmin, IEtcdAuthAdmin`
  so nothing breaks immediately. `DotnetEtcdBasedClient` still implements `IEtcdClient`.
- Register each sub-interface in the container, resolving to the same singleton instance.
- Change each screen to depend on the narrowest interface it actually uses.
- Delete `SearchKeysAsync` — it is dead code (no screen calls it; `KeyBrowseScreen` filters
  its own in-memory list).

**Verify.** No screen's constructor mentions `IEtcdClient` except a screen that genuinely
needs more than one facet.

---

## [ ] T20. Preserve the cause of etcd failures

**Problem.** `CreateUserAsync`, `DeleteRoleAsync`, `GrantRoleAsync` etc. all do
`catch (RpcException) { return false; }`. "Already exists", "permission denied",
"auth not enabled", and "server unreachable" collapse into one indistinguishable `false`,
and the user sees only "Failed to create user".

**Do.**
- Add `src/EtcdTerminal/EtcdOperationResult.cs`:
  a readonly record struct with `bool Success` and `string? ErrorMessage`, plus
  `static Ok()` / `static Fail(string)` factories.
- Change the `bool`-returning admin methods on `IEtcdClient` to return it.
- In `DotnetEtcdBasedClient`, map `RpcException` to `Fail(ex.Status.Detail)` (fall back to
  `ex.Message` when the detail is empty).
- In the screens, pass the message through to `Message.ShowError` (T17) so the user sees
  the actual reason.

**Verify.** Creating a user that already exists shows etcd's real message, not a generic one.

---

# Phase 5 — Polish

## [ ] T21. Deduplicate connection-string validation
Three identical copies of the `Uri.TryCreate` + `http/https` scheme check exist in
`InstanceSelectionScreen` (twice) and `JsonBasedConnectionConfigRepository.IsValid`.
Move the rule onto `EtcdConnectionConfig` as `bool IsConnectionStringValid` (the type
already has a derived-property precedent) and call it from all three places.

## [ ] T22. Merge `AddInstanceInteractive` and `EditInstanceInteractive`
They are ~90% identical in `InstanceSelectionScreen` (~117-172 and ~174-241) — same prompt
sequence, character-for-character identical URI validation, same config construction. Merge
into one private method parameterised by the existing config (`null` = add) and the success
message. **Do T4 and T5 first**, then merge, so the fixes are not duplicated.

## [ ] T23. Deduplicate the ANSI colour properties
`ConsoleTerminal` has ~11 near-identical
`$"\x1b[38;2;{ThemeStore.Current.X.R};{...G};{...B}m"` bodies, each of which rebuilds the
string on **every access** (`StatusBar.Render` reads ~15 per frame). Add
`private static string Fg(RgbColor c)` / `Bg(RgbColor c)` helpers and cache the results per
theme instance.

## [ ] T24. Replace `Thread.Sleep` in the paste-detection path
`Prompt.IsPastedNewLine` calls `Thread.Sleep(40)`, blocking a thread inside a call chain
reached from `async Task ShowAsync`. Make `ReadMultiLine` async (`Task<string?>`) and use
`await Task.Delay(...)`. Update `KeyImportJsonScreen` to await it.

## [ ] T25. Localize the remaining hardcoded English strings
`InstanceSelectionScreen` ~line 47: `$"Failed to connect: {ex.Message}"`.
`KeyImportJsonScreen` ~line 92: `$"Importing {entries.Count} keys..."`.
Add `ILocalization` properties with `{0}` placeholders and implement them in
`EnglishLocalization`.

## [ ] T26. Fix the Ctrl+C double-dispose
`Program.cs` `Cleanup()` (~22-32) resolves `ITerminal` from the container and then disposes
the container. It is called from both the `CancelKeyPress` handler (~34-39) and the
`finally` block (~72-74), so the second call resolves from a **disposed** container. Guard
with an `Interlocked.Exchange`-based run-once flag.

## [ ] T27. Fix the brace-style violation
`Screens/Keys/KeyBrowseLayout.cs` ~31-34 has a single-statement `else` with braces, which
`AGENTS.md` forbids. Remove the braces.

## [ ] T28. Document the encryption key's threat model (or fix it)
`ConfigProtector.LoadOrCreateKey` writes the AES key in plaintext to
`~/.config/etcd-terminal/.key`, **next to** the `config.json` it encrypts. The AES-GCM
implementation itself is correct, but storing the key beside the ciphertext means it
protects against nothing realistic (backups, dotfile repos, exfiltration all capture both).
Either integrate an OS keychain / DPAPI / passphrase-derived key, **or** add an explicit
note to the README stating that this is obfuscation, not protection. Do not leave it
implied that stored passwords are secure.

## [ ] T29. Note the N+1 and full-scan query patterns
No code change required unless it is easy. Document in the README or an issue:
- `GetUsersAsync` / `GetRolesAsync` issue one round-trip per entity.
- `KeyBrowseScreen.LoadKeysAsync` fetches the **entire keyspace** and filters client-side,
  which is a full range scan against a real cluster.

---

# Definition of done

- `cd src && dotnet build` → 0 warnings, 0 errors.
- `grep -rn "AnsiConsole\|Spectre\|\\\\x1b\|Console\." src/EtcdTerminal.App/` → no results.
- `grep -rn "choice == LocalizationStore" src/` → no results.
- `grep -rn "CancellationToken.None" src/` → no results.
- `grep -rn "_client!" src/` → no results.
- Every type named in `AGENTS.md` exists in `src/`.
- Manual smoke test: connect → browse keys → create key → import JSON (object **and**
  array) → manage users → manage roles → view permissions → settings → exit, with no
  crash and no leftover spinner text.
