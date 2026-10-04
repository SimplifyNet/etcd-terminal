using EtcdTerminal.Configuration;
using EtcdTerminal.Infrastructure;
using EtcdTerminal.Keys;
using EtcdTerminal.Permissions;
using EtcdTerminal.Roles;
using EtcdTerminal.Security;
using EtcdTerminal.Users;
using Grpc.Net.Client;

namespace EtcdTerminal.IntegrationTests;

public sealed class DotnetEtcdBasedClient : IEtcdConnection, IEtcdKeyStore, IEtcdUserAdmin, IEtcdRoleAdmin, IEtcdAuthAdmin, IDisposable
{
	private readonly EtcdConnectionHandle _handle;
	private readonly DotnetEtcdKeyStore _keys;
	private readonly DotnetEtcdUserAdmin _users;
	private readonly DotnetEtcdRoleAdmin _roles;
	private readonly DotnetEtcdAuthAdmin _auth;

	public DotnetEtcdBasedClient(Func<string, string?, string?, Action<GrpcChannelOptions>, dotnet_etcd.interfaces.IEtcdClient> createTransport)
	{
		_handle = new EtcdConnectionHandle(createTransport);
		_keys = new DotnetEtcdKeyStore(_handle);
		_users = new DotnetEtcdUserAdmin(_handle);
		_roles = new DotnetEtcdRoleAdmin(_handle);
		_auth = new DotnetEtcdAuthAdmin(_handle);
	}

	public Task ConnectAsync(EtcdConnectionConfig config, CancellationToken ct = default) => _handle.ConnectAsync(config, ct);
	public Task DisconnectAsync() => _handle.DisconnectAsync();
	public void Disconnect() => _handle.Disconnect();
	public void Dispose() => _handle.Dispose();

	public Task<EtcdKeyValue?> GetKeyAsync(string key, CancellationToken ct = default) => _keys.GetKeyAsync(key, ct);
	public Task<IReadOnlyList<EtcdKeyValue>> GetKeysByPrefixAsync(string prefix, CancellationToken ct = default) => _keys.GetKeysByPrefixAsync(prefix, ct);
	public Task<IReadOnlyList<EtcdKeyValue>> GetKeysByRangeAsync(string start, string endExclusive, CancellationToken ct = default) => _keys.GetKeysByRangeAsync(start, endExclusive, ct);
	public Task<bool> CreateKeyAsync(string key, string value, CancellationToken ct = default) => _keys.CreateKeyAsync(key, value, ct);
	public Task<bool> UpdateKeyAsync(string key, string value, CancellationToken ct = default) => _keys.UpdateKeyAsync(key, value, ct);
	public Task<bool> DeleteKeyAsync(string key, CancellationToken ct = default) => _keys.DeleteKeyAsync(key, ct);

	public Task<IReadOnlyList<EtcdUser>> GetUsersAsync(CancellationToken ct = default) => _users.GetUsersAsync(ct);
	public Task<EtcdUser?> GetUserAsync(string username, CancellationToken ct = default) => _users.GetUserAsync(username, ct);
	public Task<EtcdOperationResult> CreateUserAsync(string username, string password, CancellationToken ct = default) => _users.CreateUserAsync(username, password, ct);
	public Task<EtcdOperationResult> DeleteUserAsync(string username, CancellationToken ct = default) => _users.DeleteUserAsync(username, ct);
	public Task<EtcdOperationResult> ChangeUserPasswordAsync(string username, string newPassword, CancellationToken ct = default) => _users.ChangeUserPasswordAsync(username, newPassword, ct);
	public Task<EtcdOperationResult> GrantRoleToUserAsync(string username, string roleName, CancellationToken ct = default) => _users.GrantRoleToUserAsync(username, roleName, ct);
	public Task<EtcdOperationResult> RevokeRoleFromUserAsync(string username, string roleName, CancellationToken ct = default) => _users.RevokeRoleFromUserAsync(username, roleName, ct);

	public Task<IReadOnlyList<EtcdRole>> GetRolesAsync(CancellationToken ct = default) => _roles.GetRolesAsync(ct);
	public Task<EtcdRole?> GetRoleAsync(string roleName, CancellationToken ct = default) => _roles.GetRoleAsync(roleName, ct);
	public Task<EtcdOperationResult> CreateRoleAsync(string roleName, CancellationToken ct = default) => _roles.CreateRoleAsync(roleName, ct);
	public Task<EtcdOperationResult> DeleteRoleAsync(string roleName, CancellationToken ct = default) => _roles.DeleteRoleAsync(roleName, ct);
	public Task<EtcdOperationResult> GrantPermissionAsync(string roleName, PermissionType permissionType, string key, PermissionScope scope, CancellationToken ct = default) => _roles.GrantPermissionAsync(roleName, permissionType, key, scope, ct);
	public Task<EtcdOperationResult> RevokePermissionAsync(string roleName, string key, PermissionScope scope, CancellationToken ct = default) => _roles.RevokePermissionAsync(roleName, key, scope, ct);

	public Task<bool> IsAuthenticationEnabledAsync(CancellationToken ct = default) => _auth.IsAuthenticationEnabledAsync(ct);
}
