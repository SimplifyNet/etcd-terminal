namespace EtcdTerminal.Models;

public sealed class EtcdUser
{
	public string Username { get; init; } = string.Empty;
	public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
}
