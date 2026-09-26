# Architecture Review and Implementation Plan

Review date: 2026-09-26. Baseline commit: `f763445`.

## Scope and Baseline

Reviewed the domain, Infrastructure, App composition root, Engine, Components,
screens, and tests against `AGENTS.md`, SOLID, and observable behavior.
Application code was not changed as part of this review.

Verification: `dotnet test src/EtcdTerminal.slnx --no-restore` built the solution
and passed all 41 tests, with no failures or skips. This is not evidence that
etcd integration or physical terminal behavior is correct. No live etcd or
interactive terminal tests were performed.

Paths below are relative to the repository root. Line references describe the
reviewed commit; locate the named methods if lines have moved.

## Findings by Priority

P1 means possible unintended mutation/data loss or a broken supported workflow.
P2 means a correctness, lifecycle, or meaningful boundary/testability issue.
P3 means preventative architecture work after behavior is covered.

| Priority | Finding and evidence | Work item |
| --- | --- | --- |
| P1 | Create/update use GET followed by unconditional PUT; denied GET is treated as absence. `src/EtcdTerminal.Infrastructure/DotnetEtcdBasedClient.cs:70-122` | T03 |
| P1 | Value updates use a PUT without preserving the existing lease. `src/EtcdTerminal.Infrastructure/DotnetEtcdBasedClient.cs:113-122` | T03 |
| P1 | Settings save replaces malformed configuration; connection mutations serialize a filtered subset of existing entries. `src/EtcdTerminal.Infrastructure/Configuration/JsonBasedSettingsRepository.cs:31-41`, `JsonBasedConnectionConfigRepository.cs:10-75` | T02 |
| P1 | JSON trailing-comma regex modifies quoted strings and property names. `src/EtcdTerminal/Keys/JsonKeyFlattener.cs:38-49,91-92` | T01 |
| P1 | Exact-key and explicit-range permissions are fetched as prefixes. `src/EtcdTerminal/Keys/ReadableKeysProvider.cs:15-22` | T05 |
| P2 | Transport/auth failures become missing users/roles; missing username can mean unrestricted access. `src/EtcdTerminal.Infrastructure/DotnetEtcdBasedClient.cs:151-167,235-251`, `src/EtcdTerminal/Security/UserCapabilitiesProvider.cs:13-24` | T04 |
| P2 | Post-connect failure/cancellation and exceptions inside the main menu leave client/session state active. `src/EtcdTerminal.App/Screens/InstanceSelectionScreen.cs:49-72`, `MainScreen.cs:18-41` | T06 |
| P2 | Range bounds use UTF-16 ordering and mishandle the open-ended zero-byte sentinel. `src/EtcdTerminal/Permissions/PermissionRange.cs:17-39`, `EtcdPermission.cs:25-34` | T05 |
| P2 | Revoke accepts a permission type that is ignored; range displays omit the endpoint. `src/EtcdTerminal.Infrastructure/DotnetEtcdBasedClient.cs:345-360`, `src/EtcdTerminal.App/Screens/Permissions/PermissionViewRenderer.cs:29-34` | T07 |
| P2 | Connection rename can produce duplicate names; deletion then removes both. `src/EtcdTerminal.Infrastructure/Configuration/JsonBasedConnectionConfigRepository.cs:56-75` | T02 |
| P2 | Import loses confirmed partial progress when an operation throws or is cancelled. `src/EtcdTerminal/Keys/KeyImporter.cs:11-35`, `src/EtcdTerminal.App/Screens/Keys/KeyImportJsonScreen.cs:84-109` | T08 |
| P2 | Prompt cancellation, empty input, and whitespace policies differ; paste initialization discards queued input. `src/EtcdTerminal.App/Engine/Prompt.cs:18-51`, `Components/MultiLinePasteReader.cs:18-27`, `Screens/Keys/KeyImportJsonScreen.cs:38-39` | T09 |
| P2 | Browse reload removes filtering while leaving the search query displayed. `src/EtcdTerminal.App/Screens/Keys/KeyBrowseScreen.cs:100-110` | T10 |
| P2 | Menus/browser pages exceed terminal height; rows/footer exceed terminal width. `src/EtcdTerminal.App/Engine/Menu.cs:40-48`, `Screens/Keys/KeyBrowseLayout.cs:16-21,54-70`, `Components/StatusBar.cs:48-71` | T11-T12 |
| P2 | Engine reads app settings; generic footer interprets session data; orchestration owns rendering policy. `src/EtcdTerminal.App/Engine/Prompt.cs:7`, `Components/StatusBar.cs:8,39-55`, `Screens/Keys/KeyImportJsonScreen.cs:112-142` | T09, T13 |
| P2/P3 | Adapter has no replaceable transport seam; architecture tests miss method-body dependencies; terminal fake cannot establish layout correctness. `src/EtcdTerminal.Infrastructure/DotnetEtcdBasedClient.cs:19-40`, `src/EtcdTerminal.Tests/ArchitectureTests.cs:177-201`, `Fakes/FakeTerminal.cs:12-18,48-69,101,125` | T03, T11, T14 |

## SOLID Assessment

| Principle | Assessment |
| --- | --- |
| Single Responsibility | Mostly reasonable feature boundaries. Concrete leaks exist where `Prompt` owns settings policy, `StatusBar` interprets connection state, and orchestration formats previews. Fix those bounded cases, not every long class. |
| Open/Closed | `IMainMenuEntry` and the composition-root entry collection already support extending the menu without editing its execution loop. No plugin framework or strategy class per operation is justified. |
| Liskov Substitution | The concern is behavioral contracts, not inheritance. A create operation must not overwrite, an update must not create, and a failed read must not masquerade as absence. Document and test those contracts across the adapter and fakes. |
| Interface Segregation | Focused key/user/role/auth/connection and terminal interfaces are a strength. The aggregate `IEtcdClient` is acceptable because consumers receive focused ports. A missing range operation is a capability mismatch, not a reason to split every interface again. |
| Dependency Inversion | Domain has no Infrastructure dependency; vendor integrations are isolated. Composition-root references to concrete implementations are appropriate. Improve the adapter's internal test seam and remove app-domain policy from generic UI primitives. |

Keep the current project structure. A singleton client is appropriate for one
active connection; exceptional cleanup, not its lifetime designation, is the
problem. The password-protection decorator is a useful boundary. Its limited
threat model and full-keyspace/N+1 performance tradeoffs are already documented
in `README.md:77-85` and are not new SOLID violations.

## Instructions for the Implementing Model

1. Read `AGENTS.md` and the complete files relevant to the selected work item.
2. Implement one work item at a time. Do not execute the entire plan as one large refactor.
3. Add a regression test for the reported behavior before changing it where feasible. Keep the regression after the fix.
4. Use the smallest correct change. No generic repository framework, mediator, event bus, service locator, or interface per class.
5. Keep vendor types in Infrastructure. Use primary-constructor dependencies with leading underscores and follow the repository's formatting/member-order rules.
6. Search all callers, DI registrations, and test doubles when changing a contract. Do not add compatibility overloads for internal callers; update them together. Preserve the existing persisted configuration format.
7. Verify exact client API signatures and server behavior against the installed `dotnet-etcd` version before implementing protocol-dependent work. Do not guess overloads or status codes.
8. Do not use a production cluster or the user's real configuration for tests. Use temporary files and an explicitly isolated etcd instance.
9. After each item run its focused tests and `dotnet test src/EtcdTerminal.slnx`. Report any integration tests not executed; do not claim mocks prove server semantics.
10. Do not commit, upgrade dependencies, or perform unrelated cleanup unless requested. A narrowly justified test-only dependency for T14 is an exception to the dependency restriction, not permission for general upgrades.

Suggested sequence: T01, T02, T03, T04, T05, T06, T07, T08, T09, T10,
T11, T12, T13, T14. T01, T02, and T10 are independent small starting tasks.
T05's byte-boundary changes deserve explicit human review before merging.

## Work Items

### T01 - Stop Rewriting JSON String Data [P1]

**Files:** `src/EtcdTerminal/Keys/JsonKeyFlattener.cs` and
`src/EtcdTerminal.Tests/JsonKeyFlattenerTests.cs`.

**Problem:** `{"value":"keep,]"}` becomes `{"value":"keep]"}` before parsing.
The regex cannot distinguish punctuation inside strings from JSON syntax.

**Implementation:**
1. Remove trailing-comma regex preprocessing and its generated method/import. Remove `partial` if no longer needed.
2. Enable trailing commas through `JsonDocumentOptions` on the existing `JsonNode.Parse` call.
3. Preserve the existing bare-property wrapping, array-prefix rule, and flattening behavior.

**Acceptance:** Valid strings/property names containing `,]`, `,}`, whitespace
after commas, and escaped quotes round-trip unchanged. Structural trailing
commas and existing flattening tests still pass.

### T02 - Make Configuration Mutations Non-Destructive [P1/P2]

**Files:** `src/EtcdTerminal.Infrastructure/Configuration/JsonConfigFile.cs`,
`JsonBasedSettingsRepository.cs`, `JsonBasedConnectionConfigRepository.cs`,
and new repository tests under `src/EtcdTerminal.Tests/`.

**Implementation:**
1. Use `ReadRootOrThrow` for settings saves. A missing file may initialize a new object; a malformed or non-object existing file must not be replaced.
2. Separate tolerant loading for display from strict loading for connection mutations. Never mutate the output of the current filtering loader.
3. Prefer the bounded policy: reject a mutation if the existing Instances section has invalid shape, invalid entries, or ambiguous duplicate identities. Leave the original file unchanged and explain the issue. Do not silently discard invalid entries to repair the file.
4. Parse the root once per mutation and save that same root, preserving unrelated top-level sections. Keep the existing atomic-write/private-file implementation.
5. Reject `UpdateInstance` when its new name belongs to another connection. Allow renaming to the same name. Preserve existing intentional add/replace behavior unless separately approved.
6. Ensure errors reach the UI's existing error handling without writing credentials into diagnostics. Keep encryption/decryption in the existing decorator.

**Acceptance:** Temporary-file tests cover malformed JSON, a non-object root,
an invalid connection among valid ones, duplicate identities, rename collision,
rename-to-self, missing-file initialization, and preservation of settings during
connection writes and connections during settings writes. Rejected operations
must leave the original bytes unchanged. Encryption regression tests still pass.

### T03 - Enforce Atomic Key Mutation Contracts [P1]

**Files:** `src/EtcdTerminal.Infrastructure/DotnetEtcdBasedClient.cs`,
`src/EtcdTerminal/Keys/IEtcdKeyStore.cs`,
`src/EtcdTerminal.App/Setup/IocRegistrations.cs`, and adapter tests.

**Implementation:**
1. Add one Infrastructure-local transport-construction seam. Prefer the vendor's existing client interface and a small factory/delegate after checking the actual API. Keep a single shared connected transport and disposal ownership. Do not introduce a second domain-wide client hierarchy.
2. Implement create-if-absent with a server-side transaction comparing key version to zero and writing only on success. Return false for an existing key, not for permission/transport failures.
3. Implement update-if-present atomically and preserve the key's lease. A transaction comparing version greater than zero with a `PutRequest` that ignores the lease is a suitable approach after checking API support. Do not copy a lease from an earlier GET or fall back to an unconditional PUT.
4. Document the meaning of mutation booleans and failures on `IEtcdKeyStore`. Conditional operations may need read permission; report denial rather than weakening the operation for write-only users.
5. Keep value updates distinct from lease removal. No new lease-management UI is required.

**Acceptance:** Request-mapping tests assert conditional comparisons and lease
preservation. Integration tests against isolated etcd cover two competing
creators, a missing/deleted key during update, an existing leased key, and a
write-only account. Exactly one creator succeeds; neither operation violates
its presence condition; updating a leased key retains its lease and revoking
that lease still removes the key. No GET-then-PUT mutation path remains.

### T04 - Keep Absence, Failure, and Authorization Distinct [P2]

**Dependencies:** T03 transport test seam.

**Files:** `src/EtcdTerminal.Infrastructure/DotnetEtcdBasedClient.cs`,
`src/EtcdTerminal/Security/UserCapabilitiesProvider.cs`, relevant port contracts,
and `src/EtcdTerminal.Tests/UserCapabilitiesProviderTests.cs`.

**Implementation:**
1. Stop converting denied key reads into null/empty collections.
2. Narrow user/role lookup catches to verified missing-entity responses only. Check actual etcd error mapping rather than assuming every absence uses gRPC `NotFound`.
3. Propagate operational failures. If a typed error is needed by T08, use one small domain-owned operation exception/classification with Infrastructure translation; do not expose `RpcException` to domain logic.
4. Preserve cancellation as cancellation, including client RPC cancellation when the supplied token is cancelled. Do not turn it into missing data or an ordinary failed CRUD result.
5. Return unrestricted capabilities only when auth is disabled or root membership is established. Auth enabled plus null username must produce no capabilities, not unrestricted access.

**Acceptance:** Missing user/role, unavailable server, deadline, denied access,
invalid authentication, and cancellation are separate tests. Discovery failure
cannot return successful empty capabilities. Null username with auth enabled is
not root. Empty successful key reads remain distinguishable from failed reads.
This fixes misleading UI capability claims, not an etcd server authorization bypass.

### T05 - Make Permission Reads Match Permission Bounds [P1/P2]

**Dependencies:** T03 and T04.

**Files:** `src/EtcdTerminal/Permissions/PermissionRange.cs`, `EtcdPermission.cs`,
`src/EtcdTerminal/Keys/IEtcdKeyStore.cs`, `ReadableKeysProvider.cs`,
`src/EtcdTerminal.Infrastructure/DotnetEtcdBasedClient.cs`, and related tests.

**Implementation:**
1. Add one explicit bounded-range read to the key-store port. Exact permissions use exact lookup, prefixes use prefix lookup, and ranges use their actual start and exclusive end. Keep overlap deduplication and ignore write-only permissions.
2. Correctly represent etcd's zero-byte open-ended end bound: `[a, zero-byte)` covers keys greater than or equal to `a`; zero-byte start/end represents all keys.
3. Preserve raw permission-boundary bytes from RPC mapping. Use a small vendor-independent immutable range representation if necessary. Text keys may remain strings; this task does not require a binary key/value editor.
4. Compute prefix successors and compare bounds in UTF-8 byte order, not by incrementing a C# `char` or using UTF-16 ordinal range comparisons. Do not decode arbitrary range endpoints to strings and then re-encode them for RPC requests.
5. Use a safe display representation for non-text endpoints. Keep display text separate from authoritative request bounds.
6. Reject unsupported range grants/revocations explicitly if the caller supplies no endpoint. Never silently map `PermissionScope.Range` to a single-key operation.
7. Verify the empty-prefix/all-keys request explicitly. Do not retain a fallback slash query merely to hide an incorrect all-keys request or a denied read.

**Acceptance:** Tests cover exact `/a` without `/ab`, bounded `[a,c)` including
`b`, exclusive endpoints, open-ended `[a, zero-byte)`, all keys, overlapping
permissions, and write-only permissions. Byte tests include a prefix ending in
U+D7FF, supplementary characters, and non-text endpoint bytes. A
permission-enforcing fake or isolated etcd must reject over-broad requests so
the tests cannot pass by returning prefiltered data regardless of the query.

### T06 - Establish Session Cleanup Boundaries [P2]

**Files:** `src/EtcdTerminal.App/Screens/InstanceSelectionScreen.cs`,
`MainScreen.cs`, and session-flow tests. Inspect `Program.cs:41-71` and
`src/EtcdTerminal/Session/ConnectionSession.cs` as callers/contracts.

**Implementation:**
1. Treat connect plus capability discovery as initialization: do not start a session until both succeed.
2. If initialization fails or is cancelled after connecting, disconnect and clear the session before returning to selection.
3. Wrap the connected main-menu workflow in `try/finally`; run cleanup on normal exit, cancellation, and exceptions. Remove duplicated normal-exit cleanup branches.
4. Ensure session state is cleared even if disconnect fails. Do not mask the original operation error with a cleanup error.
5. Keep terminal shutdown at the composition root and avoid introducing a generic session state-machine framework.

**Acceptance:** Connect-success/discovery-failure, discovery cancellation,
throwing menu entry, normal disconnect, and failing disconnect all leave no
active session. Reopening selection never shows the previous connection in the
footer. Successful initialization starts exactly one session.

### T07 - Make Permission Administration and Display Honest [P2]

**Dependencies:** T05 representation changes.

**Files:** `src/EtcdTerminal/Roles/IEtcdRoleAdmin.cs`,
`src/EtcdTerminal.Infrastructure/DotnetEtcdBasedClient.cs`,
`src/EtcdTerminal.App/Screens/Roles/RoleManagementScreen.cs`,
`RoleListRenderer.cs`, `PermissionScopeText.cs`, `PermissionTypeSelector.cs`,
`src/EtcdTerminal.App/Screens/Permissions/PermissionViewRenderer.cs`, and localization.

**Implementation:**
1. Remove `permissionType` from revoke and all callers/fakes. Only grants ask for a type. Explain that revocation removes the whole permission for the target interval.
2. Do not implement partial bit revocation implicitly by revoke-and-regrant.
3. Share the small permission-display mapping used by the two feature renderers. Show exact keys, prefixes, bounded ranges with exclusive endpoints, open-ended ranges, and all keys distinctly.
4. Reuse localized permission-type labels and localize the current hard-coded `Role:`/`User:` title text. Do not print a raw zero-byte sentinel.

**Acceptance:** Revoke request identifies an interval without promising a type
distinction. Two ranges with the same start and different ends render
differently. Capture actual `TableData` in tests. Sentinel localization proves
that labels/types/titles do not fall back to hard-coded English or enum names.

### T08 - Report Confirmed Partial Import Progress [P2]

**Dependencies:** T03/T04 mutation and error contracts.

**Files:** `src/EtcdTerminal/Keys/IKeyImporter.cs`, `KeyImporter.cs`,
`KeyImportResult.cs`, `src/EtcdTerminal.App/Screens/Keys/KeyImportJsonScreen.cs`,
localization, and `src/EtcdTerminal.Tests/KeyImporterTests.cs`.

**Implementation:**
1. Preserve current sequential import and stop-on-operational-failure behavior. Do not silently introduce retries, rollback, or parallel writes.
2. Expose a snapshot after each confirmed outcome, for example through a synchronous progress callback carrying `KeyImportResult`. Keep UI dependencies out of the importer and do not rely on a callback asynchronously arriving after cancellation handling.
3. On operational failure or cancellation, retain and display confirmed created/overwritten/failed counts and state that previous writes were not rolled back.
4. Distinguish entries not attempted from entries known to have failed. A timed-out or cancelled RPC may have committed on the server: label its outcome unconfirmed rather than claiming no write occurred.
5. Catch only expected operation failures in the import screen; let programming errors retain normal error handling. Keep cancellation distinct from failure.

**Acceptance:** After one successful write, a denied second write, a transport
failure, or cancellation does not erase confirmed progress. No later entry is
attempted after stop. Normal success still shows the existing summary. An
uncertain in-flight result is not counted as a confirmed success or rollback.

### T09 - Separate Input Policy from Generic Prompt Mechanics [P2]

**Files:** `src/EtcdTerminal.App/Engine/Prompt.cs`,
`Components/MultiLinePasteReader.cs`, `Screens/Keys/KeyCreateScreen.cs`,
`Screens/Keys/KeyImportJsonScreen.cs`, all Prompt callers, DI, and input tests.

**Implementation:**
1. Remove `IAppSettingsStore` from `Prompt`. Pass explicit trim/empty policies from callers that own application settings. Start with parameters, not a policy-service hierarchy.
2. Apply trimming only when requested. With trimming disabled, accepted whitespace-only values must remain byte-for-byte unchanged. Keep required-field validation separate from cancellation handling.
3. Allow empty values when creating keys, consistently with editing. Keep key names and required connection/administration fields validated. Preserve intentional secret trimming according to the configured policy.
4. Check each import prompt result for null and return immediately on cancellation. Prefix uses `allowEmpty: true`; an empty prefix is not cancellation. Do not coalesce a cancelled separator to its default.
5. Remove the unconditional key drain at paste-reader startup. Do not discard already queued paste or Escape input.

**Acceptance:** Tests cover empty create/edit values, preserved whitespace with
trimming off, trimming on, cancellation at both import prompts, empty prefix,
prequeued paste, and queued Escape. Cancelling before paste must not invoke the
paste reader/importer. Prompt tests require no settings store. Audit every
caller so removing implicit settings lookup does not silently change policy.

### T10 - Preserve Search Across Browser Mutations [P2]

**Files:** `src/EtcdTerminal.App/Screens/Keys/KeyBrowseScreen.cs` and browser tests.

**Implementation:** In `ReloadAsync`, filter the newly loaded source with
`_control.SearchQuery` before clamping page/selection. Do not call the existing
`ApplyFilter` blindly if its navigation reset would change the intended behavior.
No new pager abstraction is required.

**Acceptance:** Search then edit/delete keeps the query and displayed results
consistent. Deleting the final match and editing a value so it no longer matches
produce correct counts and valid selection/page state.

### T11 - Add Layout Test Support and Bound Row Widths [P2]

**Files:** `src/EtcdTerminal.Tests/Fakes/FakeTerminal.cs` or a separate layout
fake, `src/EtcdTerminal.App/Screens/Keys/KeyBrowseLayout.cs`,
`Components/StatusBar.cs`, import preview rendering, and terminal abstractions
only if display-cell measurement needs a narrowly scoped operation.

**Implementation:**
1. Add a configurable layout-recording terminal fixture with dimensions, cursor advancement, write positions, and captured tables. Styling markers have zero visible width. Test the fixture itself; keep the existing simple output fake if changing it would obscure unrelated tests.
2. Use one plain-text, single-line preview formatter where browser/import formatting currently diverges. Normalize CR/LF/tab/control content for display only, include ellipsis inside the width budget, and handle zero/narrow widths safely.
3. Account for display width rather than assuming UTF-16 length equals terminal columns. Reuse terminal-layer facilities where possible; keep Spectre/ANSI knowledge out of screens and components.
4. Budget the complete footer row dynamically. Shorten/omit lower-priority connection fields and hints when needed instead of writing beyond the window. Bound long names/usernames as well as URLs.
5. Define safe behavior when the terminal is shorter than the footer reservation. Never issue negative cursor coordinates. Avoid changing stored values or edit defaults to their shortened previews.

**Acceptance:** Long keys and values together, CR/LF/tab, wide Unicode text,
long connection details, and very narrow/short windows do not overflow row
budgets or issue invalid positions. Footer cursor preservation and actual table
contents are asserted. Run a manual smoke test on a real terminal because a
recording fixture is not a complete terminal emulator.

### T12 - Keep Interactive Selections Inside the Viewport [P2]

**Dependency:** T11 layout fixture and bounded rows.

**Files:** `src/EtcdTerminal.App/Engine/Menu.cs`,
`Screens/Keys/KeyBrowseScreen.cs`, `KeyBrowseControl.cs`, `KeyBrowseLayout.cs`,
and menu/browser tests.

**Implementation:**
1. Add a menu viewport/scroll offset based on available rows above the footer. Preserve selectable-item identity and existing nonselectable separators.
2. Calculate the browser's effective page size from physical content rows, accounting for header/search/pagination/actions/footer. The configured page size is an upper bound, not a promise that all rows fit.
3. Use the same effective size for page count, rendering, command handling, and reload clamping. Recompute when dimensions or action-panel state change.
4. Keep the selected item visible after movement/resizing. For windows too small to show controls, render a bounded size warning with a working exit path instead of forcing a positive page size into nonexistent space.

**Acceptance:** At 80x24 and smaller supported sizes, long menus and large page
settings remain navigable without drawing under the footer. Test last-item
selection, separators, action-panel opening, resize, and empty results.

### T13 - Finish Bounded UI Responsibility Cleanup [P2/P3]

**Dependencies:** T09, T11, T12 behavior tests.

**Files:** `src/EtcdTerminal.App/Components/StatusBar.cs`,
`Screens/Keys/KeyImportJsonScreen.cs`, `KeyBrowseScreen.cs`, feature renderers,
and `Setup/IocRegistrations.cs`.

**Implementation:**
1. Extract import preview and key edit/delete detail rendering into small feature-local renderers, following existing renderer conventions. Screens retain orchestration, prompts, result handling, and navigation.
2. Remove session interpretation from the reusable footer. Supply presentation-only footer data through a small UI-owned contract; its application-side adapter reads `IConnectionSession` and builds current display fields on demand.
3. Do not move `EtcdConnectionConfig` into a generic UI contract, store passwords in display data, or capture a stale singleton snapshot. Keep the adapter outside generic Engine/Components.
4. Keep existing permitted localization/theme/environment dependencies unless a specific rule/test requires otherwise. Do not reinterpret 'depend on terminal only' as banning every harmless UI data type.
5. Verify primitive dependencies no longer directly reference application configuration/session policy. Update DI and tests together.

**Acceptance:** Renderers can be tested without a store/importer, footer
rendering without a connection session, and generic prompt mechanics without
settings. Switching/disconnecting refreshes displayed state. Existing visual
behavior remains covered by T11/T12; no wholesale screen rewrite is needed.

### T14 - Make Architecture Tests Enforce the Actual Boundaries [P3]

**Dependency:** T13 to avoid codifying known violations as exceptions.

**Files:** `src/EtcdTerminal.Tests/ArchitectureTests.cs`, test fixtures, and the
test project file only if an IL/semantic inspection library is necessary.

**Implementation:**
1. Replace signature-only dependency discovery with a focused IL-aware or semantic dependency check. Prefer a maintained inspection library to a hand-written opcode parser.
2. Detect method-body references, including generated async/lambda types, to Infrastructure/vendor APIs from forbidden App namespaces and direct Console calls from Engine/Components/screens.
3. Preserve the composition-root exception and feature-local rendering allowance. Enforce domain assembly independence from App, Infrastructure, Spectre, dotnet-etcd, and gRPC.
4. Add explicit checks for the app configuration/session dependencies removed in T09/T13. Do not broadly forbid all domain namespaces since terminal/localization contracts live there too.
5. Replace the single-spelling escape scan with a check that covers actual escape literal values in forbidden UI code. Do not reject harmless mentions in comments or the allowed Infrastructure implementations.
6. Test the checker against deliberately violating fixtures/snippets outside the production assemblies. Include a direct static call that has no forbidden type in its public signature, an async method, and a valid composition-root case.

**Acceptance:** The checker proves it detects its positive fixtures and accepts
valid boundaries. Real production code passes without blanket exemptions or
weakened assertions. The ordinary test suite remains independent of live etcd.

## Completion Checklist

- [ ] Each implemented item has its specified regression coverage.
- [ ] All changed contracts have updated callers, DI registrations, and fakes.
- [ ] `dotnet build src/EtcdTerminal.slnx` passes.
- [ ] `dotnet test src/EtcdTerminal.slnx` passes.
- [ ] Protocol-sensitive items have isolated etcd verification, or are explicitly reported as integration-unverified.
- [ ] Layout-sensitive items have small-window/resize tests and a real-terminal smoke test, or are explicitly reported as manually unverified.
- [ ] No real configuration, credentials, or production etcd data were touched by tests.
- [ ] Final diff contains only the selected work item and its tests/documentation.

## Deliberately Deferred

Do not split `DotnetEtcdBasedClient` solely because it is long: first add the
transport seam and contract coverage, then split only if independent change
pressure justifies it. Do not replace all concrete components with interfaces.
Do not add binary value editing, automatic configuration repair, distributed
configuration locking, pagination over live etcd, a plugin system, or the
planned `Panel` component as part of this remediation. These need separate
requirements and should not distract from the verified defects above.
