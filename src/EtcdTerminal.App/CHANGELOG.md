# Changelog

## [0.3] - Unreleased

### Added

- Password encryption in config file (AES-256-GCM, machine-local key)
- Real connection health check before entering main screen
- Background reset on exit (normal exit and Ctrl-C)

## [0.2] - 2026-07-24

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