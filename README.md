# etcd-terminal

**etcd-terminal** — a console client for etcd v3+ with a convenient TUI based on Spectre.Console.

## Features

### Connection management
- Support for multiple etcd instances with switching on startup
- SSL/TLS and non-secure (HTTP) connections
- Login/password authentication for secured instances
- Configuration from `~/.config/etcd-terminal/config.json`

### Key operations (CRUD)
- **Browse** — key overview with prefix-based navigation (Tree view)
- **Search** — global search across keys and their values (text is searched in both key names and values)
- **Create** — add new keys
- **Edit** — modify existing key values
- **Delete** — delete keys with confirmation

### User and role management (RBAC)
- **Users** — list, create, delete, change password
- **Roles** — list, create, delete
- **Role assignment** — grant/revoke roles to/from users
- **Permissions** — view permissions bound to roles (read, write, readwrite)
- **Access grants** — assign/revoke key permissions for roles
- **Info** — view the full picture: which users have which roles and their permissions

## Architecture

The application is built on **Domain-Driven Design** and **Onion Architecture** principles:

```
┌──────────────────────────────────────────┐
│  EtcdTerminal.Console (Presentation)     │
│  Spectre.Console + Simplify.DI           │
├──────────────────────────────────────────┤
│  EtcdTerminal.Infrastructure             │
│  dotnet-etcd, Configuration              │
├──────────────────────────────────────────┤
│  EtcdTerminal (Domain)                   │
│  Models, Interfaces                      │
└──────────────────────────────────────────┘
```

### Projects
- **EtcdTerminal** (Domain) — data models and interfaces (IEtcdClient, IConnectionConfigRepository). No external dependencies.
- **EtcdTerminal.Infrastructure** — interface implementations via the `dotnet-etcd` library, JSON configuration loading.
- **EtcdTerminal.Console** (Presentation) — console user interface with Spectre.Console, DI container with Simplify.DI.

## Technologies

- **.NET 10**
- **Spectre.Console** — TUI for the console application
- **Simplify.DI** — Dependency Injection container
- **Simplify.System** — Simplify utilities
- **dotnet-etcd** — gRPC client for etcd v3+
- **Microsoft.Extensions.Configuration** — JSON configuration loading

## Configuration

Configuration files are stored at `~/.config/etcd-terminal/config.json`.

Example configuration:

```json
{
  "Instances": [
    {
      "Name": "Local etcd",
      "ConnectionString": "http://localhost:2379",
      "UseSsl": false
    },
    {
      "Name": "Production cluster",
      "ConnectionString": "https://etcd.example.com:2379",
      "UseSsl": true,
      "Username": "admin",
      "Password": "secret"
    }
  ]
}
```
