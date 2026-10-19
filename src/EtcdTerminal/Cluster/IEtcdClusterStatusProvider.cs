namespace EtcdTerminal.Cluster;

public interface IEtcdClusterStatusProvider
{
	Task<IReadOnlyList<EtcdClusterMember>> GetMembersAsync(CancellationToken ct = default);
}
