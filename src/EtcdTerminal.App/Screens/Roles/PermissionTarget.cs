using EtcdTerminal.Permissions;

namespace EtcdTerminal.App.Screens.Roles;

public sealed record PermissionTarget(string RoleName, string Key, PermissionScope Scope);
