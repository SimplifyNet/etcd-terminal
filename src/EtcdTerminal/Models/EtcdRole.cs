namespace EtcdTerminal.Models;

public sealed class EtcdRole
{
	public string Name { get; init; } = string.Empty;
	public IReadOnlyList<EtcdPermission> Permissions { get; init; } = Array.Empty<EtcdPermission>();
}
