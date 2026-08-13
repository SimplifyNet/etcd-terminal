namespace EtcdTerminal.Permissions;

public sealed class EtcdPermission
{
	public PermissionType Type { get; init; }
	public string KeyPrefix { get; init; } = string.Empty;
}
