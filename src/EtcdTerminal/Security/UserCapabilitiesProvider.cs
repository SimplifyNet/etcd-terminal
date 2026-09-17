using EtcdTerminal.Permissions;
using EtcdTerminal.Roles;
using EtcdTerminal.Users;

namespace EtcdTerminal.Security;

public sealed class UserCapabilitiesProvider(IEtcdUserAdmin _userAdmin, IEtcdRoleAdmin _roleAdmin, IEtcdAuthAdmin _authAdmin) : IUserCapabilitiesProvider
{
	private const string RootRole = "root";

	public async Task<UserCapabilities> GetCapabilitiesAsync(string? username, CancellationToken ct = default)
	{
		var authEnabled = await _authAdmin.IsAuthenticationEnabledAsync(ct);

		if (!authEnabled || username is null)
			return UserCapabilities.Unrestricted;

		var user = await _userAdmin.GetUserAsync(username, ct);

		if (user is null)
			return new();

		if (user.Roles.Contains(RootRole))
			return UserCapabilities.Unrestricted;

		List<EtcdPermission> permissions = [];

		foreach (var roleName in user.Roles)
		{
			var role = await _roleAdmin.GetRoleAsync(roleName, ct);

			if (role is null)
				continue;

			permissions.AddRange(role.Permissions);
		}

		return new() { Permissions = permissions };
	}
}
