namespace EtcdTerminal.Keys;

public interface IEtcdKeyStore
{
	Task<EtcdKeyValue?> GetKeyAsync(string key, CancellationToken ct = default);
	Task<IReadOnlyList<EtcdKeyValue>> GetKeysByPrefixAsync(string prefix, CancellationToken ct = default);
	Task<bool> CreateKeyAsync(string key, string value, CancellationToken ct = default);
	Task<bool> UpdateKeyAsync(string key, string value, CancellationToken ct = default);
	Task<bool> DeleteKeyAsync(string key, CancellationToken ct = default);
}
