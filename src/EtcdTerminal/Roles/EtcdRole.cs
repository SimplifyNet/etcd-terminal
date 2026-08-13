using EtcdTerminal.Permissions;

namespace EtcdTerminal.Roles;

public sealed class EtcdRole
{
	public string Name { get; init; } = string.Empty;
	public IReadOnlyList<EtcdPermission> Permissions { get; init; } = [];
}
