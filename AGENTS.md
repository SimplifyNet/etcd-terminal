# Project Rules

## Architecture

Layers: **Terminal → Components → Screens**.

- `Terminal/` — low-level abstraction (`ITerminal`) and its `ConsoleTerminal` implementation. The only layer that knows about `System.Console` and ANSI escape sequences. Types: `TerminalColor`, `TerminalStyle`, `TextRun`, `Palette`.
- `Components/` — reusable UI components (`Panel`, `Header`, `StatusBar`, `ScreenLayout`, `Menu`, `Prompt`, `PressAnyKey`, `Message`). Depend on `ITerminal` only. Colors come from `Palette`. `Panel` is the core design element.
- `Screens/` — orchestration: only use components + feature-local controls. Never perform raw console work.

**Dependency rules:**
- `Screens` → `Components` (+ feature-local controls). No `Console.*`, `AnsiConsole.*`, ANSI, `Palette`.
- `Components` → `Terminal`. Colors only from `Palette`.
- `Terminal` — nothing from App. `ConsoleTerminal` is the sole `System.Console` touchpoint.

**Exceptions (documented):** `Prompt` uses Spectre `TextPrompt`/`Confirm` via `EscapableConsole`; `Header` uses Spectre `FigletText` as a line generator (output via `ITerminal`); `Program.cs` uses `AnsiConsole.WriteException` in the crash handler.

**Feature-local controls** (e.g. `KeyBrowseControl`, `UserListRenderer`, `RoleListRenderer`, `PermissionViewRenderer`) live in `Screens/` but may use `ITerminal` and `Palette` directly for rendering — they are part of the Components layer conceptually but scoped to a single feature.

## File structure

- One public type per file (class, struct, interface, enum). File name must match the type name.

## Class member ordering

Members must appear in this order:

1. **Fields** (const, static, readonly, instance)
2. **Constructor** (or primary constructor declaration)
3. **Properties** (auto-props, expression-bodied, computed)
4. **Methods**

Within each group: **public → protected → private** (most open to most closed).

## Control flow

- Single-statement `if`, `else`, `for`, `foreach`, `while` bodies must NOT use braces.
- Multi-statement bodies must use braces.
- `switch` expressions preferred over `switch` statements when mapping values.

## Variable declarations

Lines with variable declarations (`var x = ...` or `Type x = ...`) must be separated from non-declaration code by a blank line after the last declaration.

Consecutive variable declarations (without intervening code) are grouped and count as one block — no blank lines between them.

```
var a = Foo();
var b = Bar();

if (a > b) ...
```

## Modern C# syntax

- **File-scoped namespaces** (`namespace X.Y;` — no braces).
- **Primary constructors** for classes that take dependencies.
- **Collection expressions** (`[]` instead of `Array.Empty<T>()`, `new List<T>()`, or `new T[] { }`).
- **Target-typed `new()`** where the type is obvious (`new() { ... }`).
- **Expression-bodied members** for single-expression methods, properties, and operators.
- **Pattern matching**: use `is` / `is not` instead of `==` / `!=` for bool? comparisons.
