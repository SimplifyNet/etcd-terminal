using EtcdTerminal.Permissions;
using EtcdTerminal.Roles;
using EtcdTerminal.Security;
using EtcdTerminal.Users;

namespace EtcdTerminal.Keys;

public sealed class ReadableKeysProvider(IEtcdKeyStore _keyStore, IEtcdUserAdmin _userAdmin, IEtcdRoleAdmin _roleAdmin, IEtcdAuthAdmin _authAdmin) : IReadableKeysProvider
{
	public async Task<IReadOnlyList<EtcdKeyValue>> GetReadableKeysAsync(string? username, CancellationToken ct = default)
	{
		var authEnabled = await _authAdmin.IsAuthenticationEnabledAsync(ct);

		if (!authEnabled)
			return await LoadAllKeysAsync(ct);

		if (username is null)
			return await LoadAllKeysAsync(ct);

		var user = await _userAdmin.GetUserAsync(username, ct);

		if (user is null || user.Roles.Count == 0 || user.Roles.Contains("root"))
			return await LoadAllKeysAsync(ct);

		var keys = new List<EtcdKeyValue>();

		foreach (var roleName in user.Roles)
		{
			var role = await _roleAdmin.GetRoleAsync(roleName, ct);

			if (role is null)
				continue;

			foreach (var perm in role.Permissions)
			{
				if (perm.Type is not (PermissionType.Read or PermissionType.ReadWrite))
					continue;

				var prefix = perm.KeyPrefix;

				if (prefix == "\0")
					prefix = "";

				var prefixKeys = await _keyStore.GetKeysByPrefixAsync(prefix, ct);

				keys.AddRange(prefixKeys);
			}
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
