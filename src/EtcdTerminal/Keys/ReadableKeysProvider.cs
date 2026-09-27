using EtcdTerminal.Permissions;
using EtcdTerminal.Security;

namespace EtcdTerminal.Keys;

public sealed class ReadableKeysProvider(IEtcdKeyStore _keyStore) : IReadableKeysProvider
{
	public async Task<IReadOnlyList<EtcdKeyValue>> GetReadableKeysAsync(UserCapabilities capabilities, CancellationToken ct = default)
	{
		if (capabilities.IsRoot)
			return await _keyStore.GetKeysByPrefixAsync("", ct);

		var keys = new List<EtcdKeyValue>();

		foreach (var permission in capabilities.Permissions)
		{
			if (permission.Type is not (PermissionType.Read or PermissionType.ReadWrite))
				continue;

			IReadOnlyList<EtcdKeyValue> permissionKeys = permission.Scope switch
			{
				PermissionScope.Key => await GetExactOrEmptyAsync(permission.KeyPrefix, ct),
				PermissionScope.Range => await _keyStore.GetKeysByRangeAsync(permission.KeyPrefix, permission.RangeEnd, ct),
				_ => await _keyStore.GetKeysByPrefixAsync(EtcdPermission.NormalizeKey(permission.KeyPrefix), ct)
			};

			keys.AddRange(permissionKeys.Where(kv => permission.Covers(kv.Key)));
		}

		return [.. keys.DistinctBy(kv => kv.Key)];
	}

	private async Task<IReadOnlyList<EtcdKeyValue>> GetExactOrEmptyAsync(string key, CancellationToken ct)
	{
		var exact = await _keyStore.GetKeyAsync(key, ct);

		return exact is null ? [] : [exact];
	}
}
