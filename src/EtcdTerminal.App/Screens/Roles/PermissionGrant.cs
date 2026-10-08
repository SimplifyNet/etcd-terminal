using EtcdTerminal.Permissions;

namespace EtcdTerminal.App.Screens.Roles;

public sealed record PermissionGrant(PermissionTarget Target, PermissionType Type);
