# Refactoring Tasks (step-by-step checklist)

This is the executable version of `REFACTORING.md`. Read `REFACTORING.md` sections 3 and 4 once for the target picture, then work through this file top to bottom. Do not reorder tasks. Do not start a task until every checkbox of the previous task is ticked.

## How to work

1. Before each task run `git status`; the tree must be clean. One task = one commit. Commit message format: `[r] T<number> <short title>` (example: `[r] T1.3 Screen component replaces ScreenShell`).
2. After each task run, from the repository root:
   ```
   dotnet build src -nologo -warnaserror
   dotnet test src/EtcdTerminal.Tests -nologo
   ```
   Both must succeed. If they do not, fix the task; do not move on and do not "temporarily" disable a test.
3. Follow `AGENTS.md` code style exactly: primary constructors with `_` parameter names, member order fields → constructor → properties → methods, no braces for single-statement bodies, blank line after a group of `var` declarations, file-scoped namespaces, collection expressions, one public type per file, file name = type name.
4. Never add `using Spectre.Console` to a file under `src/EtcdTerminal.App` or `src/EtcdTerminal.Presentation`.
5. Never write `"\x1b"`, `"\u001b"`, `Console.`, `CursorTop`, `CursorLeft`, `WindowWidth`, `WindowHeight`, `new string(' ', ...)` anywhere except in `src/EtcdTerminal.Infrastructure/Terminal/ConsoleTerminalSession.cs` and only where this file says so.
6. When a task says "delete", delete the file with `git rm`; do not comment code out or leave empty files.
7. STOP AND ASK the user when: a Spectre API named here does not exist or behaves differently, a test cannot be made to pass without changing behavior that is not listed in `REFACTORING.md` section 4, or you need a third raw escape sequence.
8. Verified Spectre 0.57.2 facts you may rely on (do not re-verify, do not guess others):
   - `IAnsiConsole.WriteAnsi(Action<AnsiWriter>)`. `AnsiWriter` methods: `SaveCursor(bool stayOnPage)`, `RestoreCursor(bool stayOnPage)` (`false` emits `ESC 7`/`ESC 8`), `CursorPosition(int row, int column)` (1-based), `CursorHome()`, `EraseInDisplay(int mode)` (`2` = whole screen), `EnterAltScreen()`, `ExitAltScreen()`, `ShowCursor()`, `HideCursor()`, `Write(string)`.
   - `console.Profile.Width`, `console.Profile.Height`, `console.Profile.Capabilities.Ansi`, `console.Profile.Capabilities.AlternateBuffer`.
   - `console.Cursor.Show(bool)`, `console.Input.ReadKey(bool intercept)` returns `ConsoleKeyInfo?`, `console.Input.IsKeyAvailable()`.
   - `SelectionPrompt<T>`: `.Title(string markup)`, `.AddChoices(IEnumerable<T>)`, `.UseConverter(Func<T,string>)` (result is markup → escape), `.HighlightStyle(Style)`, `.WrapAround(bool)`, `.PageSize(int)`, `.MoreChoicesText(string)`, `.AddCancelResult(T)`; `console.Prompt(prompt)`.
   - `Live`: `console.Live(IRenderable).AutoClear(bool).Overflow(VerticalOverflow.Crop).Start(Func<LiveDisplayContext, T>)`; `ctx.UpdateTarget(IRenderable)`, `ctx.Refresh()`.
   - `Status`: already used in `SpectreStatusIndicator`, keep it.
   - `Markup.Escape(string)` makes literal text safe for markup parameters.
   - Prompts, `Status` and `Live` cannot be nested in each other. Raw `console.Input.ReadKey` inside a `Live` callback is fine.

---

## Open visual regressions (fix before continuing)

Both reports have one root cause: `a3bf879` (T1.4) made `ConsoleTerminalSession.Start()` set the scroll region `ESC[1;{H-1}r`. Spectre's `Layout` always renders the whole console height: it writes `H` lines and emits a line feed after each of the first `H-1`. The feed from the viewport's last row (`H-1`) scrolls the viewport up by one line inside the region, so every paint shifts the whole content area up by one row while row `H` is never written. The same PTY stream with `ESC[1;{H}r` renders byte-identical to `6c1166f` (verified with a scroll-region-aware emulator: `orig == fixed`).

### V1 Header lost its one-line top indent
- [x] Reported: the header no longer has the one-line indent above it that it always had.
- [x] Confirmed not an intermediate bug: `6c1166f` renders the top line, `d0b9863` loses it; only the scroll region differs in the startup prefix.
- [x] Fixed together with V2 (same root cause); the first screen again starts exactly as it did before T1.4.

### V2 Whole content area repaints on every menu keypress
- [x] Reported: moving in a menu repaints everything except the footer; the area visibly blinks. Unacceptable.
- [x] Root cause: the one-line viewport scroll per paint described above; unchanged glyphs are rewritten after the shift, so the whole content area jumps on every arrow key.
- [x] Fix: while a Live frame owns the console the region is `ESC[1;{H}r`. `ITerminalSession` gained `BeginFrame()`/`EndFrame()`, called by `SpectreScreenHost`: `Begin` widens the region before the pump starts, `End` restores `ESC[1;{H-1}r` after the final paint and re-parks the cursor on the hand-over row (changing the region homes it). Approved by the user as "region for the lifetime of the Live frame"; recorded as the third raw sequence in `REFACTORING.md` D1.
- [x] PTY check: arrow keys in a menu change only the highlighted line, banner top line present, footer on the last row, no content shift over repeated navigation.

Verified on a 24x80 pty with a scroll-region-aware emulator, input `7x Down, Enter, ESC, Down, Enter, ESC`: first paint shows the full banner including its top line, each arrow key changes exactly the two marker rows, zero scroll events, footer stays on row 23, output streamed after the frame lands on the hand-over rows, and every checkpoint screen matches `6c1166f` running the same input (the only difference is a connection-cancellation race on the last step). Not verified: terminal resize while a frame is open, the non ANSI backend, colors in a real terminal emulator.

---

## Phase 0 — Housekeeping

### T0.1 Remove dead contracts
- [x] Delete `src/EtcdTerminal.Presentation/IPanelRenderer.cs`.
- [x] In `IocRegistrations.cs` remove the `IPanelRenderer` registration and the `: IPanelRenderer` on `SpectrePanelRenderer`.
- [x] In `PanelKind.cs` remove `Selection` and `Actions`. In `KeyBrowseLayout.cs` change the two usages to `PanelKind.Default` (just drop the second argument).
- [x] In `ITheme.cs` remove `PanelBackground` and `PanelDarkerBackground`; remove them from `ReddyTheme.cs` and from `TerminalColor.cs`.
- [x] Build, test, commit.

### T0.2 Register IAnsiConsole once
- [x] In `IocRegistrations.RegisterTerminal` add `.Register<IAnsiConsole>(c => AnsiConsole.Console, LifetimeType.Singleton)` (needs `using Spectre.Console;` — allowed only in this file under App because it is the composition root; this using already exists indirectly via Infrastructure types, keep it minimal).
- [x] Change `SpectrePanelRenderer`, `SpectreStatusBarRenderer`, `SpectreStatusIndicator`, `SpectreScreenHost` registrations to plain `.Register<X>(LifetimeType.Singleton)` so the container injects `IAnsiConsole` by constructor.
- [x] `EscapableConsole` registration: `.Register<EscapableConsole>(c => new(c.Resolve<IAnsiConsole>()), LifetimeType.Singleton)`.
- [x] Delete `SpectreConsoleSource.cs`.
- [x] Build, test, commit.

---

## Phase 1 — Viewport, canvas and streaming screens

### T1.1 New block models in Presentation
Create these files in `src/EtcdTerminal.Presentation/` (namespace `EtcdTerminal.Presentation`):

- [x] `Block.cs`: `public abstract record Block;`
- [x] `TextBlock.cs`: `public sealed record TextBlock(IReadOnlyList<IReadOnlyList<StyledText>> Lines) : Block` plus two static helpers: `public static TextBlock Line(params IReadOnlyList<StyledText> spans) => new([spans]);` and `public static TextBlock Blank() => new([[]]);`
- [x] `TitleBlock.cs`: `public sealed record TitleBlock(StyledText Title) : Block;`
- [x] `BannerBlock.cs`: `public sealed record BannerBlock(string Text) : Block;`
- [x] `TableBlock.cs`: `public sealed record TableBlock(IReadOnlyList<StyledText> Header, IReadOnlyList<IReadOnlyList<StyledText>> Rows) : Block;`
- [x] Build (nothing uses them yet), commit.

### T1.2 Replace PanelModel with Block everywhere
- [x] `Header.BuildModel()` returns `BannerBlock` (`new BannerBlock("etcd-terminal")`).
- [x] `KeyBrowseLayout`: `Search`, `Pagination`, `Detail`, `Selected` return `TextBlock`; `KeyList` returns `TableBlock` with empty `Header` and rows `[marker, key, value]` (marker stays for now, removed in T3.3); `Actions` returns `TextBlock`. Return type of every method: `Block`.
- [x] `UserListLayout`, `RoleListLayout`, `PermissionViewLayout`: return `IReadOnlyList<Block>`; tables become `TableBlock` with the header row moved into `Header`; titles become `TitleBlock`; notices become `TextBlock.Line(new StyledText(text, TextRole.Warning))`.
- [x] `KeyImportJsonScreen.ConfirmImport` preview: build a `List<Block>` (title line as `TextBlock`, entries as `TableBlock(Header: [], Rows: [[key, value]])`, "more" line, question line). Keep the `Menu.ShowFramed(..., notices: blocks)` call for now; change the `notices` parameter type in `Menu` to `IReadOnlyList<Block>?`.
- [x] `Menu.Frame`: title → `TitleBlock`, rows → `TextBlock`.
- [x] `PressAnyKeyPrompt.Hint()` → `TextBlock` of two lines.
- [x] `ScreenModel`: `Header` becomes `BannerBlock?`, `Body` becomes `IReadOnlyList<Block>`.
- [x] `SpectrePanelRenderer`: rename file and class to `BlockRenderer`. Public API: `IRenderable Render(Block block)`. Implementation with a `switch` expression on the record type: `TextBlock` → `new Rows(lines.Select(_styles.Build))`; `TitleBlock` → `new Padder(_styles.Build([title]), new Padding(0, 1, 0, 1))`; `BannerBlock` → `FigletText` colored with `ITheme.Banner`, `.Centered()`; `TableBlock` → `new Table().NoBorder().Expand()`; add one `TableColumn` per header cell (or per widest row when header is empty) with `NoWrap = true`; `ShowHeaders = header.Count > 0`; every cell is `_styles.Build([span])` with `Overflow = Overflow.Ellipsis`. Delete all `cellWidth` arithmetic, `ColumnGap`, `MarkerColumns`, `PaddingRight`.
- [x] `SpectreScreenHost.Main` uses `_panels.Render(...)`.
- [x] Delete `PanelModel.cs`, `PanelLine.cs`, `PanelKind.cs`.
- [x] Update tests: `PanelModelTests` → rename to `LayoutModelTests`; assertions use `TextBlock.Lines[i]` and `TableBlock.Rows[i]`. `SpectreRenderingTests` panel tests call `BlockRenderer.Render`. Delete the tests at `SpectreRenderingTests.cs:46-56` (column-0 indentation) and `:74-99` (row count/line length).
- [x] Build, test, commit.

### T1.3 New operation contracts in Presentation
- [x] `ITerminalSession.cs`:
  ```csharp
  public interface ITerminalSession
  {
      void Start();
      void Stop();
      void OnInterrupt(Action handler);
  }
  ```
- [x] `IScreenCanvas.cs`:
  ```csharp
  public interface IScreenCanvas
  {
      void NewScreen(StatusBarModel footer);
      void Write(Block block);
      void Write(IReadOnlyList<Block> blocks);
      void UpdateFooter(StatusBarModel footer);
      void WriteException(Exception exception);
  }
  ```
- [x] `IKeyReader.cs`:
  ```csharp
  public interface IKeyReader
  {
      bool KeyAvailable { get; }
      ConsoleKeyInfo ReadKey();
  }
  ```
- [x] Build, commit.

### T1.4 ConsoleTerminalSession (Infrastructure)
Create `src/EtcdTerminal.Infrastructure/Terminal/ConsoleTerminalSession.cs`. This is the only file allowed to contain `Console.` and raw escape strings.

```csharp
public sealed class ConsoleTerminalSession(IAnsiConsole _console, ITheme _theme) : ITerminalSession
{
    private const int FooterRows = 1;

    public void Start()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        if (!_console.Profile.Capabilities.Ansi)
            return;

        _console.WriteAnsi(writer =>
        {
            if (_console.Profile.Capabilities.AlternateBuffer)
                writer.EnterAltScreen();

            writer.Write(BackgroundSequence(_theme.WindowBackground));
            writer.HideCursor();
            writer.Write(ScrollRegionSequence(_console.Profile.Height - FooterRows));
            writer.EraseInDisplay(2);
            writer.CursorHome();
        });
    }

    public void Stop()
    {
        if (!_console.Profile.Capabilities.Ansi)
            return;

        _console.WriteAnsi(writer =>
        {
            writer.Write(ResetScrollRegionSequence);
            writer.Write(ResetBackgroundSequence);
            writer.EraseInDisplay(2);
            writer.CursorHome();
            writer.ShowCursor();

            if (_console.Profile.Capabilities.AlternateBuffer)
                writer.ExitAltScreen();
        });
    }

    public void OnInterrupt(Action handler) => Console.CancelKeyPress += (_, args) =>
    {
        args.Cancel = true;
        handler();
    };

    // The four sequences Spectre cannot emit (REFACTORING.md D1, D2).
    private const string ResetScrollRegionSequence = "\u001b[r";
    private const string ResetBackgroundSequence = "\u001b]111\u0007";
    private static string ScrollRegionSequence(int lastRow) => $"\u001b[1;{lastRow}r";
    private static string BackgroundSequence(RgbColor c) => $"\u001b]11;#{c.R:X2}{c.G:X2}{c.B:X2}\u0007";
}
```
(Move the constants above the constructor to satisfy member ordering.)

- [x] Create the file as above.
- [x] Register `ITerminalSession` → `ConsoleTerminalSession` Singleton.
- [x] Build, commit.

### T1.5 SpectreScreenCanvas and StatusBarRenderer (Infrastructure)
- [x] Rename `SpectreStatusBarRenderer` → `StatusBarRenderer`. Keep only `Build(StatusBarModel)` and the private `Right(...)`/`Spaced(...)` helpers. `Build` returns `IRenderable`:
  ```csharp
  var grid = new Grid { Expand = true };
  grid.AddColumn(new GridColumn { NoWrap = true, Padding = new Padding(2, 0, 0, 0) });
  grid.AddColumn(new GridColumn { NoWrap = true, Alignment = Justify.Right, Padding = new Padding(0, 0, 2, 0) });
  var left = _styles.Build(model.Hints); left.Overflow = Overflow.Crop;
  var right = _styles.Build(Right(model)); right.Overflow = Overflow.Ellipsis;
  grid.AddRow(left, right);
  return new BottomLine(grid);
  ```
  Keep the `BottomLine` nested class. Delete `Fit`, `Fits`, `WidthOf`, `RightWidth`, `Truncate`, `Write`, `EnsureRoomAbove`, `ClearBelow`, all `const int`s except none, the `ITerminalCursor` parameter, `using EtcdTerminal.Presentation.Terminal`.
- [x] Create `SpectreScreenCanvas.cs`:
  ```csharp
  public sealed class SpectreScreenCanvas(IAnsiConsole _console, BlockRenderer _blocks, StatusBarRenderer _footer) : IScreenCanvas
  {
      public void NewScreen(StatusBarModel footer)
      {
          _console.Clear(true);

          if (_console.Profile.Capabilities.Ansi)
              UpdateFooter(footer);
          else
              _console.Write(_footer.Build(footer));
      }

      public void Write(Block block) => _console.Write(_blocks.Render(block));

      public void Write(IReadOnlyList<Block> blocks)
      {
          foreach (var block in blocks)
              Write(block);
      }

      public void UpdateFooter(StatusBarModel footer)
      {
          if (!_console.Profile.Capabilities.Ansi)
              return;

          var renderable = _footer.Build(footer);

          _console.WriteAnsi(writer => writer.SaveCursor(false).CursorPosition(_console.Profile.Height, 1));
          _console.Write(renderable);
          _console.WriteAnsi(writer => writer.RestoreCursor(false));
      }

      public void WriteException(Exception exception) => _console.WriteException(exception);
  }
  ```
- [x] Create `SpectreKeyReader.cs`: `KeyAvailable => _console.Input.IsKeyAvailable()`, `ReadKey() => _console.Input.ReadKey(true) ?? throw new InvalidOperationException("No key available.")`.
- [x] `SpectreTextInput`: wrap each `_console.Prompt(...)` with `_console.Cursor.Show(true)` before and `_console.Cursor.Show(false)` in `finally`; pass `Markup.Escape(prompt)` to `TextPrompt`.
- [x] Register `IScreenCanvas` → `SpectreScreenCanvas`, `IKeyReader` → `SpectreKeyReader`, `BlockRenderer`, `StatusBarRenderer` (all Singleton). Remove the `IStatusBarRenderer` registration and delete `IStatusBarRenderer.cs`.
- [x] `SpectreScreenHost`: temporarily change its footer region to use `_footer.Build(model.Footer)` directly (no `Fit`). It is deleted in Phase 3.
- [x] Build (App does not compile yet because `StatusBar` uses `IStatusBarRenderer` — continue to T1.6 before committing).

### T1.6 Streaming components in App
- [x] `StatusBar`: constructor `(IAppInfo _appInfo, IConnectionSession _session, ILocalization _localization)`; keep only `BuildModel()`, `Username(...)`, `BuildHints()`.
- [x] New `Components/Screen.cs`:
  ```csharp
  public sealed class Screen(IScreenCanvas _canvas, Header _header, StatusBar _statusBar)
  {
      public void Open(IReadOnlyList<Block>? body = null)
      {
          _canvas.NewScreen(_statusBar.BuildModel());
          _canvas.Write(_header.BuildModel());

          if (body is not null)
              _canvas.Write(body);
      }

      public void Write(Block block) => _canvas.Write(block);

      public void Write(IReadOnlyList<Block> blocks) => _canvas.Write(blocks);
  }
  ```
- [x] Delete `ScreenShell.cs`; replace every `_shell.Show()` with `_screen.Open()`.
- [x] `PressAnyKeyPrompt(Screen _screen, IKeyReader _keys, ILocalization _localization)`: `Show(body)` = `_screen.Open([.. body, TextBlock.Blank(), TextBlock.Line(new StyledText(_localization.PressAnyKey, TextRole.Muted))])` then `_keys.ReadKey()`.
- [x] `Message(Screen _screen, IKeyReader _keys, ILocalization _localization)`: `Show(text, role)` writes `TextBlock.Blank()`, one `TextBlock` with every line in `role`, `TextBlock.Blank()`, the press-any-key line in `TextRole.Muted`, then `_keys.ReadKey()`. Signature of the public methods unchanged; `TerminalColor` → `TextRole`.
- [x] `Prompt(ITextInput _textInput)`: remove every other dependency, the cursor calls, `_output.Write(_style.Indent)`, `_statusBar.*`. Keep the three `Ask`/`Secret` methods and trim/empty policies.
- [x] `ManageConnectionsScreen`: remove `ITerminalOutput _terminal` and the `_terminal.WriteLine()` call. `KeyImportJsonScreen`: remove `ITerminalOutput _output`, `ITerminalStyle _style`, both `_output.WriteLine()` calls, and the `_style.Indent +` prefix.
- [x] New `App/AppRunner.cs` (namespace `EtcdTerminal.App`):
  ```csharp
  public sealed class AppRunner(ITerminalSession _terminal, IScreenCanvas _canvas, ILocalization _localization, IKeyReader _keys, IDIContainerProvider _container)
  ```
  with `Task RunAsync()` containing the loop currently in `Program.cs` (scope per iteration, settings load, `InstanceSelectionScreen`, `MainScreen`, `catch` → `_canvas.WriteException(ex)`, `_canvas.Write(TextBlock.Line(new StyledText(_localization.PressAnyKeyRestart, TextRole.Muted)))`, `_keys.ReadKey()`), and `Start()`/`Stop()` wrappers around `_terminal`. `Program.cs` becomes: register, verify, resolve `AppRunner`, `runner.Start()`, `OnInterrupt(() => { runner.Stop(); Environment.Exit(0); })`, `try { await runner.RunAsync(); } finally { runner.Stop(); }`. `Stop` must be idempotent (keep the `Interlocked.Exchange` guard inside `AppRunner`).
- [x] `IocRegistrations`: register `Screen`, `AppRunner`; remove `ScreenShell`.
- [x] Tests: create `Fakes/FakeScreenCanvas.cs` (records `Footers`, `Blocks`, `NewScreenCount`) and `Fakes/FakeKeyReader.cs` (queue + `Press(ConsoleKey)`/`Press(char)`); delete `Fakes/FakeStatusBarRenderer.cs`. Update `PromptTests`, `StatusBarPersistenceTests`, `LayoutTests`, `SessionFlowTests`, `SettingsScreenTests`, `ManageConnectionsScreenTests` constructors. Delete `SpectreRenderingTests.cs:151-230` (Fit) and `:249-317` (EnsureRoomAbove/ClearBelow). Add two `TestConsole` tests for `StatusBarRenderer.Build`: literal connection string with brackets appears; version appears.
- [x] Build, test, commit.

### T1.7 Phase 1 PTY check
- [x] Run the app in a real terminal. Check: footer on the last row at start; add a connection (text prompts) — footer stays on the last row while typing; a message after saving — footer intact. If the footer line scrolls the screen by one row, STOP AND ASK (the `BottomLine` wrapper may need adjusting).
- [x] Record results in the commit message of the next task or in the PR description.

---

## Phase 2 — Menus on SelectionPrompt

### T2.1 Contracts
- [x] `Choice.cs`: `public sealed record Choice<TId>(TId Id, string Label);`
- [x] `ChoiceList.cs`: `public sealed record ChoiceList<TId>(string? Title, IReadOnlyList<Choice<TId>> Items);`
- [x] `ISelectionPrompt.cs`: `TId? Select<TId>(ChoiceList<TId> list) where TId : notnull;` — returns `default`/null on Escape. To keep value-type ids (enums) distinguishable from "cancelled", return `Choice<TId>?` instead: `Choice<TId>? Select<TId>(ChoiceList<TId> list);`. Use this second signature.
- [x] Delete `Presentation/Terminal/MenuItem.cs` after T2.3 (keep until callers are updated).
- [x] `ILocalization`: add `string MoreChoices { get; }` → `EnglishLocalization`: `"(move up and down to reveal more)"`.

### T2.2 SpectreSelectionPrompt (Infrastructure)
```csharp
public sealed class SpectreSelectionPrompt(IAnsiConsole _console, RoleStyleMapper _styles, ILocalization _localization) : ISelectionPrompt
{
    private const int ReservedRows = 12;

    public Choice<TId>? Select<TId>(ChoiceList<TId> list)
    {
        var cancel = new Choice<TId>(default!, string.Empty);
        var prompt = new SelectionPrompt<Choice<TId>>()
            .AddChoices(list.Items)
            .UseConverter(choice => Markup.Escape(choice.Label))
            .HighlightStyle(_styles.Resolve(TextRole.Accent))
            .WrapAround(true)
            .PageSize(Math.Max(3, _console.Profile.Height - ReservedRows))
            .MoreChoicesText(Markup.Escape(_localization.MoreChoices))
            .AddCancelResult(cancel);

        if (!string.IsNullOrEmpty(list.Title))
            prompt.Title(Markup.Escape(list.Title));

        var selected = _console.Prompt(prompt);

        return ReferenceEquals(selected, cancel) ? null : selected;
    }
}
```
- [x] Create, register `ISelectionPrompt` → `SpectreSelectionPrompt` Singleton.
- [x] `TestConsole` test: two choices, push `DownArrow` + `Enter` → second choice; push `Escape` → null; a label containing `[x]` is shown literally in `console.Output`.

### T2.3 Menu and callers (App)
- [x] `Engine/Menu.cs` becomes:
  ```csharp
  public sealed class Menu(Screen _screen, ISelectionPrompt _selection)
  {
      public Choice<TId>? Show<TId>(string? title, IReadOnlyList<Choice<TId>> items, IReadOnlyList<Block>? preamble = null)
      {
          _screen.Open(preamble);

          return _selection.Select(new ChoiceList<TId>(title, items));
      }
  }
  ```
  Delete `ShowFramed`, `Frame`, `StepSelection`, `displayConverter`.
- [x] An empty item list returns null without opening a screen (the old menu did the same; `SelectionPrompt` throws on an empty list).
- [x] Replace every `MenuItem<T>` with `Choice<T>` and every `_menu.ShowFramed(` with `_menu.Show(` in: `MenuScreen`, `InstanceSelectionScreen`, `MainScreen`, `ManageConnectionsScreen`, `SettingsScreen`, `PermissionTypeSelector`, `PermissionScopeSelector`, `KeyImportJsonScreen`.
- [x] `InstanceSelectionScreen.PromptForChoice`: labels are `$"{i.Name}  ({i.ConnectionString})"` built directly; remove the separator item and the converter lambda; the "no connections" notice goes in `preamble`.
- [x] `KeyImportJsonScreen.ConfirmImport`: `preamble` = preview blocks; items `[new(true, Yes), new(false, No)]`; `return _menu.Show(null, items, preview)?.Id ?? false;`.
- [x] Delete `MenuItem.cs`.
- [x] Tests: create `Fakes/FakeSelectionPrompt.cs` (queue of answers: `Answer<TId>(TId id)` / `Cancel()`, records every `ChoiceList`). Rewrite `MenuTests`, `ScreenFrameTests` (delete the live-frame ones, keep "cancel returns null" and "preamble is written before the prompt" via `FakeScreenCanvas`), `SessionFlowTests`, `ManageConnectionsScreenTests`, `SettingsScreenTests`, `KeyBrowseScreenTests` (where they use menus) to script answers instead of key presses. Assertions on which labels/ids are offered stay.
- [x] Build, test, commit.

### T2.4 Phase 2 PTY check
- [x] Instance menu, main menu, settings, manage connections: arrows, Enter, Esc; highlight in accent color; footer on the last row throughout.

Verified on a 24x80 pty with a scroll region aware emulator (`/tmp/opencode/steps_t24.py`, `/tmp/opencode/vt.py`): the instance menu, Manage Connections, Settings (including a toggle that composes the menu again) and the main menu after connecting to `etcd-local` were driven with arrows, Enter and Esc. Arrows move only the marker rows, the highlighted row carries the accent colour `220;95;51` while the other rows are default, the footer stays on row 23 at every step (the session bullet appears after connect and disappears after Esc), there are zero scroll events, the banner top line stays on row 0, and Esc from the instance menu exits the application. Not verified: terminal resize, the non ANSI backend, a real terminal emulator's colours.

---

## Phase 3 — Live frame for key browser and paste reader

### T3.1 Contracts
- [x] `FrameModel.cs`: `public sealed record FrameModel(IReadOnlyList<Block> Body);`
- [x] `LiveFrameEnd.cs`: `public enum LiveFrameEnd { Clear, Keep }`
- [x] `ILiveFrameUpdater.cs`: `void Update(FrameModel model);`
- [x] `ILiveFrame.cs`: `T Run<T>(FrameModel initial, LiveFrameEnd end, Func<ILiveFrameUpdater, T> interaction);`

### T3.2 SpectreLiveFrame (Infrastructure)
```csharp
public sealed class SpectreLiveFrame(IAnsiConsole _console, BlockRenderer _blocks) : ILiveFrame
{
    private const int FooterRows = 1;

    public T Run<T>(FrameModel initial, LiveFrameEnd end, Func<ILiveFrameUpdater, T> interaction)
    {
        try
        {
            return _console.Live(Render(initial))
                .AutoClear(end is LiveFrameEnd.Clear)
                .Overflow(VerticalOverflow.Crop)
                .Start(ctx => interaction(new Updater(ctx, this)));
        }
        finally
        {
            _console.Cursor.Show(false);
        }
    }

    private IRenderable Render(FrameModel model) =>
        new ClampedRows(new Rows(model.Body.Select(_blocks.Render)), _console.Profile.Height - FooterRows);

    private sealed class Updater(LiveDisplayContext _ctx, SpectreLiveFrame _owner) : ILiveFrameUpdater
    {
        public void Update(FrameModel model)
        {
            _ctx.UpdateTarget(_owner.Render(model));
            _ctx.Refresh();
        }
    }

    /// Crops to the viewport above the footer; Live itself crops only at full console height.
    private sealed class ClampedRows(IRenderable _inner, int _maxLines) : Renderable
    {
        protected override Measurement Measure(RenderOptions options, int maxWidth) => _inner.Measure(options, maxWidth);

        protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
        {
            var lines = Segment.SplitLines(_inner.Render(options, maxWidth));
            var result = new List<Segment>();

            foreach (var line in lines.Take(Math.Max(1, _maxLines)))
            {
                result.AddRange(line);
                result.Add(Segment.LineBreak);
            }

            return result;
        }
    }
}
```
(`SegmentLine` is a `List<Segment>`; it has no `Append`, so the loop above is the way.)
- [x] Create, register `ILiveFrame` → `SpectreLiveFrame` Singleton.
- [ ] Delete `SpectreScreenHost.cs`, `SpectreCursorPosition.cs`, `Presentation/IScreenHost.cs`, `Presentation/ScreenModel.cs`, test fake `FakeScreenHost.cs` (replace by `FakeLiveFrame` in T3.3).

### T3.3 Key browser (App)
- [ ] `KeyBrowseControl(IKeyReader _keys, KeyBrowseLayout _layout, Header _header, IConnectionSession _session)`: replace `Render`/`ShowDetails`/`Release`/`_frameOpen` with `public FrameModel Frame(IReadOnlyList<EtcdKeyValue> pageKeys, int totalPages, int totalKeys) => new([_header.BuildModel(), .. Body(...)]);`. `ReadCommand` uses `_keys.ReadKey()`.
- [ ] `KeyBrowseLayout.KeyList`: `TableBlock(Header: [], Rows: [[key, value]])` with both cells `TextRole.Accent` on the selected row and `TextRole.Primary` otherwise. Remove the marker column and the `ITerminalStyle` dependency.
- [ ] `KeyBrowseScreen.ShowAsync`:
  ```csharp
  while (true)
  {
      var command = _live.Run(Frame(), LiveFrameEnd.Clear, updater =>
      {
          while (true)
          {
              var (page, totalPages) = CurrentPage();
              var command = _control.ReadCommand(page, totalPages);

              if (command.Action is KeyBrowseAction.SearchChanged)
                  ApplyFilter();

              if (command.Action is KeyBrowseAction.Edit or KeyBrowseAction.Delete or KeyBrowseAction.Exit)
                  return command;

              updater.Update(Frame());
          }
      });

      switch (command.Action) { Edit → await EditKeyAsync; Delete → await DeleteKeyAsync; Exit → return; }
  }
  ```
  `Frame()` builds the current page from `_pager` and `_control`. `EditKeyAsync`/`DeleteKeyAsync` start with `_screen.Open([detail blocks])` instead of `_control.ShowDetails(...)`.
- [ ] `MultiLinePasteReader(IKeyReader _keys, ILiveFrame _live, Screen _screen, ILocalization _localization)`: `_screen.Write(TextBlock.Line(prompt))`, then `_live.Run(Status(0), LiveFrameEnd.Keep, updater => { ...existing key loop with _keys...; updater.Update(Status(CountLines(buffer))); })`. `Status(n)` returns `TextBlock.Line(new StyledText(text, n == 0 ? TextRole.Subtle : TextRole.Accent))`. Delete `RenderPasteStatus`, the cursor calls and the `_statusBar` calls. `ReadAsync` becomes synchronous inside `Run`; the `IsPastedNewLineAsync` delay becomes `Thread.Sleep(_pasteBurstThresholdMs)`; the public method may stay `Task<string?> ReadAsync` returning `Task.FromResult`.
- [ ] Tests: `Fakes/FakeLiveFrame.cs` (runs `interaction` immediately with an updater that records every `FrameModel`). Update `KeyBrowseScreenTests`, `MultiLinePasteReaderTests`, `LayoutModelTests` (selected row role, literal key).
- [ ] Build, test, commit.

### T3.4 Phase 3 PTY check
- [ ] Key browser: type to filter, arrows, Enter → actions, E/D, Esc; frame redraws in place; after edit the browser is back; footer intact.
- [ ] Import JSON: paste → counter updates → final "Pasted N lines" stays → preview → Yes/No.

---

## Phase 4 — Delete the legacy terminal layer

### T4.1 Delete
- [ ] `git rm` these files: `Presentation/Terminal/ITerminal.cs`, `ITerminalOutput.cs`, `ITerminalCursor.cs`, `ITerminalInput.cs`, `ITerminalStyle.cs`, `ITerminalWidgets.cs`, `ITerminalLifecycle.cs`, `TerminalColor.cs`, `DisplayCells.cs`; `Infrastructure/Terminal/ConsoleTerminal.cs`; `Tests/Fakes/FakeTerminal.cs`, `Tests/Fakes/RecordingTerminal.cs`, `Tests/DisplayCellsTests.cs`.
- [ ] Move `Presentation/Terminal/ITextInput.cs` to `Presentation/ITextInput.cs` (namespace `EtcdTerminal.Presentation`); delete the empty `Terminal` folder.
- [ ] `ValuePreview` → rename to `DisplayText`, keep `Sanitize` only; delete `Preview` and `_previewValueLength`. Update `ValuePreviewTests` → `DisplayTextTests` (sanitization cases only).
- [ ] `Spinner(IKeyReader _keys, IStatusIndicator _status)`.
- [ ] `IocRegistrations.RegisterTerminal` contains exactly: `IAnsiConsole`, `EscapableConsole`, `RoleStyleMapper`, `BlockRenderer`, `StatusBarRenderer`, `ITerminalSession`, `IScreenCanvas`, `ISelectionPrompt`, `ITextInput`, `IStatusIndicator`, `IKeyReader`, `ILiveFrame`.
- [ ] Run: `rg -n '\\x1b|\\u001b|Console\.|CursorTop|CursorLeft|WindowWidth|WindowHeight|DisplayCells|SelectionPointer|\.Indent\b' src/EtcdTerminal.App src/EtcdTerminal.Presentation` → must print nothing. `rg -n '\\u001b|\\x1b' src/EtcdTerminal.Infrastructure` → only `ConsoleTerminalSession.cs`.
- [ ] Build, test, commit.

### T4.2 Architecture tests
Add to `ArchitectureTests.cs` (source-text tests like the existing `AppSourcesContainNoEscapeLiterals`):
- [ ] `AppAndPresentationSourcesContainNoGeometryApis` — forbidden substrings: `CursorTop`, `CursorLeft`, `SetCursorPosition`, `WindowWidth`, `WindowHeight`, `DisplayCells`, `SelectionPointer`, `new string(' '`.
- [ ] `InfrastructureEscapeSequencesLiveOnlyInTerminalSession` — every Infrastructure `.cs` except `ConsoleTerminalSession.cs` has no `\u001b`/`\x1b`.
- [ ] `InfrastructureDoesNotReferenceApp` — no `using EtcdTerminal.App` in Infrastructure.
- [ ] `SystemConsoleIsUsedOnlyByTerminalSession` — `Console.` appears only in `ConsoleTerminalSession.cs` within Infrastructure.
- [ ] `EveryMainMenuEntryIsRegistered` — reflection: all non-abstract types implementing `IMainMenuEntry` appear in the resolved `IEnumerable<IMainMenuEntry>`.
- [ ] Build, test, commit.

### T4.3 Full PTY checklist
- [ ] Execute `REFACTORING.md` section 7 items 1–11 and record pass/fail per item in the PR description.

---

## Phase 5 — Application layer

### T5.1 ConnectionWorkflow
- [ ] `src/EtcdTerminal/Session/IConnectionWorkflow.cs`: `Task ConnectAsync(EtcdConnectionConfig config, CancellationToken ct); Task DisconnectAsync();`
- [ ] `ConnectionWorkflow(IEtcdConnection _connection, IUserCapabilitiesProvider _capabilities, IConnectionSession _session)`: `ConnectAsync` = connect → capabilities → `_session.Start`; on any exception after connect: `_session.End()`, try disconnect (ignore errors), rethrow. `DisconnectAsync` = `_session.End()` then `_connection.DisconnectAsync()`.
- [ ] `InstanceSelectionScreen`: replace the nested try/catch with `await _spinner.RunAsync(_localization.Connecting, ct => _workflow.ConnectAsync(selected, ct))`; remove `IEtcdConnection`, `IUserCapabilitiesProvider`, `IConnectionSession` dependencies. `MainScreen`: `DisconnectAsync` calls go through `_workflow`; keep the "ignore disconnect errors while another exception propagates" behavior with a `try/catch` around the single `_workflow.DisconnectAsync()` call in the error path only.
- [ ] Register Transient. Move the connect/rollback tests from `SessionFlowTests` to `ConnectionWorkflowTests`.
- [ ] Build, test, commit.

### T5.2 Settings
- [ ] Delete `IAppSettings.cs`; `AppSettings` stays a record.
- [ ] `IAppSettingsStore`: `AppSettings Current { get; } void Reload(); void Save(AppSettings settings);`. `AppSettingsStore(IAppSettingsRepository _repository)` implements `Reload` = `Current = _repository.Load()`, `Save` = `_repository.Save(settings); Current = settings;`.
- [ ] `SettingsScreen`: remove `IAppSettingsRepository`; single `_settings.Save(updated)` inside the existing `try`. `AppRunner`: `settingsStore.Reload()`.
- [ ] Update `SettingsScreenTests`. Build, test, commit.

### T5.3 Display concerns out of the domain
- [ ] Remove `DisplayKey`, `DisplayRangeEnd`, `DisplayText` from `EtcdPermission`. `PermissionDisplay.For` uses `DisplayText.Sanitize(permission.KeyPrefix)` / `DisplayText.Sanitize(permission.RangeEnd)`. `PermissionDisplayTests` expectations unchanged.
- [ ] Build, test, commit.

### T5.4 Failure kind in results
- [ ] `EtcdOperationResult`: add `EtcdOperationFailureKind? Kind`; `Fail(string message, EtcdOperationFailureKind kind)`. `DotnetEtcdBasedClient.RpcFail` passes the kind from `Failure(ex)`.
- [ ] Screens: `_message.ShowResult(result.Success, successText, failureHeadline + "\n" + result.ErrorMessage)` where `failureHeadline` is the existing localized `Failed*` string. Update tests that assert the failure text.
- [ ] Build, test, commit.

### T5.5 Unique-name rule
- [ ] Add `bool IsNameTaken(string name, string? exceptName)` to `IConnectionConfigRepository` (implemented in `JsonBasedConnectionConfigRepository`, forwarded by `ProtectedConfigRepository`). `ManageConnectionsScreen.SaveInstanceInteractive` checks it and shows a localized error (`ILocalization.InstanceNameTaken`, add to `EnglishLocalization`). The repository keeps throwing `InvalidOperationException` as a programmer-error guard.
- [ ] Build, test, commit.

---

## Phase 6 — Optional: split DotnetEtcdBasedClient

Do this phase only if the user confirms. Steps are in `REFACTORING.md` Phase 6.

---

## Phase 7 — Closure

- [ ] Re-read `AGENTS.md` "UI Rendering And Migration": remove sentences that describe migration state; keep rules. Rename the heading to "UI Rendering".
- [ ] Remove the reference to `REFACTORING.md` from `AGENTS.md`, then delete `REFACTORING.md` and `REFACTORING_TASKS.md`.
- [ ] Final `dotnet build src -warnaserror`, `dotnet test`, full PTY checklist, commit `[r] T7 refactoring closed`.
