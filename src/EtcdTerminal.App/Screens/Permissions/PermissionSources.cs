using EtcdTerminal.Roles;
using EtcdTerminal.Users;

namespace EtcdTerminal.App.Screens.Permissions;

public sealed record PermissionSources(IReadOnlyList<EtcdUser> Users, IReadOnlyList<EtcdRole> Roles);
