# Refactoring Plan: Constructor Dependencies and SRP

This is an execution spec. Every design decision has already been made. Your job is to apply it exactly. If reality does not match this document (a signature differs, a step cannot compile, a test contradicts the spec), **stop and report**. Do not invent a workaround.

## 0. Ground rules

1. Read `AGENTS.md` first and follow it in every file you touch. It covers primary constructors with `_`-prefixed parameters, one public type per file, member order, blank-line rules, no braces around single-statement bodies, collection expressions, and file-scoped namespaces.
2. **Target:** at most **4** primary-constructor parameters for every non-record class in `EtcdTerminal.App` and `EtcdTerminal.Infrastructure`. `ILocalization` counts as a dependency.
3. **Forbidden:** "bag of services" types that only forward several dependencies (e.g. `ScreenServices { Prompt; Message; Spinner; }`). Every new type below owns one behavior.
4. **Behavior must not change.** This covers texts, the order of menu items, the order of prompts, when messages appear relative to reloads, Escape/cancel behavior, and exception propagation. Move code verbatim and change only what the step says.
5. Screens and components never create Infrastructure classes or resolve services. Registrations go only into `src/EtcdTerminal.App/Setup/*Registrations.cs`, using the existing style (`.Register<T>(LifetimeType.Transient)`).
6. New interfaces and classes go into the folder and namespace given in each step. The namespace must match the folder.
7. Do not delete or weaken a test to make it pass. Constructor changes do require updating test harnesses; that is expected. Assertions must stay the same unless a step says otherwise.
8. **One step = one commit.** Before each commit run:
   ```
   dotnet build src/EtcdTerminal.slnx
   dotnet test src/EtcdTerminal.Tests
   ```
   Both must be green with zero new warnings. Commit message style: `[r] <short description>` (matches repo history).
9. To find every construction site that must be updated after a constructor change, use `rg -n "new <TypeName>\(" src`.

### Step 0 - Baseline

- [ ] Run the build and tests and note the number of passing tests. Every later step must keep that number or increase it.

---

## Step 1 - `UserInput`: stop passing the trim setting around

**Problem:** 16 call sites do `var trim = _settings.Current.TrimInputValues;` and then call `_prompt.Ask(..., trim: trim)`. That is the only reason `IAppSettingsStore` is injected into most screens. `Prompt` is documented as a generic mechanism whose trim policy comes from the caller, so **do not change `Prompt`**.

- [ ] Create `src/EtcdTerminal.App/Components/UserInput.cs`:
  ```csharp
  namespace EtcdTerminal.App.Components;

  /// <summary>
  /// Asks the user for text with the trimming policy the user configured.
  /// </summary>
  public sealed class UserInput(Prompt _prompt, IAppSettingsStore _settings)
  {
      public string? Ask(string prompt, bool allowEmpty = false) =>
          _prompt.Ask(prompt, allowEmpty, Trim);

      public string? Ask(string prompt, string defaultValue) =>
          _prompt.Ask(prompt, defaultValue, Trim);

      public string? Secret(string prompt) =>
          _prompt.Secret(prompt, Trim);

      private bool Trim => _settings.Current.TrimInputValues;
  }
  ```
  Member order per AGENTS.md: properties before methods, so `Trim` goes above the methods. Add the needed `using`s.
- [ ] Register it in `PresentationRegistrations.RegisterComponents`: `.Register<UserInput>(LifetimeType.Transient)`.
- [ ] In each class listed below, replace `Prompt _prompt` with `UserInput _input`. Delete every `var trim = ...` line and every `trim:` argument. Remove `IAppSettingsStore _settings` where it is no longer used (see the "Settings still used?" column).

  | Class | Settings still used? |
  |---|---|
  | `RolesManagementScreen` | no, remove |
  | `UsersManagementScreen` | no, remove |
  | `KeyCreateScreen` | no, remove |
  | `KeyImportJsonScreen` | no, remove |
  | `ManageConnectionsScreen` | no, remove. Also remove the `bool trim` parameter of `AskPassword`. |
  | `KeyBrowseScreen` | **yes** (`PageSize`), keep |
  | `SettingsScreen` | handled in Step 9, **do not touch now** |

- [ ] Add `src/EtcdTerminal.Tests/UserInputTests.cs` with 3 tests:
  - Trim enabled: input `"  hi  "` returns `"hi"`.
  - Trim disabled: input `"  hi  "` returns `"  hi  "`.
  - `Secret` follows the setting too.

  Reuse the fakes `PromptTests` and `SettingsScreenTests` already use (`StubTextInput`, `FakeSettingsRepository`, `AppSettingsStore`).
- [ ] Update test harnesses: `rg -n "new (RolesManagementScreen|UsersManagementScreen|KeyCreateScreen|KeyImportJsonScreen|ManageConnectionsScreen|KeyBrowseScreen)\(" src`.
- [ ] Commit `[r] UserInput applies trim setting`.

## Step 2 - Shared result and load helpers

### 2a. `Message.ShowResult` overload

- [ ] In `src/EtcdTerminal.App/Components/Message.cs`, add next to the existing `ShowResult(bool, ...)`:
  ```csharp
  public void ShowResult(EtcdOperationResult result, string success, string failure) =>
      ShowResult(result.Success, success, failure + "\n" + result.ErrorMessage);
  ```
- [ ] Replace every `_message.ShowResult(x.Success, A, B + "\n" + x.ErrorMessage)` with `_message.ShowResult(x, A, B)`. Find them with `rg -n 'ErrorMessage\)' src/EtcdTerminal.App`. Leave the `bool` calls (key store) unchanged.

### 2b. `CancellableLoad`

The pattern "spinner, then warning `OperationCancelled` if cancelled" appears in `ListRolesAsync`, `ListUsersAsync` and `PermissionListScreen.ShowAsync`.

- [ ] Create `src/EtcdTerminal.App/Components/CancellableLoad.cs`:
  ```csharp
  /// <summary>
  /// Loads data behind the spinner. Escape cancels the load, warns that the
  /// operation was cancelled and returns null. Failures reach the caller unchanged.
  /// </summary>
  public sealed class CancellableLoad(Spinner _spinner, Message _message, ILocalization _localization)
  {
      public async Task<T?> RunAsync<T>(string status, Func<CancellationToken, Task<T>> load) where T : class
      {
          T? value = null;

          var loaded = await _spinner.RunAsync(status, async ct => value = await load(ct));

          if (loaded)
              return value;

          _message.ShowWarning(_localization.OperationCancelled);

          return null;
      }
  }
  ```
- [ ] Register it in `RegisterComponents`.
- [ ] Add `src/EtcdTerminal.Tests/CancellableLoadTests.cs`:
  - Completed load returns the value and writes no warning.
  - Escape pressed returns `null` and writes the `OperationCancelled` text.
  - A throwing load propagates the exception.

  Use `FakeKeyReader`, `FakeStatusIndicator` and the canvas fakes from `SpinnerTests` / `PressAnyKeyScrollTests`.
- [ ] Do **not** apply `CancellableLoad` yet. Steps 4-6 use it.
- [ ] Commit `[r] result overload and CancellableLoad`.

## Step 3 - Main menu labels move to the main menu

**Problem:** every `IMainMenuEntry` injects `ILocalization` only to provide `Label`, and `MainScreen` has 5 dependencies.

- [ ] Remove `string Label { get; }` from `Screens/MainMenu/IMainMenuEntry.cs`, and delete the `Label` property from all 6 implementations.
- [ ] Create `Screens/MainMenu/MainMenuLabels.cs`:
  ```csharp
  public sealed class MainMenuLabels(ILocalization _localization)
  {
      public string For(MainMenuAction action) => action switch
      {
          MainMenuAction.BrowseKeys => _localization.BrowseKeys,
          MainMenuAction.CreateKey => _localization.CreateKey,
          MainMenuAction.ImportJson => _localization.ImportJson,
          MainMenuAction.ManageUsers => _localization.ManageUsers,
          MainMenuAction.ManageRoles => _localization.ManageRoles,
          MainMenuAction.ListPermissions => _localization.ListPermissions,
          MainMenuAction.Disconnect => _localization.Disconnect,
          _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
      };
  }
  ```
- [ ] Create `Screens/MainMenu/MainMenuItems.cs`, moving `BuildMenuItems` and the entry lookup out of `MainScreen`:
  ```csharp
  public sealed class MainMenuItems(IEnumerable<IMainMenuEntry> _entries, IConnectionSession _session, MainMenuLabels _labels)
  {
      /// Only the actions the connected account may perform, then Disconnect.
      public List<Choice<MainMenuAction>> Build() { ... same logic, labels via _labels.For(e.Action) / _labels.For(MainMenuAction.Disconnect) ... }

      public IMainMenuEntry? Find(MainMenuAction action) => _entries.FirstOrDefault(e => e.Action == action);
  }
  ```
- [ ] Change `MainScreen` to `MainScreen(IConnectionWorkflow _workflow, MainMenuItems _items, Menu _menu)`. Keep the try/catch disconnect logic exactly as it is.
- [ ] Register `MainMenuLabels` and `MainMenuItems` in `ScreensRegistrations`.
- [ ] Remove `ILocalization` from an entry only if nothing else in the class uses it after `Label` is gone (`rg -n "_localization" <file>`).
- [ ] Tests: add a `MainMenuItemsTests` that checks (1) order and labels equal the previous output (`BrowseKeys, CreateKey, ImportJson, ManageUsers, ManageRoles, ListPermissions, Disconnect` for full capabilities), and (2) unavailable entries are filtered out. `ArchitectureTests.EveryMainMenuEntryIsRegistered` must still pass.
- [ ] Commit `[r] main menu owns its labels`.

## Step 4 - Menu commands for Roles and Users

### 4a. Contract

- [ ] Create `src/EtcdTerminal.App/Components/IMenuCommand.cs`:
  ```csharp
  /// <summary>One action of a sub-menu. The owning screen keeps the menu
  /// title, labels and order; the command only performs the action.</summary>
  public interface IMenuCommand<out TAction> where TAction : struct, Enum
  {
      TAction Action { get; }

      Task ExecuteAsync();
  }
  ```

### 4b. List texts move into list layouts

- [ ] In `Screens/Roles/RoleListLayout.cs`, add the properties `Loading => _localization.LoadingRoles`, `Empty => _localization.NoRolesFound` and `Total => _localization.TotalPermissions`.
- [ ] In `Screens/Users/UserListLayout.cs`, add `Loading => _localization.LoadingUsers`, `Empty => _localization.NoUsersFound` and `Total => _localization.TotalUsers`.
- [ ] In `Screens/Permissions/PermissionListLayout.cs`, add `Empty => _localization.NoPermissionsFound` and `Total => _localization.TotalPermissions`.

### 4c. Permission target prompt (shared by grant and revoke)

- [ ] Move the private `PermissionTarget` record out of `RolesManagementScreen` into `Screens/Roles/PermissionTarget.cs` as `public sealed record PermissionTarget(string RoleName, string Key, PermissionScope Scope);`.
- [ ] Create `Screens/Roles/PermissionGrant.cs`: `public sealed record PermissionGrant(PermissionTarget Target, PermissionType Type);`.
- [ ] Create `Screens/Roles/PermissionTargetPrompt.cs`:
  ```csharp
  public sealed class PermissionTargetPrompt(UserInput _input, PermissionScopeSelector _scope, PermissionTypeSelector _type, ILocalization _localization)
  {
      public PermissionTarget? AskTarget() { /* verbatim body of the old PromptTarget(), using _input */ }

      /// Target first, then the permission type - the order the user saw before.
      public PermissionGrant? AskGrant()
      {
          var target = AskTarget();

          if (target is null)
              return null;

          var type = _type.Select();

          return type is null ? null : new(target, type.Value);
      }
  }
  ```

### 4d. Role commands (folder `Screens/Roles/Commands/`, namespace `EtcdTerminal.App.Screens.Roles.Commands`)

Move each body **verbatim** from the matching private method of `RolesManagementScreen`, then apply Steps 1 and 2.

| Class | Constructor | Source method |
|---|---|---|
| `ListRolesCommand` | `(IEtcdRoleAdmin _roleAdmin, CancellableLoad _load, RoleListLayout _layout, ListBrowser _browser)` | `ListRolesAsync`: `var roles = await _load.RunAsync(_layout.Loading, _roleAdmin.GetRolesAsync); if (roles is null) return; _browser.Show(_layout.Headers(), _layout.Rows(roles), _layout.Empty, _layout.Total);`. Keep the existing comment about permission rows. |
| `CreateRoleCommand` | `(IEtcdRoleAdmin _roleAdmin, UserInput _input, Message _message, ILocalization _localization)` | `CreateRoleAsync` |
| `DeleteRoleCommand` | same as Create | `DeleteRoleAsync` |
| `GrantRolePermissionCommand` | `(IEtcdRoleAdmin _roleAdmin, PermissionTargetPrompt _targets, Message _message, ILocalization _localization)` | `GrantPermissionAsync`, using `_targets.AskGrant()` |
| `RevokeRolePermissionCommand` | same as Grant | `RevokePermissionAsync`, using `_targets.AskTarget()`. Keep the comment. |

Each implements `IMenuCommand<RoleMenuAction>` with the matching `Action`.

> If `GetRolesAsync` does not have the shape `Func<CancellationToken, Task<IReadOnlyList<EtcdRole>>>`, use a lambda `ct => _roleAdmin.GetRolesAsync(ct)`.

- [ ] Rewrite `RolesManagementScreen`:
  ```csharp
  public sealed class RolesManagementScreen(MenuScreen _menuScreen, IEnumerable<IMenuCommand<RoleMenuAction>> _commands, ILocalization _localization) : IMainMenuEntry
  ```
  `ShowAsync` keeps the **same literal choice list** (same order, same labels). The handler becomes `action => _commands.Single(c => c.Action == action).ExecuteAsync()`. Delete the `switch` and all the private methods.
- [ ] Register in `ScreensRegistrations` the new types `PermissionTargetPrompt` and the five commands. Also register the collection, following the existing `IEnumerable<IMainMenuEntry>` pattern:
  ```csharp
  .Register<IEnumerable<IMenuCommand<RoleMenuAction>>>(c =>
  [
      c.Resolve<ListRolesCommand>(),
      c.Resolve<CreateRoleCommand>(),
      c.Resolve<DeleteRoleCommand>(),
      c.Resolve<GrantRolePermissionCommand>(),
      c.Resolve<RevokeRolePermissionCommand>()
  ], LifetimeType.Transient)
  ```
- [ ] Add an architecture test `EveryRoleCommandIsRegistered`, modeled on `EveryMainMenuEntryIsRegistered`. It should also assert that every `RoleMenuAction` value has exactly one command.
- [ ] Add unit tests for `GrantRolePermissionCommand` (prompt order: role, scope, key, type; cancel at each prompt calls no admin method) and `ListRolesCommand` (cancel shows a warning and does not open the browser). Use the existing fakes; if a fake for `IEtcdRoleAdmin` does not exist, add one under `src/EtcdTerminal.Tests/Fakes/`.
- [ ] Commit `[r] role menu actions as commands`.

### 4e. User commands (folder `Screens/Users/Commands/`)

Same procedure. Each class implements `IMenuCommand<UserMenuAction>`.

| Class | Constructor | Source method |
|---|---|---|
| `ListUsersCommand` | `(IEtcdUserAdmin _userAdmin, CancellableLoad _load, UserListLayout _layout, ListBrowser _browser)` | `ListUsersAsync` |
| `CreateUserCommand` | `(IEtcdUserAdmin _userAdmin, UserInput _input, Message _message, ILocalization _localization)` | `CreateUserAsync` |
| `DeleteUserCommand` | same | `DeleteUserAsync` |
| `ChangePasswordCommand` | same | `ChangePasswordAsync` |
| `AssignRoleCommand` | same | `AssignRoleAsync` |
| `RemoveRoleCommand` | same | `RevokeRoleAsync` (action `UserMenuAction.RemoveRole`) |

- [ ] `UsersManagementScreen(MenuScreen _menuScreen, IEnumerable<IMenuCommand<UserMenuAction>> _commands, ILocalization _localization)`. Same literal choice list, same dispatch.
- [ ] Register the commands and the collection. Add `EveryUserCommandIsRegistered`.
- [ ] Commit `[r] user menu actions as commands`.

## Step 5 - `PermissionListScreen`

- [ ] Create `Screens/Permissions/PermissionSources.cs`: `public sealed record PermissionSources(IReadOnlyList<EtcdUser> Users, IReadOnlyList<EtcdRole> Roles);`.
- [ ] Create `Screens/Permissions/PermissionSourcesLoader.cs`:
  ```csharp
  public sealed class PermissionSourcesLoader(IEtcdUserAdmin _userAdmin, IEtcdRoleAdmin _roleAdmin, CancellableLoad _load, ILocalization _localization)
  {
      public Task<PermissionSources?> LoadAsync() =>
          _load.RunAsync(_localization.LoadingPermissions, async ct =>
              new PermissionSources(await _userAdmin.GetUsersAsync(ct), await _roleAdmin.GetRolesAsync(ct)));
  }
  ```
  Users are loaded first, then roles, as before.
- [ ] `PermissionListScreen(PermissionSourcesLoader _loader, PermissionListLayout _layout, ListBrowser _browser, Screen _screen)`. `ShowAsync` calls `_screen.Open()`, then loads; on `null` it returns; otherwise it calls `_browser.Show(_layout.Headers(), _layout.Rows(sources.Users, sources.Roles), _layout.Empty, _layout.Total)`.
- [ ] Register the loader. Commit `[r] permission list loader`.

## Step 6 - `ListBrowser`

- [ ] Add to `Components/Screen.cs`: `public BannerBlock Banner() => _header.BuildModel();`.
- [ ] Create `Components/ListView.cs`:
  ```csharp
  /// The frame of a view-only list page: banner, filter band, one table page, pagination band.
  public sealed class ListView(Screen _screen, BrowseLayout _layout)
  {
      public void Reset() => _screen.Reset();

      public FrameModel Frame(IReadOnlyList<StyledText> headers, IReadOnlyList<IReadOnlyList<StyledText>> pageRows,
          string query, int page, int totalPages, int filteredCount, string emptyText, string totalLabel) =>
          new([ /* exactly the blocks of the old local Frame(), with _screen.Banner() instead of _header.BuildModel() */ ]);
  }
  ```
- [ ] `ListBrowser(ListView _view, ILiveFrame _live, IKeyReader _keys, IAppSettingsStore _settings)`. The local `Frame()` now calls `_view.Frame(...)`, and `_screen.Reset()` becomes `_view.Reset()`. The key handling loop stays **unchanged**.
- [ ] Register `ListView`. Update `ListBrowserTests` (harness only). Commit `[r] ListView owns list frame`.

## Step 7 - `KeyBrowseScreen` and `KeyBrowseControl`

Read `KeyBrowseScreen.cs`, `KeyBrowseControl.cs` and `KeyBrowseScreenTests.cs` fully before starting.

### 7a. Split rendering out of `KeyBrowseControl`

- [ ] Create `Screens/Keys/KeyBrowseViewState.cs`:
  `public sealed record KeyBrowseViewState(string SearchQuery, int CurrentPage, int SelectedIndex, EtcdKeyValue? SelectedKey, bool ShowActions, bool CanModify);`
- [ ] Create `Screens/Keys/KeyBrowseView.cs`:
  ```csharp
  public sealed class KeyBrowseView(KeyBrowseLayout _layout, BrowseLayout _browse, Header _header, ILocalization _localization)
  {
      public FrameModel Frame(KeyBrowseViewState state, IReadOnlyList<EtcdKeyValue> pageKeys, int totalPages, int totalKeys) =>
          new([_header.BuildModel(), .. Body(state, pageKeys, totalPages, totalKeys)]);

      private IEnumerable<Block> Body(...) { /* verbatim old Body(), reading from state */ }
  }
  ```
- [ ] `KeyBrowseControl(IKeyReader _keys, IConnectionSession _session, KeyBrowseView _view)`. Its `Frame(...)` becomes `_view.Frame(new(SearchQuery, CurrentPage, SelectedIndex, SelectedKey, ShowActions, CanModifySelectedKey), pageKeys, totalPages, totalKeys)`. Input handling is unchanged.

### 7b. List state

- [ ] Create `Screens/Keys/KeyBrowseList.cs`, moving everything about the pager and the control out of the screen:
  ```csharp
  public sealed class KeyBrowseList(IReadableKeysProvider _readableKeys, IConnectionSession _session, IAppSettingsStore _settings, KeyBrowseControl _control)
  {
      private readonly KeyPager _pager = new();

      public async Task OpenAsync()      // old: ResetNavigation + ClearSearch + LoadKeysAsync
      public async Task ReloadAsync()    // old ReloadAsync verbatim
      public KeyBrowseCommand ReadCommand()  // CurrentPage() + _control.ReadCommand + ApplyFilter on SearchChanged
      public FrameModel Frame()          // old Frame()
  }
  ```
  Field order per AGENTS.md: `_pager` is a field, so it goes before the constructor members. In practice that means the first line of the body.

### 7c. Edit/delete

- [ ] Create `Screens/Keys/KeyEditPrompt.cs`:
  ```csharp
  public sealed class KeyEditPrompt(Screen _screen, KeyBrowseLayout _layout, UserInput _input, ILocalization _localization)
  {
      public string? AskNewValue(EtcdKeyValue key)   // old: _screen.Open([...two Detail blocks...]) + Ask(EnterNewValue, key.Value)
      public void ShowDeleting(EtcdKeyValue key)     // old: _screen.Open([Detail(DeleteKey, key.Key, Danger)])
  }
  ```
- [ ] Create `Screens/Keys/KeyChanges.cs`:
  ```csharp
  public sealed class KeyChanges(IEtcdKeyStore _keyStore, KeyEditPrompt _prompt, Message _message, ILocalization _localization)
  {
      public async Task EditAsync(EtcdKeyValue key, Func<Task> reload)
      public async Task DeleteAsync(EtcdKeyValue key, Func<Task> reload)
  }
  ```
  Order must stay: prompt, store call, **reload if succeeded**, then the message.
- [ ] `KeyBrowseScreen(KeyBrowseList _list, KeyChanges _changes, Screen _screen, ILiveFrame _live)`. Its `ShowAsync`/`RunLiveLoop`/`HandleCommandAsync` keep the same loop, delegating to `_list` and `_changes` (`_changes.EditAsync(command.SelectedKey!, _list.ReloadAsync)`).
- [ ] Register `KeyBrowseView`, `KeyBrowseList`, `KeyEditPrompt` and `KeyChanges` as Transient. `KeyBrowseControl` is already registered.
- [ ] Update `KeyBrowseScreenTests` (harness only; all assertions must pass unchanged).
- [ ] Commit `[r] split key browser`.

## Step 8 - `KeyCreateScreen` and `KeyImportJsonScreen`

### 8a. Create

- [ ] Create `Screens/Keys/KeyCreateForm.cs`, `KeyCreateForm(Screen _screen, UserInput _input, ILocalization _localization)`, with `public (string Key, string Value)? Ask()`. It calls `_screen.Open()`, asks for the key, then asks for the value with `allowEmpty: true`, and returns `null` on any cancel.
- [ ] `KeyCreateScreen(IEtcdKeyStore _keyStore, KeyCreateForm _form, Message _message, ILocalization _localization)`.

### 8b. Import

Split by responsibility. Move code verbatim.

| New class | Constructor | Responsibility (moved code) |
|---|---|---|
| `ImportSource` (record) | `(string Separator, string Prefix, string Json)` | data |
| `ImportSourceReader` | `(Screen _screen, UserInput _input, MultiLinePasteReader _pasteReader, ILocalization _localization)` | `Task<ImportSource?> ReadAsync()`: `_screen.Open()`, separator (default `":"`), prefix (`allowEmpty: true`), paste |
| `ImportEntriesParser` | `(Message _message, ILocalization _localization)` | `ParseEntries` verbatim, made public as `Parse(ImportSource)` |
| `ImportPreview` | `(Menu _menu, Message _message, ILocalization _localization)` | `_previewLimit`, `BuildPreview`, `ConfirmImport` and the `pastedStatus` block. `bool Confirm(entries, json)` shows the `ImportCancelled` warning itself when declined. |
| `ImportReport` | `(Message _message, ILocalization _localization)` | `ShowImportSummary`, `ShowStopped`, `ShowCancelled` |
| `ImportRunner` | `(IKeyImporter _importer, Spinner _spinner, ImportReport _report, ILocalization _localization)` | `ImportAsync` verbatim |

- [ ] `KeyImportJsonScreen(ImportSourceReader _reader, ImportEntriesParser _parser, ImportPreview _preview, ImportRunner _runner)`. `ShowAsync` runs: read, parse, confirm, then run. It returns early on every `null`/`false`.
- [ ] All files go in `Screens/Keys/Import/`. Register all classes.
- [ ] Add tests: `ImportEntriesParserTests` (invalid JSON gives an error message; empty gives `NoKeysInJson`), and `ImportReportTests` (the cancelled-with-nothing-done branch versus the partial branch). Base them on `KeyImporterTests` fakes.
- [ ] Commit `[r] split key create and import`.

## Step 9 - Settings and connections

### 9a. Settings

- [ ] Create `Screens/Settings/SettingsWriter.cs`, `SettingsWriter(IAppSettingsStore _settings, Message _message, ILocalization _localization)`:
  - `AppSettings Current => _settings.Current;`
  - `bool TrySave(AppSettings updated)` (verbatim)
  - `void ToggleTrimInputValues()` (verbatim)
- [ ] Create `Screens/Settings/PageSizeEditor.cs`, `PageSizeEditor(UserInput _input, SettingsWriter _writer, Message _message, ILocalization _localization)`. Move `MinPageSize`, `MaxPageSize` and `EditPageSize()` into it as `public void Edit()`. `_input.Ask(...)` replaces the explicit trim.
- [ ] `SettingsScreen(Menu _menu, PageSizeEditor _pageSize, SettingsWriter _writer, ILocalization _localization)`. Menu labels read `_writer.Current`.
- [ ] Update `SettingsScreenTests`, `SessionFlowTests` and `ConnectionWorkflowTests` harnesses.

### 9b. Manage connections

- [ ] Create `Screens/Connections/ConnectionEditor.cs`, `ConnectionEditor(IConnectionConfigRepository _configRepo, UserInput _input, Message _message, ILocalization _localization)`, with `public void Add()` → `Save(null, _localization.InstanceAdded)` and `public void Edit(EtcdConnectionConfig existing)` → `Save(existing, _localization.InstanceUpdated)`. Move `SaveInstanceInteractive` and `AskPassword` here verbatim, as private methods.
- [ ] Create `Screens/Connections/ConnectionOrganizer.cs`, `ConnectionOrganizer(IConnectionConfigRepository _configRepo, Menu _menu, Message _message, ILocalization _localization)`, with:
  - `EtcdConnectionConfig? PickForEdit(instances)`
  - `void Move(instances, int direction)`
  - `void Remove(instances)`
  - private `SelectInstanceName`

  Bodies are verbatim.
- [ ] `ManageConnectionsScreen(Menu _menu, ConnectionEditor _editor, ConnectionOrganizer _organizer, ILocalization _localization)`. Keep the action list building (same conditions, same order) and the switch.

### 9c. Instance selection

- [ ] Create `Screens/Connections/DecryptFailureNotice.cs`, `(IDecryptFailureSource _source, Message _message, ILocalization _localization)`, with `void ShowIfAny()` (verbatim `ShowDecryptWarningIfNeeded`).
- [ ] Create `Screens/Connections/InstanceMenuResult.cs`: `public sealed record InstanceMenuResult(IReadOnlyList<EtcdConnectionConfig> Instances, InstanceMenuChoice? Choice);`.
- [ ] Create `Screens/Connections/InstanceMenu.cs`, `(IConnectionConfigRepository _configRepo, DecryptFailureNotice _notice, Menu _menu, ILocalization _localization)`, with `InstanceMenuResult Show()`. **The order is required:** `LoadInstances()`, then `_notice.ShowIfAny()`, then the old `PromptForChoice` (verbatim, including its comment). Decrypt failures are produced by the load.
- [ ] Create `Screens/Connections/InstanceConnector.cs`, `(IConnectionWorkflow _workflow, Spinner _spinner, Message _message, ILocalization _localization)`, with `Task<EtcdConnectionConfig?> ConnectAsync(EtcdConnectionConfig selected)` (verbatim).
- [ ] Create `Screens/Connections/InstanceToolActions.cs`, `(ManageConnectionsScreen _manageConnections, SettingsScreen _settings)`, with `bool Run(InstanceFixedAction action, IReadOnlyList<EtcdConnectionConfig> instances)`. It returns `false` for `Exit` and `true` otherwise.
- [ ] `InstanceSelectionScreen(InstanceMenu _menu, InstanceConnector _connector, InstanceToolActions _tools)`. The loop keeps the same semantics.
- [ ] Register all the new classes. Update `SessionFlowTests`, `ConnectionWorkflowTests`, `DecryptFailureTests` and `ManageConnectionsScreenTests` harnesses.
- [ ] Commit `[r] split settings and connection screens`.

## Step 10 - Infrastructure: `BackgroundBand`

- [ ] Create `src/EtcdTerminal.Infrastructure/Terminal/BandStyle.cs`:
  ```csharp
  public sealed record BandStyle(Color Background, bool TrailingBreak)
  {
      public Style? StripeStyle { get; init; }
      public int StripeOffset { get; init; }
      public int ContentIndent { get; init; }
      public IReadOnlyList<Color>? LineBackgrounds { get; init; }
      public Color? TopPaddingBackground { get; init; }
      public bool VerticalPadding { get; init; } = true;
  }
  ```
- [ ] `BackgroundBand(IRenderable _target, BandStyle _style)`. Replace field reads with `_style.X`. Do not change the rendering logic.
- [ ] Update the 3 call sites: `BlockRenderer.cs:40` and `:49`, and `StatusBarRenderer.cs:34`. Map named arguments to `with`/initializer properties one-to-one.
- [ ] `SpectreRenderingTests`, `LayoutTests` and `StatusBarPersistenceTests` must pass **without changing expected output**.
- [ ] Commit `[r] BandStyle for BackgroundBand`.

## Step 11 - Lock the rule

- [ ] Add to `ArchitectureTests`:
  ```csharp
  [Test]
  public void ConstructorsTakeAtMostFourParameters()
  ```
  - Assemblies: App (`typeof(App.Screens.Connections.InstanceSelectionScreen).Assembly`) and Infrastructure (any public Infrastructure type, e.g. `typeof(Infrastructure.Terminal.BackgroundBand).Assembly`).
  - Types: `IsClass && !IsAbstract`, not compiler-generated (`!type.IsDefined(typeof(CompilerGeneratedAttribute))`, and the name does not contain `<`), not records (`type.GetMethod("<Clone>$") is null`), and namespace not starting with `EtcdTerminal.App.Setup`.
  - Check every public instance constructor: `GetParameters().Length > 4` is a violation formatted as `"{type.FullName}: {count}"`.
  - `Assert.That(violations, Is.Empty)`. **No allowlist.** If something still violates, report it instead of adding an exception.
- [ ] Add under `## Architecture` in `AGENTS.md`:
  > **Constructor size:** a class takes at most 4 constructor parameters (`ILocalization` included). When a class needs more, split it by responsibility: menu commands (`IMenuCommand<TAction>`), focused components, prompts/forms, loaders. Never introduce a container type that only forwards dependencies.
- [ ] Commit `[r] enforce constructor size`.

## Step 12 - Final verification (required; record the results in the final report)

- [ ] `dotnet build src/EtcdTerminal.slnx` and `dotnet test src/EtcdTerminal.Tests`: green, with test count ≥ the Step 0 baseline.
- [ ] Re-run the audit and attach its output:
  ```
  python3 - <<'EOF'
  import re,glob
  rx=re.compile(r'class\s+(\w+)(?:<[^>]*>)?\s*\(([^)]*)\)',re.S)
  for f in glob.glob('src/**/*.cs',recursive=True):
      if '/obj/' in f or 'Tests' in f: continue
      for m in rx.finditer(open(f,encoding='utf-8').read()):
          n=len([x for x in m.group(2).split(',') if x.strip()])
          if n>4: print(n,m.group(1),f)
  EOF
  ```
  Expected output: nothing.
- [ ] **Manual run in a real terminal** (`dotnet run --project src/EtcdTerminal.App`, against a local etcd), with each item marked pass or fail:
  1. The instance list shows; the decrypt warning appears before the menu when applicable; manage connections: add, edit, move, remove.
  2. Settings: change the page size, toggle trim; the labels update.
  3. Connect: Escape during connect gives the cancelled warning.
  4. The main menu shows the same items in the same order for root and for a read-only user.
  5. Browse keys: filter, pages, Enter to open actions, edit (the value is reloaded before the message), delete, Escape. The footer stays at the bottom throughout.
  6. Create key; import JSON (paste, preview, decline, accept, Escape during import).
  7. Users and Roles: every sub-menu item, grant with every scope, Escape at each prompt.
  8. Permission list: Escape during loading.
- [ ] Final report: the list of commits, test counts before and after, audit output, the manual checklist with results, and **any scenario you could not verify, stated explicitly**.
