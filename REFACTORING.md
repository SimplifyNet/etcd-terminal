s# Refactoring Plan: Clean Presentation Architecture On Spectre.Console

This document is the review result and the execution plan for the cleanup that must land before new features are started. It replaces every earlier migration note. `AGENTS.md` holds the permanent rules; this file holds the findings, the decisions and the phase order. The task-by-task checklist with code skeletons is `REFACTORING_TASKS.md`. Delete both files when Phase 7 is accepted.

## 1. Goal

1. Components under `EtcdTerminal.App` (`Components`, `Engine`, `Screens`) describe the UI only in abstract terms: literal text, semantic roles, logical blocks, available actions, navigation. They never touch cursor, width, ANSI, colors, indentation strings, pointer glyphs or terminal dimensions.
2. Infrastructure converts those abstract models into Spectre.Console widgets. Spectre does the measuring, wrapping, cropping, alignment, borders, prompts, spinners and live redraw.
3. Spectre is used wherever it has a widget for the job. Hand-written terminal code is allowed only for proven library gaps, each listed in section 4 with the exact sequence it emits.
4. The footer stays pinned on the last terminal row in every state, including text input, menus, spinners and messages.
5. No workarounds, fallbacks, compatibility shims or duplicated model versions remain when the plan is done.

## 2. Baseline

- Commit `820e55b`, `dotnet build` 0 errors / 0 warnings, `EtcdTerminal.Tests` 214 passed.
- Spectre.Console 0.57.2 (pinned). Facts verified against the package, not memory:
  - `SelectionPrompt<T>` has `CancelResult` / `AddCancelResult` (Escape), `HighlightStyle`, `DisabledStyle`, `PageSize`, `WrapAround`, `MoreChoicesText`, `UseConverter`, `AddChoiceGroup`. Title and converted labels are **markup** and must be escaped.
  - `TextPrompt<T>` has `EditableDefaultValue`, `ShowDefaultValue`, `AllowEmpty`, `Secret`, `ClearOnFinish`, but **no** Escape cancel hook.
  - `IAnsiConsole.WriteAnsi(Action<AnsiWriter>)` is public. `AnsiWriter` offers `SaveCursor`, `RestoreCursor`, `CursorPosition`, `CursorHome`, `EraseInDisplay`, `EnterAltScreen`, `ExitAltScreen`, `ShowCursor`, `HideCursor`, `Write(string)`. It has no scroll-region (DECSTBM) call.
  - `Spectre.Console.Rendering.ControlCode` is **not** public in 0.57.2.
  - `AnsiConsoleExtensions.AlternateScreen(Action)` runs inside `ExclusivityMode.Run`; prompts, `Status` and `Live` inside it throw. Do not use it; use `AnsiWriter.EnterAltScreen/ExitAltScreen`.
  - `Live`, `Status`, `Progress` and all prompts are mutually exclusive on one console. Raw key reads through `IAnsiConsole.Input` inside a `Live` callback are allowed.
  - `Live.Overflow(VerticalOverflow.Crop)` crops to the full console height, not to a sub-region.
  - `Profile.Width`/`Profile.Height` read the console live; `Spectre.Console.Testing.TestConsole` lets tests set them.

## 3. Target architecture

### 3.1 Projects and dependencies (unchanged shape, cleaned content)

```
EtcdTerminal              domain + application: entities, rules, ports, use-case services
EtcdTerminal.Presentation UI contracts: data models + operation interfaces + ITheme + ILocalization
EtcdTerminal.Infrastructure  Spectre mapping, terminal viewport, etcd client, repositories
EtcdTerminal.App          components, engine, screens, composition root, theme, localization text
```

Dependency rules from `AGENTS.md` apply. Additional hard rules after this plan:

- `EtcdTerminal.App` references `EtcdTerminal.Infrastructure` **only** from `Setup/IocRegistrations.cs`.
- No type in `EtcdTerminal.Presentation` exposes cursor, dimensions, display widths, escape strings, indentation or pointer glyphs.
- `System.Console` is used in exactly one Infrastructure class (`ConsoleTerminalSession`) for encoding, `CancelKeyPress` and nothing else.

### 3.2 Presentation contracts (final set)

Models (data only, one public type per file):

| Type | Content |
|---|---|
| `TextRole` | `Default, Primary, Secondary, Success, Danger, Warning, Muted, Subtle, Accent` (unchanged) |
| `StyledText(Text, Role)` | literal run (unchanged) |
| `Block` (abstract record) | base of all body content |
| `TextBlock(IReadOnlyList<IReadOnlyList<StyledText>> Lines)` | free lines of styled runs |
| `TitleBlock(StyledText Title)` | section heading |
| `BannerBlock(string Text)` | application banner |
| `TableBlock(IReadOnlyList<StyledText> Header, IReadOnlyList<IReadOnlyList<StyledText>> Rows)` | columns and cells; `Header` may be empty |
| `ChoiceList<TId>(StyledText? Title, IReadOnlyList<Choice<TId>> Items, IReadOnlyList<Block> Preamble)` | a selection; `Preamble` is content shown above it |
| `Choice<TId>(TId Id, string Label)` | one selectable item, always selectable |
| `FrameModel(IReadOnlyList<Block> Body)` | content of a live frame |
| `StatusBarModel` | hints + session fields + version (unchanged) |

Operations (implemented in Infrastructure):

| Interface | Methods | Spectre behind it |
|---|---|---|
| `ITerminalSession` | `Start()`, `Stop()`, `BeginFrame()`, `EndFrame()`, `OnInterrupt(Action)` | encoding, alt screen, scroll region, window background, cursor visibility |
| `IScreenCanvas` | `NewScreen(StatusBarModel footer)`, `Write(Block)`, `Write(IReadOnlyList<Block>)`, `UpdateFooter(StatusBarModel)`, `WriteException(Exception)` | `Clear`, `Write(IRenderable)`, footer grid on the last row, `WriteException` |
| `ISelectionPrompt` | `TId? Select<TId>(ChoiceList<TId> list)` returning `null` on Escape | `SelectionPrompt<T>` with `CancelResult` |
| `ITextInput` | `ReadLine(prompt, default?)`, `ReadSecret(prompt)` returning `null` on Escape (unchanged) | `TextPrompt<string>` over the Escape-aware console |
| `IStatusIndicator` | `RunAsync(StyledText, Func<Task>)` (unchanged) | `Status` |
| `IKeyReader` | `ConsoleKeyInfo ReadKey()`, `bool KeyAvailable` | `IAnsiConsole.Input` |
| `ILiveFrame` | `T Run<T>(FrameModel initial, LiveFrameEnd end, Func<ILiveFrameUpdater, T> interaction)`; `ILiveFrameUpdater.Update(FrameModel)` | `Live` with `AutoClear` from `LiveFrameEnd { Clear, Keep }` |
| `ITheme`, `RgbColor`, `ILocalization` | unchanged |

Deleted: `ITerminal`, `ITerminalOutput`, `ITerminalCursor`, `ITerminalInput`, `ITerminalStyle`, `ITerminalWidgets`, `ITerminalLifecycle`, `TerminalColor`, `DisplayCells`, `MenuItem`, `IPanelRenderer`, `IStatusBarRenderer`, `IScreenHost`, `PanelModel`, `PanelLine`, `PanelKind`, `ScreenModel`.

### 3.3 Rendering model: streaming content above a pinned footer

The console is split once per session into a scrolling viewport (rows `1..H-1`) and a footer row (`H`). The terminal, not the application, keeps the footer in place: output that reaches the bottom of the viewport scrolls inside the viewport and never touches row `H`. This is the mechanism of tmux and vim status lines and the only way to combine Spectre's streaming prompts with a pinned footer without cursor arithmetic.

- `ITerminalSession.Start()`: UTF-8, enter alternate screen (when `Capabilities.AlternateBuffer`), set window background (section 4), hide cursor, set scroll region `1..H-1`.
- `ITerminalSession.BeginFrame()`/`EndFrame()`: while a live frame draws the footer itself the region is widened to `1..H` and restored to `1..H-1` when the frame is released. A full height frame writes `H` lines with a feed after each of the first `H-1`; inside `1..H-1` that feed scrolls the viewport by one line per paint (regressions V1/V2 in `REFACTORING_TASKS.md`). Changing the region homes the cursor, so the caller re-parks it afterwards.
- `IScreenCanvas.NewScreen(footer)`: erase display, cursor home, draw footer: `SaveCursor`, `CursorPosition(H, 1)`, `Write(footerRenderable)`, `RestoreCursor`. Everything a screen writes afterwards streams from the top of the viewport with plain `console.Write(renderable)`.
- `IScreenCanvas.UpdateFooter(footer)`: the save/position/write/restore sequence only.
- `ITerminalSession.Stop()`: reset scroll region, reset background, show cursor, leave alternate screen.
- Every Spectre interactive widget (`SelectionPrompt`, `TextPrompt`, `Status`, `Live`) runs unchanged inside the viewport. None of them know about the footer; none of them need to.
- The live frame used by the key browser is clamped to `H-1` rows by a thin `Renderable` wrapper in Infrastructure (`Segment.SplitLines(...).Take(height)`), because `Live.Overflow(Crop)` crops to `H`.
- Resize while a screen is open is not tracked. The next `NewScreen`/`UpdateFooter` uses the current `Profile.Height`. Record this as a known limitation; do not add polling.
- Without ANSI support (`Capabilities.Ansi == false`, legacy Windows console) the viewport is not split and the footer is written inline after the banner. This is a capability policy, not a fallback renderer: one `if` in `ConsoleTerminalSession`/`SpectreScreenCanvas`, no second code path in App.

### 3.4 Component to widget map

| Component (App) | Produces | Infrastructure renders with |
|---|---|---|
| `Header` | `BannerBlock` | `FigletText` colored by `ITheme.Banner`, centered |
| `StatusBar` | `StatusBarModel` | two-column `Grid`, right column `NoWrap` + `Overflow.Ellipsis`, pinned on row `H` |
| `Screen` (new; replaces `ScreenShell`, `PressAnyKeyPrompt` framing, `Menu` framing) | `NewScreen(footer)` + banner + body blocks | `IScreenCanvas` |
| `Menu` | `ChoiceList<TId>` | `SelectionPrompt<Choice<TId>>` + `CancelResult`, `HighlightStyle = Accent`, escaped labels |
| `Prompt` | prompt text + policies | `TextPrompt<string>` |
| `Message` | `TextBlock` with role + press-any-key hint | `console.Write(Rows)` then `IKeyReader.ReadKey()` |
| `Spinner` | message + Escape policy | `Status` |
| `PressAnyKeyPrompt` | blocks + hint | streaming write + `ReadKey` (no live frame) |
| `UserListLayout`, `RoleListLayout`, `PermissionViewLayout` | `TitleBlock`, `TableBlock`, `TextBlock` | `Table` (`Border.None` or `Rounded`, header row from model), `Rule`/`Markup` for titles |
| `KeyBrowseLayout` + `KeyBrowseControl` | `FrameModel` with search `TextBlock`, `TableBlock`, pagination, actions | `ILiveFrame.Run`, `Live` + `Rows`/`Table`, selected row highlighted by role |
| `KeyImportJsonScreen` preview | `TableBlock` of key/value, `ChoiceList<bool>` with preamble | `Table` + `SelectionPrompt` |
| `MultiLinePasteReader` | `TextBlock` "waiting"/"pasted N lines" | `ILiveFrame.Run(..., LiveFrameEnd.Keep, ...)` + `IKeyReader` |
| `Program` | nothing | `ITerminalSession`, `IScreenCanvas.WriteException` |

## 4. Decisions and approved exceptions

Each item below either changes visible behavior or writes a byte Spectre cannot write. They are part of this plan; a change to any of them must be recorded here before code changes.

| Id | Decision | Reason |
|---|---|---|
| D1 | Footer pinning uses a terminal scroll region. Infrastructure emits the scroll region sequences through `AnsiWriter.Write`: `ESC[1;{H-1}r` on start, `ESC[1;{H}r` when a live frame begins (`BeginFrame`), `ESC[1;{H-1}r` when it ends (`EndFrame`), `ESC[r` on stop. | Spectre has no scroll-region API. A frame that draws the footer itself has to own the whole terminal: its full height render feeds from row `H-1`, which would scroll the viewport by one line per paint otherwise (regressions V1/V2; the third sequence was approved by the user). Everything else (save/restore cursor, absolute position, erase, alt screen, cursor visibility) goes through `AnsiWriter` methods. |
| D2 | Window background keeps using OSC 11 (`ESC]11;#rrggbb BEL`) and OSC 111 on exit, emitted through `AnsiWriter.Write`. | Spectre has no API for the terminal background; the color is part of the visual language. |
| D3 | Menus are Spectre `SelectionPrompt`. The pointer becomes Spectre's `>` highlight instead of `  ❯ `; item indentation becomes Spectre's. Highlight style is `ITheme.Accent`. | Reusing the library widget is the stated goal; exact glyph parity is not required by `AGENTS.md`. |
| D4 | The non-selectable blank separator in the instance menu is dropped. `Choice` has no `IsSelectable`. Optional: group the instances with `AddChoiceGroup` under a localized heading. | `SelectionPrompt` has no disabled rows; groups are its mechanism for structure. |
| D5 | Footer shortening by priority (`SpectreStatusBarRenderer.Fit`) is replaced by Spectre `Overflow.Ellipsis` on the right column and `Overflow.Crop` on the hints. `DisplayCells` is deleted. | Removes the last home-grown width engine; a narrow terminal still shows the connection name first because it is the first span in the column. |
| D6 | `TextPrompt` Escape cancellation keeps the `EscapableConsole` input decorator, used **only** by `SpectreTextInput`. | `TextPrompt` has no `CancelResult` in 0.57.2. Revisit when it does. |
| D7 | The `MultiLinePasteReader` status line is a `Live` with `AutoClear(false)` (`LiveFrameEnd.Keep`) and raw key reads from `IAnsiConsole.Input`. | `Status` always clears; a `Live` that keeps its last frame is the library way to leave the line on screen. |
| D8 | Alternate screen buffer is entered on start when supported. Nothing from the application remains in the shell scrollback after exit. | Replaces manual full clears on shutdown; one `AnsiWriter` call each way. |
| D9 | `ValuePreview.Preview(value, width)` is deleted; previews are cells with `NoWrap` + `Overflow.Ellipsis`. `ValuePreview` keeps only control-character sanitization and is renamed `DisplayText`. | Width budgets belong to the widget. Sanitization is a safety requirement and stays. |
| D10 | Domain and application code stay in one project (`EtcdTerminal`) in feature folders. No physical Domain/Application split now. | The project is small; the rule that matters (no UI concepts in core) is enforced by tests. Revisit when a second host appears. |
| D11 | Live frame runs the interaction on the caller thread inside `Live.Start(ctx => ...)`. The background pump, semaphore, TCS and lock in `SpectreScreenHost` are deleted. | Matches Spectre's intended lifecycle; removes a thread and three synchronization primitives. |

## 5. Findings

Only items that this plan changes are listed. `file:line` refers to the baseline commit.

### 5.1 App components carry physical rendering

- `Engine/Menu.cs:80-94` builds indentation (`_terminal.Indent`), pointer glyphs (`_terminal.SelectionPointer`) and a hand-rolled selection loop instead of `SelectionPrompt`. `Screens/Keys/KeyBrowseLayout.cs:39-41` does the same for the key list. `Screens/Keys/KeyImportJsonScreen.cs:166` prepends `_style.Indent` to literal text.
- `Components/Message.cs:34-50` reserves rows (`EnsureRoomAbove`), erases the screen bottom (`ClearBelow`) and redraws the footer while preserving the cursor. `Engine/Prompt.cs:14-26,38,56,81` toggles cursor visibility, writes an indent string and repaints the footer twice per question. `Components/StatusBar.cs:12-23` reads and sets the cursor position.
- `Components/MultiLinePasteReader.cs:117-128` emits `\r`, pads with spaces to `WindowWidth` and concatenates escape strings (`_terminal.Subtle`, `_terminal.Reset`).
- `Components/ValuePreview.cs:27-50` measures display cells and truncates to a width budget; `KeyImportJsonScreen.cs:27` hard-codes `_previewValueLength = 60`.
- `Screens/ManageConnectionsScreen.cs:149` and `KeyImportJsonScreen.cs:50,99` write stray blank lines through `ITerminalOutput`.
- `Program.cs:11-67` resolves `ITerminal` from the static container four times (service locator), owns the restart loop and formats the crash message with `TerminalColor`.

### 5.2 Presentation contracts leak terminal geometry

- `Presentation/Terminal/ITerminalOutput.cs`, `ITerminalCursor.cs`, `ITerminalStyle.cs` expose `WindowWidth/Height`, `CursorLeft/Top`, `SetCursorPosition`, escape strings (`Subtle`, `Accent`, `Reset`) and glyphs (`SelectionPointer`, `Indent`).
- `Presentation/Terminal/DisplayCells.cs` is a display-width engine inside a contracts project.
- `Presentation/Terminal/TerminalColor.cs` duplicates `TextRole` and adds `PanelBackground`/`PanelDarkerBackground`/`WindowBackground`/`Banner`, which no component may select.
- `Presentation/PanelKind.cs`: `Selection` and `Actions` are never read by any renderer; `Table` turns a line's spans into cells, which is a hidden protocol between `KeyBrowseLayout`/`UserListLayout` and `SpectrePanelRenderer`.
- `Presentation/IStatusBarRenderer.cs:17-30` publishes `EnsureRoomAbove(rows)` and `ClearBelow()`: cursor-reservation operations dressed as a contract.
- `Presentation/IPanelRenderer.cs` is registered (`IocRegistrations.cs:77`) and consumed by nobody.
- `Presentation/Theming/ITheme.cs:8-9`: `PanelBackground`, `PanelDarkerBackground` are unused.

### 5.3 Infrastructure re-implements what Spectre already does

- `Terminal/ConsoleTerminal.cs`: hand-built SGR/CSI/OSC strings (`\x1b[38;2;...`, `\x1b[2J\x1b[3J\x1b[H`, `\x1b[?25l`), an escape cache and a reflection lookup of theme colors (`ResolveRoleColor`, line 96).
- `Terminal/SpectreCursorPosition.cs` compensates a one-based/two-based disagreement that only exists because the code positions the cursor by hand.
- `Terminal/SpectreScreenHost.cs`: background pump task + `SemaphoreSlim` + `TaskCompletionSource` + `Lock` (lines 23-27, 89-141); after `End()` it re-paints the frame, measures rendered line count (`HandOverRow`, line 115) and parks the cursor at a computed row with a `StreamReserve = 7` magic number so that later streaming output does not collide with the footer.
- `Terminal/SpectreStatusBarRenderer.cs`: `Fit` (lines 130-171) and `Truncate` (lines 208-225) implement width fitting with `DisplayCells`; `EnsureRoomAbove` (60-79) scrolls by printing newlines; `ClearBelow` (87-97) erases by writing `Width` spaces per row; `PanelHeight = 3` works around `Panel` height math; `BottomLine` strips a trailing line break.
- `Terminal/SpectrePanelRenderer.cs:81-97` computes `cellWidth = (contentWidth - ColumnGap * columns) / contentColumns` instead of letting `Table`/`Grid` size columns.
- `IocRegistrations.cs:66-91` constructs five Infrastructure classes by hand with `c.Resolve<SpectreConsoleSource>().Console` because `IAnsiConsole` itself is not registered.

### 5.4 Core and application wiring

- Session lifecycle is split across two state machines kept in sync by screens: `IEtcdConnection.ConnectAsync/DisconnectAsync` and `IConnectionSession.Start/End` (`InstanceSelectionScreen.cs:57-100`, `MainScreen.cs:63-82`). The rollback on failed capability discovery is a nested `try/catch/catch` inside a screen.
- `AppSettings` has four types for two values (`IAppSettings`, `AppSettings`, `IAppSettingsRepository`, `IAppSettingsStore`) and every writer performs `Save` + `Update` by hand (`SettingsScreen.cs:57-59,82-84`, `Program.cs:45-48`).
- `Permissions/EtcdPermission.cs:28-39` carries `DisplayKey`/`DisplayRangeEnd` (display sanitization) on a domain entity.
- `EtcdOperationResult` has no `EtcdOperationFailureKind`; `DotnetEtcdBasedClient.RpcFail` (line 591) always sets `ErrorMessage`, so the localized fallbacks in `UserManagementScreen.cs:90,104,...` are dead.
- `DotnetEtcdBasedClient.cs` (656 lines) implements six interfaces, repeats the same `catch (RpcException)` pair in about twenty methods and mixes two failure policies (throw vs. result).
- `IEtcdClient` is only a DI anchor (`IocRegistrations.cs:114-134`); no App code consumes it.

### 5.5 Tests that exist only because of the above

- Geometry tests: `DisplayCellsTests.cs` (all), `SpectreRenderingTests.cs:46-56,74-99,151-230,249-317`.
- Geometry fakes: `Fakes/RecordingTerminal.cs` (cursor emulator), the cursor/size surface of `Fakes/FakeTerminal.cs`, `RoomRequests`/`ClearRequests` in `Fakes/FakeStatusBarRenderer.cs`.
- Eleven test files use `FakeTerminal` only to inject keys.
- `ArchitectureTests.cs` does not forbid `DisplayCells`/cursor/width APIs in App, does not check Infrastructure → App, and its reflection walker ignores method bodies.

## 6. Phases

Rules for every phase:

- One phase is one reviewable unit; the tree builds with 0 warnings and all tests pass at the end of each phase.
- Migrate complete vertical slices: contract, Infrastructure implementation, DI, every caller, tests. Never keep the old path alive "for now".
- Delete code and its tests in the same commit.
- Follow the conventions in `AGENTS.md` (primary constructors with `_` parameters, member order, braces, blank-line rules, one type per file, `ProjectReference` before `PackageReference`).
- Finish each phase with the verification protocol in section 7 and record the PTY checklist results in the PR description.

### Phase 0 — Housekeeping (no behavior change)

1. Remove dead contracts: `IPanelRenderer` + its registration, `PanelKind.Selection`/`Actions` (change the two producers to `Default`), `ITheme.PanelBackground`/`PanelDarkerBackground` + their values in `ReddyTheme` and `TerminalColor`.
2. Register `IAnsiConsole` itself in `IocRegistrations.RegisterTerminal` (`c => AnsiConsole.Console`, Singleton) and let Infrastructure classes take `IAnsiConsole` via primary constructors; delete `SpectreConsoleSource` and the hand-written factory lambdas.
3. Add architecture tests (section 8) for the rules that already hold; keep them green.

Done when: build/tests green, `rg "SpectreConsoleSource|IPanelRenderer|PanelDarkerBackground"` returns nothing.

### Phase 1 — Terminal session, viewport, canvas and the streaming screens

Introduce the viewport and move every streaming component onto it. This phase deletes the footer-reservation workarounds.

Presentation:
- Add `Block`, `TextBlock`, `TitleBlock`, `BannerBlock`, `TableBlock` (section 3.2). Replace `PanelModel`/`PanelLine`/`PanelKind` everywhere; `PanelKind.Table` producers become `TableBlock` with an explicit header row; `PanelKind.Title` becomes `TitleBlock`; `PanelKind.Banner` becomes `BannerBlock`.
- Add `ITerminalSession`, `IScreenCanvas`, `IKeyReader`.
- Delete `IStatusBarRenderer`, `ITerminalWidgets`, `ITerminalLifecycle`.

Infrastructure:
- `ConsoleTerminalSession : ITerminalSession` — the only `System.Console` user. Implements section 3.3 start/stop with `WriteAnsi`. Owns D1, D2, D8.
- `SpectreScreenCanvas : IScreenCanvas` — `NewScreen`, `Write`, `UpdateFooter`, `WriteException`. Holds the footer draw sequence (save cursor, position, write, restore) and the non-ANSI inline policy.
- `BlockRenderer` — the single `Block` → `IRenderable` mapper (`TextBlock` → `Rows` of `Paragraph`; `TitleBlock` → `Rule` or padded `Paragraph`; `BannerBlock` → `FigletText`; `TableBlock` → `Table` with `Border.None`, `NoWrap` cells, `Overflow.Ellipsis`). Replaces `SpectrePanelRenderer`.
- `StatusBarRenderer` — builds the footer `Grid` (D5). Delete `Fit`, `Truncate`, `EnsureRoomAbove`, `ClearBelow`, `PanelHeight`, `ReservedRows`; keep `BottomLine` only if a real PTY shows the last row scrolling (section 7).
- `SpectreKeyReader : IKeyReader` over `IAnsiConsole.Input`.
- `SpectreStatusIndicator`, `SpectreTextInput` unchanged except: `SpectreTextInput` shows the cursor before and hides it after the prompt through `_console.Cursor.Show(bool)`, escapes the prompt text with `Markup.Escape`.

App:
- New `Components/Screen` (`IScreenCanvas _canvas, Header _header, StatusBar _statusBar`): `Open(params IReadOnlyList<Block> body)` = `NewScreen(_statusBar.BuildModel())` + banner + body; `RefreshFooter()`. Replaces `ScreenShell`; `PressAnyKeyPrompt` becomes `Screen.Open(body + hint)` followed by `_keys.ReadKey()`.
- `StatusBar`: keep `BuildModel()`, delete `Render`, `RenderPreservingCursor`, `EnsureRoomAbove`, `ClearBelow`, the `ITerminalCursor` dependency.
- `Message`: `_canvas.Write(TextBlock lines role)`, `_canvas.Write(hint)`, `_keys.ReadKey()`. No row reservation.
- `Prompt`: only `ITextInput` + trim/empty policies. No cursor, no indent, no footer repaint.
- `Program.cs`: move the run loop into `App/AppRunner` (resolved once; takes `ITerminalSession`, `IScreenCanvas`, `ILocalization`, the two screens, the settings store). `Program.cs` becomes register → verify → `runner.RunAsync()`.
- Remove stray `WriteLine()` calls from `ManageConnectionsScreen` and `KeyImportJsonScreen`.
- `Menu`, `KeyBrowseControl`, `MultiLinePasteReader` still use the old `IScreenHost`/`ITerminal*` in this phase; `SpectreScreenHost` keeps its footer region until Phase 3. This temporary double footer is the only allowed overlap and is removed in Phase 3.

Tests:
- Model tests for `TextBlock`/`TableBlock` producers (`UserListLayout`, `RoleListLayout`, `PermissionViewLayout`, `StatusBar.BuildModel`) stay and are updated to the new types.
- `BlockRenderer` and `StatusBarRenderer`: 4–6 `TestConsole` tests — literal text (brackets, Unicode, connection string), style of one role, header row present, no markup interpretation.
- Delete `SpectreRenderingTests` sections listed in 5.5, `DisplayCellsTests` stays until Phase 4 (still referenced by `ValuePreview`).
- Replace `FakeStatusBarRenderer` with `FakeScreenCanvas` (records `Footer`, written blocks, `NewScreen` count).

### Phase 2 — Menus on `SelectionPrompt`

Presentation: add `Choice<TId>`, `ChoiceList<TId>`, `ISelectionPrompt`; delete `MenuItem`.

Infrastructure: `SpectreSelectionPrompt : ISelectionPrompt`
- `new SelectionPrompt<Choice<TId>>()` with `.Title(Markup.Escape(title))` when present, `.UseConverter(c => Markup.Escape(c.Label))`, `.HighlightStyle(_styles.Resolve(TextRole.Accent))`, `.WrapAround(true)`, `.PageSize(max(3, Profile.Height - ReservedRows))`, `.MoreChoicesText(Markup.Escape(_localization.MoreChoices))`, `.AddCancelResult(Cancel)` where `Cancel` is a private sentinel instance; returns `null` when the sentinel comes back.
- Preamble blocks are written through `BlockRenderer` before the prompt.
- Styles come from `RoleStyleMapper`; the mapper is the only place that touches `ITheme`.

App:
- `Menu.Show<TId>(string? title, IReadOnlyList<Choice<TId>> items, IReadOnlyList<Block>? preamble = null)` → `_screen.Open(preamble)` then `_selection.Select(...)`. The selection loop, `StepSelection`, `displayConverter`, `Frame`, `IScreenHost`, `ITerminal` dependencies are deleted.
- `MenuScreen` unchanged in shape; `InstanceSelectionScreen.PromptForChoice` formats labels itself (`$"{name}  ({connectionString})"`) and drops the separator item (D4).
- `KeyImportJsonScreen.ConfirmImport`: preview as `TableBlock` (key, sanitized value) + question `TextBlock` as preamble, `ChoiceList<bool>` Yes/No.
- `ILocalization`: add `MoreChoices`; remove nothing yet.

Tests: `MenuTests`, `SessionFlowTests`, `ManageConnectionsScreenTests`, `SettingsScreenTests` switch from key injection to `FakeSelectionPrompt` (scripted answers, records `ChoiceList`s). Keep assertions about which choices are offered (action availability) and about cancellation. Add one `TestConsole` test for `SpectreSelectionPrompt`: labels with `[` render literally, Escape returns `null` (`TestConsoleInput.PushKey(ConsoleKey.Escape)`).

### Phase 3 — Live frame for the key browser and the paste reader

Presentation: add `FrameModel`, `ILiveFrame`, `ILiveFrameUpdater`, `LiveFrameEnd`; delete `IScreenHost`, `ScreenModel`.

Infrastructure: `SpectreLiveFrame : ILiveFrame`
- `Run` = `_console.Live(Clamp(Render(initial))).AutoClear(end is Clear).Overflow(Crop).Start(ctx => interaction(new Updater(ctx)))`. `Updater.Update(model)` = `ctx.UpdateTarget(Clamp(Render(model)))` + `ctx.Refresh()`.
- `Clamp` is the `H-1` cropping `Renderable` from section 3.3. On completion hide the cursor again (`_console.Cursor.Show(false)`).
- Delete `SpectreScreenHost`, `SpectreCursorPosition`, the pump, `HandOverRow`, `StreamReserve`.

App:
- `KeyBrowseControl`: `Render` becomes `FrameModel Frame(page, totalPages, totalKeys)`; `ReadCommand` reads through `IKeyReader`. `KeyBrowseScreen.ShowAsync` wraps the loop in `_live.Run(frame, LiveFrameEnd.Clear, updater => { ... updater.Update(frame) ... })` and leaves the frame (returns) before running edit/delete, which then do `_screen.Open(detail blocks)` → `Prompt` → `Message` and re-enter the frame. `ShowDetails`/`Release`/`_frameOpen` are deleted.
- The banner is the first block of the frame (as today); the footer is outside the frame and no longer in the model.
- `KeyBrowseLayout.KeyList` → `TableBlock` with the selected row's cells in `TextRole.Accent`; no pointer glyph, no indent (D3 applies to all selection visuals).
- `MultiLinePasteReader`: `_live.Run(TextBlock(waiting), LiveFrameEnd.Keep, updater => { key loop over _keys; updater.Update(TextBlock(pasted N)) })`. Delete the `\r`/padding/escape-string code. Paste-burst detection logic is unchanged.

Tests: `KeyBrowseScreenTests` use `FakeLiveFrame` (runs the interaction immediately, records frames) + `FakeKeyReader`; `MultiLinePasteReaderTests` the same. `PanelModelTests` for `KeyBrowseLayout` become `FrameModel`/`TableBlock` assertions (selected row role, literal key text, pagination spans).

### Phase 4 — Delete the legacy terminal layer

- Delete `ITerminal*` (all six), `TerminalColor`, `ConsoleTerminal`, `DisplayCells`, `ValuePreview.Preview` (D9; rename the class to `DisplayText`), `EscapableConsole`'s unused forwarding members if any remain, `FakeTerminal`, `RecordingTerminal`, `DisplayCellsTests`, remaining `SpectreRenderingTests` geometry sections.
- Replace every remaining `FakeTerminal.Press(...)` with `FakeKeyReader.Press(...)`.
- `IocRegistrations.RegisterTerminal` registers exactly: `IAnsiConsole`, `EscapableConsole`, `RoleStyleMapper`, `BlockRenderer`, `StatusBarRenderer`, `ITerminalSession`, `IScreenCanvas`, `ISelectionPrompt`, `ITextInput`, `IStatusIndicator`, `IKeyReader`, `ILiveFrame`.
- Run the full PTY checklist (section 7).

Done when: `rg -n "\\x1b|Console\\.|CursorTop|CursorLeft|WindowWidth|WindowHeight|DisplayCells|SelectionPointer|Indent" src/EtcdTerminal.App src/EtcdTerminal.Presentation` returns nothing, and in `src/EtcdTerminal.Infrastructure` the only raw sequences are the four listed in D1/D2 inside `ConsoleTerminalSession`.

### Phase 5 — Application layer cleanup

1. `Session/ConnectionWorkflow` (new application service, `IConnectionWorkflow`): `Task ConnectAsync(EtcdConnectionConfig, CancellationToken)` = connect → capabilities → `session.Start`, with rollback (`session.End` + disconnect) on failure; `Task DisconnectAsync()` = `session.End` + disconnect, swallowing disconnect errors only when a prior error is propagating (pass a flag or expose two methods). `InstanceSelectionScreen` and `MainScreen` call it; the nested try/catch blocks in screens disappear.
2. Settings: delete `IAppSettings`; `IAppSettingsStore` gets `Current`, `Reload()`, `Save(AppSettings)` and wraps `IAppSettingsRepository` internally. `SettingsScreen` and `AppRunner` make one call.
3. Move `DisplayKey`/`DisplayRangeEnd` out of `EtcdPermission`; `PermissionDisplay` applies `DisplayText.Sanitize` to `KeyPrefix`/`RangeEnd`.
4. `EtcdOperationResult` gains `EtcdOperationFailureKind? Kind`; `DotnetEtcdBasedClient.RpcFail` sets it. Screens show the localized headline plus the server detail as a second line (`Message` already supports multi-line text).
5. Localized validation for the duplicate-name rule: move the check from `JsonBasedConnectionConfigRepository.UpdateInstance` to a domain rule (`ConnectionNameMustBeUnique`) evaluated by `ManageConnectionsScreen` before saving; the repository keeps a guard that throws `InvalidOperationException` for programmer error only.

Tests: `SessionFlowTests` move their connect/rollback assertions to `ConnectionWorkflowTests`; `SettingsScreenTests` assert one `Save`; `PermissionDisplayTests` unchanged in expectations.

### Phase 6 — Infrastructure etcd client split (optional, after Phase 5)

Split `DotnetEtcdBasedClient` into `EtcdConnectionHandle` (owns the dotnet-etcd client and `ConnectAsync/DisconnectAsync`), `GrpcErrorTranslator` (static: `Translate`, `IsCancellation`, `Failure`), `EtcdProtoMapper` (static), and `DotnetEtcdKeyStore`, `DotnetEtcdUserAdmin`, `DotnetEtcdRoleAdmin`, `DotnetEtcdAuthAdmin` taking the handle. Wrap every call with one `Guard(Func<Task<T>>)`. Delete `IEtcdClient`; register the role interfaces directly. Behavior and integration tests unchanged.

Skip this phase if the next planned features do not touch the client; record the decision here.

### Phase 7 — Enforcement and closure

1. Add the architecture tests in section 8 that were not addable earlier.
2. Update `AGENTS.md`: replace the "UI Rendering And Migration" wording that refers to migration state with the final rules (section 3 of this file), keep the rule list, remove references to this file.
3. Delete `REFACTORING.md`.

## 7. Verification protocol

Every phase:

```
dotnet build src -nologo -warnaserror
dotnet test src/EtcdTerminal.Tests -nologo
```

PTY checklist (real terminal, 80×24 and a wide/tall size; Linux terminal and, when available, Windows Terminal). Record each line as pass/fail in the PR:

1. Start: banner at top, footer on the last row, cursor hidden, window background applied.
2. Instance menu: arrows move highlight, Enter selects, Esc returns to previous screen/exits, footer stays on the last row while navigating.
3. Text input (add connection): typing, default-value editing, secret mask, Esc cancels, footer stays on the last row during input and after Enter.
4. Spinner (connect): animation visible, Esc cancels, nothing is left behind, footer intact.
5. Message after an action: text, press-any-key hint, footer intact; messages longer than the viewport scroll inside the viewport and never overwrite the footer.
6. Key browser: typing filters live, Up/Down/Left/Right, Enter shows actions, E/D only when permitted, Esc returns; frame replaces itself without duplicates; after edit/delete the browser frame is back and the footer is intact.
7. Import JSON: paste status updates while pasting and the final "Pasted N lines" line stays above the preview; preview table crops long values with an ellipsis; Yes/No selection.
8. Lists (users, roles, permissions): tables with header rows, titles, literal brackets/Unicode, press-any-key, footer intact.
9. Resize the terminal between screens: next screen uses the new size; footer on the new last row.
10. Exit (menu Exit and Ctrl+C): scroll region reset, background reset, cursor visible, previous shell content restored (D8).
11. Crash path (force an exception in a screen): exception panel, press-any-key-to-restart hint, footer behavior acceptable, restart works.

Scenarios that cannot be executed in the available environment are listed as unverified in the PR, not claimed.

## 8. Architecture tests to add

All in `EtcdTerminal.Tests/ArchitectureTests.cs`, source-text based where reflection cannot see method bodies:

- App sources contain none of: `\x1b`, `Console.`, `CursorTop`, `CursorLeft`, `SetCursorPosition`, `WindowWidth`, `WindowHeight`, `DisplayCells`, `SelectionPointer`, `.Indent`, `new string(' '`.
- Presentation sources contain none of the above and no `Spectre`.
- Infrastructure sources contain `\x1b` or `\u001b` only in `ConsoleTerminalSession.cs`, and only the four sequences of D1/D2 (assert by exact substrings).
- Infrastructure has no `using EtcdTerminal.App`.
- Only `EtcdTerminal.App.Setup` references `EtcdTerminal.Infrastructure` (existing, keep).
- Core assembly references neither `Spectre.Console` nor `EtcdTerminal.Presentation` nor `EtcdTerminal.Infrastructure` (existing, keep).
- `System.Console` appears in exactly one Infrastructure file (`ConsoleTerminalSession.cs`).
- Every `IMainMenuEntry` implementation is registered in `IocRegistrations.RegisterScreens` (reflection over the App assembly vs. the resolved `IEnumerable<IMainMenuEntry>`).

## 9. Out of scope

- A second renderer or a UI framework abstraction. The contracts in section 3.2 exist to keep App free of Spectre, not to swap Spectre.
- Byte-for-byte visual parity with the baseline (pointer glyph, indentation, separator rows, footer truncation order). The visual language (roles, banner, footer content, selection highlight, spacing) is preserved.
- Terminal resize tracking during an open screen.
- Localization of server-side error details.
