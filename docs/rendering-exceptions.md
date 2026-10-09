s# Approved Rendering Exceptions

`AGENTS.md` targets zero application-owned ANSI generation, cursor-based drawing, display-width calculation and space-based background filling, and requires explicit approval for any exception. This document is that approval: one section per mechanism, the Spectre API that was checked, why it exists, and the condition under which it can be deleted.

**Spectre.Console version checked: 0.57.2** (pinned in `src/EtcdTerminal.Infrastructure/EtcdTerminal.Infrastructure.csproj`; there is no `Directory.Packages.props`). Every statement below refers to that version. When the pin moves, re-check each "checked" list and re-run `ArchitectureTests` before treating this register as still valid.

Nothing outside this list may generate ANSI, place the cursor by absolute row, count display cells or fill a row with spaces. `ArchitectureTests` pins the two allow-lists (see [Enforcement](#enforcement)).

---

## 1. `BackgroundBand` — space-based background fill

**File:** `src/EtcdTerminal.Infrastructure/Terminal/BackgroundBand.cs`

**What it does:** paints a band (status bar, key filter line, pagination line) edge to edge: it measures the target line with `Segment.CellCount`, computes the remaining width and appends a `new string(' ', fill)` segment carrying the band background, then chooses itself whether to emit a trailing line break.

**Spectre API checked (0.57.2):** `Padder` (`Spectre.Console.Padder`), `PaddableExtensions.Padding(...)` and `Segment.Padding(int)`. Per the class doc-comment, verified against this version: `Segment.Padding` builds an unstyled space, so the fill carries no background, and it emits a line break after the bottom padding, so the band cannot close without scrolling when it sits on the last terminal row.

**Justification:** Spectre offers no styled fill whose trailing break can be suppressed; without the fill the band's background stops mid-row and without the break control the footer would scroll the screen.

**Removal condition:** Spectre gains a padding/fill primitive that paints its fill with a `Style` (background included) and can emit no trailing line break — for example an option on `Padder` such as "expand without trailing break". Then `BackgroundBand` becomes a wrapper around it and the `Segment.CellCount`/`new string(' ')` code is deleted (the allow-list test will fail on its own when that happens, which is the signal to update this document).

---

## 2. `SpectreScreenCanvas` — `Pinned`, `LineWindow`, `Lines`

**File:** `src/EtcdTerminal.Infrastructure/Terminal/SpectreScreenCanvas.cs`

**What it does:** it is the single output owner of a session. `LineWindow` draws the body window at an offset between the pinned header and the pinned hint, each line followed by a line break and padded to erase leftovers; `Pinned` draws one renderable at an absolute row by saving the cursor, positioning it and restoring it; `Lines` composes those writes.

**Spectre API checked (0.57.2):** `LiveDisplay`/`Live<T>` exposes only `Overflow`, `Cropping` and `AutoClear` — it crops a renderable to the screen and cannot show a taller body at an offset, so a page pinned between a header and a hint/footer cannot be expressed with `Live` alone. Cursor placement is done through Spectre's own `AnsiWriter.SaveCursor(bool)`, `CursorPosition(int, int)` and `RestoreCursor(bool)` wrapped in `Segment.Control`, so the exception here is absolute-row geometry, not hand-written escape sequences (the escape strings themselves live only in `ConsoleTerminalSession`).

**Justification:** the footer must stay on the last three rows of the scroll region and a page's body must scroll inside the rows between the pinned header and hint; `Live` has no concept of a row offset.

**Removal condition:** Spectre can render a region anchored at an absolute row — for example a `Live`/`Layout` mode that takes a start row and height and redraws a body taller than that region at an offset. Delete `Pinned`/`LineWindow` once a widget exists that a real terminal/PTY session proves keeps the header, body and footer on their own rows across resize and redraw.

---

## 3. `BlockRenderer.Halved` — equal-share column widths

**File:** `src/EtcdTerminal.Infrastructure/Terminal/BlockRenderer.cs` (nested `Halved`)

**What it does:** splits the region into equal column shares (halves, thirds) at render time from the width the parent hands in, with the last column taking the rounding remainder, and crops cells with an ellipsis.

**Spectre API checked (0.57.2):** `Table` and `TableColumn.Width` (`int?`; `null` means "adapt to content"). `Table` gives the spare width to the column it measured wider, and `Layout.Ratio` — the only ratio sizing in this version — belongs to `Layout`, not to `Table`. The class doc-comment records the second gap: `BoxBorder.None` still reports `UsePadding`, so the table measurer budgets pad cells the renderer never draws and an explicit equal split would lose a column's share.

**Justification:** a list must stay balanced the way the pre-migration terminal cut every line into halves; content-based sizing makes one column swallow the room.

**Removal condition:** Spectre adds ratio or equal-share column sizing for `Table` that is honored by borderless tables at render time (a `TableColumn` width ratio that survives `BoxBorder.None` measurement). Then `Halved` reduces to `BuildTable` with ratios.

---

## 4. `SpectreSelectionPrompt` — row composition

**File:** `src/EtcdTerminal.Infrastructure/Terminal/SpectreSelectionPrompt.cs`

**What it does:** composes the prompt rows itself (pointer on the second column, text on the fourth, the window of rows around the current index) while the library still owns the live region, the cursor and the erasing of the menu when it closes.

**Spectre API checked (0.57.2):** the public surface of `SelectionPrompt<T>` is `PageSize`, `HighlightStyle`, `DisabledStyle`, `Mode`, `MoreChoicesText`, `Search*`, `WrapAround`, `Title`, `Converter` — there is no pointer glyph and no row-indent setting, and `IListPromptStrategy<T>.Render(IAnsiConsole, bool, int, IEnumerable<(int, ListPromptItem<T>)>, bool, string)` has no parameters for either. The pointer is the hardcoded `">"` and rows cannot be indented (documented in 0.57.2, as the class doc-comment records).

**Justification:** the migrated menu must keep the pre-migration margins and pointer, which the library prompt cannot draw.

**Removal condition:** `SelectionPrompt` becomes configurable — a pointer/indicator string and a row indent (or a `SelectionPrompt.Render` hook that receives them). Then the row composition moves back into the prompt and only the model stays.

---

## 5. `ConsoleTerminalSession` — raw escape sequences

**File:** `src/EtcdTerminal.Infrastructure/Terminal/ConsoleTerminalSession.cs`

**What it does:** the only class allowed to talk to `System.Console` and to raw sequences. It enters the alternate buffer, sets the scroll region to the rows above the footer, paints the window background, turns alternate scroll on and mirrors every theme change to the terminal background.

**Sequences Spectre cannot emit in 0.57.2** (the six listed in the class):

| Sequence | Purpose |
| --- | --- |
| `CSI 1;Nr` | set the scroll region to the viewport above the footer |
| `CSI r` | reset the scroll region |
| `OSC 11;#RRGGBB BEL` | set the terminal window background |
| `OSC 111 BEL` | reset the terminal window background |
| `DECSET 1007` on | alternate scroll: the mouse wheel is reported as arrow keys inside the alternate buffer, which is how a page scrolls |
| `DECSET 1007` off | restore normal wheel behavior |

**Spectre API checked (0.57.2):** `AnsiConsole.AlternateScreen(...)`, `AnsiWriter.EnterAltScreen()`, `HideCursor()`, `ShowCursor()`, `EraseInDisplay(int)`, `CursorHome()` exist; nothing in the public surface exposes a scroll region, an OSC 11/111 window background or DECSET 1007. All other writes in this file go through `IAnsiConsole.WriteAnsi(AnsiWriter)`.

**Justification:** the footer rows must survive body scrolling and the wheel must scroll the page — neither has a Spectre API.

**Removal condition:** Spectre exposes scroll regions, window background (OSC 11) and alternate scroll mode. Then the six sequences become API calls and this file keeps only the lifecycle and resize handling.

---

## Enforcement

`src/EtcdTerminal.Tests/ArchitectureTests.cs` pins the exceptions so a new one cannot appear silently:

- `InfrastructureEscapeSequencesLiveOnlyInTerminalSession` — no `\x1b`/`\u001b` literal outside `ConsoleTerminalSession.cs`.
- `SystemConsoleIsUsedOnlyByTerminalSession` — no `Console.` usage outside `ConsoleTerminalSession.cs`.
- `InfrastructureSpaceFillLivesOnlyInBackgroundBand` — `Segment.CellCount` or `new string(' '` in Infrastructure occurs in exactly `BackgroundBand.cs`.
- `AppAndPresentationSourcesContainNoGeometryApis` — no cursor/width/measurement APIs, and no `new string(' '`, in App and Presentation.

When an exception is removed, its test entry and the section above must be updated in the same change; a test failure here means the register is out of date, not that the test should be loosened.

## Proposals

None in this pass. No Spectre-native replacement was proven, so no rendering code was changed. Any future proposal must be verified in a real terminal/PTY — resize, redraw, footer pinning, wheel scrolling and interactive transitions — before code changes, per `AGENTS.md`.
