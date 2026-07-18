namespace EtcdTerminal.Models;

public sealed class EtcdPermission
{
	public PermissionType Type { get; init; }
	public string KeyPrefix { get; init; } = string.Empty;
}
