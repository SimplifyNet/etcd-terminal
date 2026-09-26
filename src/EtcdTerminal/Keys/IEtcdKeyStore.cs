namespace EtcdTerminal.Keys;

public interface IEtcdKeyStore
{
	/// <summary>
	/// Returns the key or null when it is absent. Denied reads and transport
	/// failures are reported as <see cref="EtcdOperationException"/>, never as null.
	/// </summary>
	Task<EtcdKeyValue?> GetKeyAsync(string key, CancellationToken ct = default);
	/// <summary>
	/// Returns matching keys; an empty collection means no keys matched, not a failure.
	/// Failures are reported as <see cref="EtcdOperationException"/>.
	/// </summary>
	Task<IReadOnlyList<EtcdKeyValue>> GetKeysByPrefixAsync(string prefix, CancellationToken ct = default);
	/// <summary>
	/// Creates a key only if it is absent, using a server-side transaction.
	/// Returns true when the key was created and false when it already exists.
	/// Transport, authentication, and permission failures (the presence check
	/// requires read permission) are reported as exceptions, never as false.
	/// </summary>
	Task<bool> CreateKeyAsync(string key, string value, CancellationToken ct = default);
	/// <summary>
	/// Updates a key only if it is present, using a server-side transaction that
	/// preserves the key's existing lease. Returns true when the key was updated
	/// and false when it is absent. Failures are reported as exceptions, never as false.
	/// </summary>
	Task<bool> UpdateKeyAsync(string key, string value, CancellationToken ct = default);
	Task<bool> DeleteKeyAsync(string key, CancellationToken ct = default);
}
