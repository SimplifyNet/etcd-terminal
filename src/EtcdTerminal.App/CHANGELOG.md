# Changelog

### Fixed

## [0.8] - Unreleased

### Added

- Menu items are hidden when the connected account lacks the required permissions: key browsing needs read access, key creation and JSON import need write access, user/role/permission management is root-only
- Edit and delete actions in the key browser are only offered for keys the account may write
- Grant/Revoke Permission asks whether access applies to an exact key or to a prefix
- List Roles and the permission view show the scope (exact key / prefix) of every permission

### Changed

- Instance selection menu: empty line separates saved connections from management actions
- Role assign/remove and permission grant/revoke failures now show the etcd error detail instead of a generic message

### Fixed

- Non-root users can now connect: the connection check no longer reads a key that requires permissions and failed with `PermissionDenied`
- Granting a permission now really covers a prefix: the etcd range end was never sent, so every grant silently became a single-key permission
- Revoking a permission matches the granted range instead of only the exact key
- Key visibility respects exact-key permissions instead of treating every permission as a prefix
- Keys and auth status the account may not read no longer abort the operation with an error
- Menu with duplicate labels no longer always picks the first item
- IPv6 connection strings (e.g. `http://[::1]:2379`) are no longer corrupted in menus
- A connection named `Exit`, `Settings` or `ManageConnections` connects instead of triggering the menu action

## [0.7] - 2026-09-16

### Added

- Import JSON: flatten nested JSON into etcd keys with configurable separator and prefix
- Pressing Esc cancels long-running operations (connecting, loading, importing)

### Changed

- Custom spinner
- Config directory, `config.json` and `.key` are created owner-only (`0700`/`0600`) on Linux/macOS
- README documents that stored passwords are obfuscated, not securely protected

### Fixed

- Corrupted config file no longer wipes saved connections
- App starts with default settings when saved settings are unreadable instead of crashing
- Import JSON no longer crashes on JSON arrays; array items are imported with index suffixes
- Renaming a connection no longer leaves a duplicate behind and keeps its position in the list
- Editing a connection keeps the saved password when the password prompt is left empty
- Connections with unreadable saved passwords stay in the list with a warning instead of silently disappearing

## [0.6] - 2026-09-10

### Changed

- Removed "Select etcd instance:" header; now shows helpful message to go to Manage Connections when no connections exist
- Cursor is hidden where not required
- Messages alignment

### Fixed

- Reset keys list after manipulations

### Removed

- Actions confirmations

## [0.5] - 2026-08-13

### Added

- Settings menu on the instance selection screen: keys per page setting (browsing pagination)
- Settings menu on the instance selection screen: trim input values toggle
- Settings persisted to `config.json` (own section, same file as connection instances)

### Changed

- Input values are trimmed by default
- Input trimming can be disabled in Settings
- Passwords are never trimmed
- Menu selection pointer: custom `❯` marker instead of Spectre's `>`
- Selection pointer style unified in menus and key browse list (single source in `TerminalPanel`)
- Menu: all items are always rendered without paging (previously limited to 10 visible items)

### Fixed

- Header was missing in instance windows (Manage Connections, Edit Instance)
- Adding an instance with an empty username no longer cancels the dialog
- Empty username means no authentication (password is not requested)

## [0.4] - 2027-08-06

### Changed

- Selection colors
- Placeholder text
- Key selection dialog design

## [0.3] - 2026-07-30

### Added

- Password encryption in config file (AES-256-GCM, machine-local key)
- Connections edit
- Connections order control

## [0.2] - 2026-07-24

### Added

- Real connection health check before entering main screen
- Background reset on exit (normal exit and Ctrl-C)

### Fixed

- HTTPS/SSL connection: credentials now auto-detected from URI scheme
- `Prompt.Ask(string, string)` no longer returns `defaultValue` on Esc key
- Real gRPC errors are now shown to user instead of generic "Server did not respond"
- GitHub Actions: updated to Node.js 24-compatible action versions

### Changed

- UI: status bar colors (nav hints, connection name, username, version)
- UI: search bar and pagination styled as background boxes
- Removed redundant `UseSsl` checkbox (SSL is determined by URI scheme)

## [0.1]

Initial release