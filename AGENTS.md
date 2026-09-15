# Project Rules

## Architecture

Layers: **Terminal → Components → Screens**.

- `Terminal/` (domain `EtcdTerminal.Terminal`) — low-level abstraction (`ITerminal`, `TerminalColor`, `TableData`) and its `ConsoleTerminal` implementation. The only layer that knows about `System.Console` and ANSI escape sequences.
- `Theming/` — color system (`ITheme`, `ThemeStore`, `RgbColor`). Provides colors to Terminal layer.
- `Components/` — reusable UI components (`Header`, `MenuScreen`, `PressAnyKeyPrompt`, `ScreenLayout`, `Spinner`, `StatusBar`). Depend on `ITerminal` only. Colors come from `ITheme` via `ThemeStore.Current`.
- `Engine/` — interactive input-loop primitives (`Menu`, `Prompt`). Used by screens and components to read key input and render selection lists; like Components, they depend on `ITerminal` only.
- `Screens/` — orchestration: only use components/engine + feature-local controls. Never perform raw console work.
- `Localization/` — text system (`ILocalization`, `LocalizationStore`). Provides UI strings.

**Dependency rules:**
- `Screens` → `Components`/`Engine` (+ feature-local controls). No `Console.*`, `AnsiConsole.*`, ANSI (known exception: `Engine/Prompt.cs` still owns the Spectre prompt integration, pending extraction).
- `Components`/`Engine` → `Terminal`. Colors only from `ITheme` (`ThemeStore.Current`).
- `Terminal` — nothing from App. `ConsoleTerminal` is the sole `System.Console`/Spectre touchpoint (same `Prompt` exception as above).
- `Theming` — domain-only, no infrastructure dependencies.
- `Localization` — domain-only, implementations live in App layer.

**Infrastructure layer** (`EtcdTerminal.Infrastructure`): implementations of technical interfaces (`ITerminal`, `IAppSettingsRepository`, `IConnectionConfigRepository`). All Spectre.Console dependencies live here (same `Prompt` exception as above).

**Feature-local controls** (e.g. `KeyBrowseControl`, `UserListRenderer`, `RoleListRenderer`, `PermissionViewRenderer`) live in `Screens/` but may use `ITerminal` and `ITheme` directly for rendering — they are part of the Components layer conceptually but scoped to a single feature.

**Ambient contexts:** `ThemeStore.Current`, `LocalizationStore.Current`, `AppSettingsStore.Current` — static access to domain services, initialized in `Program.cs`.

**Planned, not yet implemented** (no such types in `src/` yet — do not treat them as existing): `Panel` (core bordered-panel design element), `Message` (success/error/warning helper with press-any-key).

**Primary-constructor convention:** dependencies are declared as primary-constructor parameters with a leading underscore and used directly, e.g. `Menu(ITerminal _terminal, ...)`, then `_terminal.Write(...)` inside methods. Do not remove the underscore and do not redeclare separate backing fields for them.

## File structure

- One public type per file (class, struct, interface, enum). File name must match the type name.
- In `.csproj`, `ProjectReference` item groups come before `PackageReference` item groups.

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

## Assignment and method call separation

Assignment lines (`x = ...`, `x.Property = ...`) must be separated from method call lines (`x.Method()`) by one blank line.

```
var config = LoadConfig();

SaveConfig(config);
```

## Modern C# syntax

- **File-scoped namespaces** (`namespace X.Y;` — no braces).
- **Primary constructors** for classes that take dependencies.
- **Collection expressions** (`[]` instead of `Array.Empty<T>()`, `new List<T>()`, or `new T[] { }`).
- **Target-typed `new()`** where the type is obvious (`new() { ... }`).
- **Expression-bodied members** for single-expression methods, properties, and operators.
- **Pattern matching**: use `is` / `is not` instead of `==` / `!=` for bool? comparisons.
