using EtcdTerminal.Permissions;
using EtcdTerminal.Security;

namespace EtcdTerminal.Keys;

public sealed class ReadableKeysProvider(IEtcdKeyStore _keyStore) : IReadableKeysProvider
{
	public async Task<IReadOnlyList<EtcdKeyValue>> GetReadableKeysAsync(UserCapabilities capabilities, CancellationToken ct = default)
	{
		if (capabilities.IsRoot)
			return await LoadAllKeysAsync(ct);

		var keys = new List<EtcdKeyValue>();

		foreach (var permission in capabilities.Permissions)
		{
			if (permission.Type is not (PermissionType.Read or PermissionType.ReadWrite))
				continue;

			var prefixKeys = await _keyStore.GetKeysByPrefixAsync(UserCapabilities.NormalizePrefix(permission.KeyPrefix), ct);

			keys.AddRange(prefixKeys);
		}

		return [.. keys.DistinctBy(kv => kv.Key)];
	}

	private async Task<List<EtcdKeyValue>> LoadAllKeysAsync(CancellationToken ct)
	{
		var keys = await _keyStore.GetKeysByPrefixAsync("", ct);

		if (keys.Count > 0)
			return [.. keys];

		return [.. await _keyStore.GetKeysByPrefixAsync("/", ct)];
	}
}
