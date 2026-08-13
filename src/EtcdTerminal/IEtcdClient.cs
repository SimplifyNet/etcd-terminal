using EtcdTerminal.Configuration;
using EtcdTerminal.Keys;
using EtcdTerminal.Permissions;
using EtcdTerminal.Roles;
using EtcdTerminal.Users;

namespace EtcdTerminal;

public interface IEtcdClient : IDisposable
{
	bool IsConnected { get; }

	Task ConnectAsync(EtcdConnectionConfig config, CancellationToken ct = default);
	Task<bool> PingAsync(CancellationToken ct = default);
	Task DisconnectAsync();

	Task<EtcdKeyValue?> GetKeyAsync(string key, CancellationToken ct = default);
	Task<IReadOnlyList<EtcdKeyValue>> GetKeysByPrefixAsync(string prefix, CancellationToken ct = default);
	Task<IReadOnlyList<EtcdKeyValue>> SearchKeysAsync(string searchTerm, CancellationToken ct = default);
	Task<bool> CreateKeyAsync(string key, string value, CancellationToken ct = default);
	Task<bool> UpdateKeyAsync(string key, string value, CancellationToken ct = default);
	Task<bool> DeleteKeyAsync(string key, CancellationToken ct = default);

	Task<IReadOnlyList<EtcdUser>> GetUsersAsync(CancellationToken ct = default);
	Task<EtcdUser?> GetUserAsync(string username, CancellationToken ct = default);
	Task<bool> CreateUserAsync(string username, string password, CancellationToken ct = default);
	Task<bool> DeleteUserAsync(string username, CancellationToken ct = default);
	Task<bool> ChangeUserPasswordAsync(string username, string newPassword, CancellationToken ct = default);

	Task<IReadOnlyList<EtcdRole>> GetRolesAsync(CancellationToken ct = default);
	Task<EtcdRole?> GetRoleAsync(string roleName, CancellationToken ct = default);
	Task<bool> CreateRoleAsync(string roleName, CancellationToken ct = default);
	Task<bool> DeleteRoleAsync(string roleName, CancellationToken ct = default);

	Task GrantRoleToUserAsync(string username, string roleName, CancellationToken ct = default);
	Task RevokeRoleFromUserAsync(string username, string roleName, CancellationToken ct = default);
	Task GrantPermissionAsync(string roleName, PermissionType permissionType, string keyPrefix, CancellationToken ct = default);
	Task RevokePermissionAsync(string roleName, PermissionType permissionType, string keyPrefix, CancellationToken ct = default);

	Task<bool> IsAuthenticationEnabledAsync(CancellationToken ct = default);
	Task<bool> EnableAuthenticationAsync(CancellationToken ct = default);
	Task<bool> DisableAuthenticationAsync(CancellationToken ct = default);
}
