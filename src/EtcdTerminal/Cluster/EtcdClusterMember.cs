namespace EtcdTerminal.Cluster;

public sealed class EtcdClusterMember
{
	public ulong Id { get; init; }
	public string Name { get; init; } = string.Empty;
	public string Version { get; init; } = string.Empty;
	public bool IsLeader { get; init; }
	public bool IsHealthy { get; init; }
	public bool HasStatus { get; init; }
	public long DbSize { get; init; }
	public long DbSizeInUse { get; init; }
	public ulong RaftTerm { get; init; }
	public ulong RaftIndex { get; init; }
	public ulong RaftAppliedIndex { get; init; }
	public IReadOnlyList<string> Errors { get; init; } = [];
	public IReadOnlyList<string> PeerUrls { get; init; } = [];
	public IReadOnlyList<string> ClientUrls { get; init; } = [];
	public bool IsLearner { get; init; }
}
