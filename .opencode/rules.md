# C# Coding Rules

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
