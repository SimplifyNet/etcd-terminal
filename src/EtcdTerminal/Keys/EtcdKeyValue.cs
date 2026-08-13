namespace EtcdTerminal.Models;

public sealed class EtcdKeyValue
{
	public string Key { get; init; } = string.Empty;
	public string Value { get; init; } = string.Empty;
	public long Version { get; init; }
	public long CreateRevision { get; init; }
	public long ModRevision { get; init; }
	public long Lease { get; init; }
}
