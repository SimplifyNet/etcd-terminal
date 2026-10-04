# Project Rules

## Architecture

The following rules describe the architecture.

**Responsibilities:**
- **Domain:** business entities, value objects, business rules and domain contracts only. UI components, terminal geometry, themes and localization are not domain concepts merely because they are interfaces or have no package dependencies.
- **Application:** use-case orchestration and application ports. Business and application code must not depend on a terminal renderer.
- **Presentation contracts:** dependency-free UI data models, semantic roles and renderer/input interfaces. Separate these from the domain and from concrete implementations. Models are data only; interfaces describe operations separately.
- **Presentation components:** `EtcdTerminal.App/Components`, `Engine`, `Screens` and feature-local controls. They assemble localized models, choose available actions and handle navigation/state transitions. They describe content, structure and semantic appearance, not physical rendering algorithms.
- **Infrastructure:** concrete model-to-Spectre mapping, console capabilities, rendering and interactive adapter lifecycle. Repository and other technical implementations also live here.
- **Composition root:** `EtcdTerminal.App/Setup/IocRegistrations.cs` connects interfaces to implementations. Screens and components must not resolve services or instantiate Infrastructure classes.

**Dependency rules:**
- Domain must not reference Presentation, Infrastructure, Spectre or terminal APIs.
- Presentation components depend on application/domain services and pure Presentation contracts, never on concrete renderers.
- Infrastructure implements contracts and may depend on their assemblies, never on App components or screens.
- All production Spectre types and `System.Console` access belong in Infrastructure. Test projects may reference `Spectre.Console.Testing` for thin adapter tests.
- `ITheme`/`RgbColor` are presentation theme contracts; `ILocalization` is a presentation text contract. Keep contracts independent of Infrastructure. Components select semantic roles; only Infrastructure translates theme values into Spectre styles.
- Feature-local controls have the same restrictions as shared components. Their location under `Screens/` is not an exception permitting cursor, ANSI, width calculation or raw color escapes.

**Shared services (Singleton, registered in `Setup/IocRegistrations.cs`):** `ITheme` to `ReddyTheme`, `ILocalization` to `EnglishLocalization`, `IAppSettingsStore` to `AppSettingsStore`, `IConnectionSession` to `ConnectionSession`. Inject dependencies; no static service locator. A future screen host must have one owner per console and an explicit session lifecycle.

## UI Rendering

- Maximize reuse of Spectre `Panel`, `Table`, `Grid`, `Layout`, `SelectionPrompt`, `TextPrompt`, `Status` and `Live` where their behavior fits. Do not recreate library widgets behind a new interface.
- UI models contain literal text, semantic spans, selected/disabled state and logical regions. No Spectre types, markup strings, ANSI, RGB escape strings, cursor positions, terminal dimensions, measured widths or strings padded to screen size. Renderer interfaces may expose operations and results; models must not carry renderer callbacks.
- For example, a menu component accepts items with stable IDs, decides which actions are available and handles the result. It does not calculate line widths, place the cursor, draw borders or fill backgrounds.
- Map models and semantic roles to Spectre renderables in one cohesive Infrastructure rendering module. Share role/style conversion between renderers; do not duplicate it per screen. This does not require one giant class or a general-purpose UI framework.
- Let Spectre measure, wrap, align, pad, crop and draw. Infrastructure may configure widget padding, region sizes, overflow and capability policy, but must not implement another geometry engine.
- Target zero application-owned ANSI generation, cursor-based drawing, display-width calculation and space-based background filling. Before adding a small adapter for a proven library gap, inspect the pinned library and document the gap. Any exception to this target or visual parity needs explicit approval; do not silently weaken either requirement.
- Literal user data must remain literal, including brackets, Unicode and connection strings. Preserve control-character sanitization and secret handling; these are application safety requirements, not redundant geometry code.
- A screen has exactly one composition/output owner. Header, body, action panels and footer are composed once; child components must not independently append or reposition a shared footer. Repeated updates replace the current state rather than duplicating panels.
- The footer must remain at the bottom of the visible interactive viewport, including input states unless an explicit exception is approved. An ordinary `Write(Panel)` is streaming output, not a pinned footer. `Layout`/`Live` are implementation candidates, not proof of parity.
- Check Spectre interactive lifecycle constraints before combining widgets. Do not nest `TextPrompt` or `Status` inside an active `Live` on the same console. Do not assume `SelectionPrompt` can be embedded in a `Layout` region.
- Preserve behavior and the established visual language: semantic colors, content/background distinction, spacing, selection, action availability, keyboard/cancellation behavior and session information. Exact byte-for-byte output is unnecessary; missing colors, inline/duplicated footers, inaccessible actions or lost input behavior are unacceptable.

## UI Verification

- Reduce UI-specific test and maintenance burden by deleting manual rendering code and its geometry/cursor/ANSI tests together. Do not delete tests merely to make a broken migration pass.
- Keep cheap tests for model content/roles, action availability, navigation, cancellation and application/domain behavior. Keep a small number of mapper tests using `TestConsole`, including literal text and style mapping.
- Do not retest Spectre's width, border, padding, cursor or ANSI algorithms with a home-grown terminal emulator. Remove geometry-only fakes when their last meaningful consumer is gone.
- `TestConsole` output is not a terminal screen emulator. Verify pinned footer, redraw, overflow, resize and interactive transitions in a real terminal or PTY; a text snapshot alone does not prove parity.
- Keep architecture checks for dependency boundaries. A migration is accepted only with build/test results and explicit visual/behavioral verification; record unverified scenarios instead of claiming success.

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
