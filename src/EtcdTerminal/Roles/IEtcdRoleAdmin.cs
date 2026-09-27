using EtcdTerminal.Permissions;

namespace EtcdTerminal.Roles;

public interface IEtcdRoleAdmin
{
	Task<IReadOnlyList<EtcdRole>> GetRolesAsync(CancellationToken ct = default);
	/// <summary>
	/// Returns the role or null when no such role exists. Failures are reported
	/// as <see cref="EtcdOperationException"/>, never as null.
	/// </summary>
	Task<EtcdRole?> GetRoleAsync(string roleName, CancellationToken ct = default);
	Task<EtcdOperationResult> CreateRoleAsync(string roleName, CancellationToken ct = default);
	Task<EtcdOperationResult> DeleteRoleAsync(string roleName, CancellationToken ct = default);
	Task<EtcdOperationResult> GrantPermissionAsync(string roleName, PermissionType permissionType, string key, PermissionScope scope, CancellationToken ct = default);
	/// <summary>
	/// Removes the whole permission for the target interval, regardless of its type.
	/// etcd revocation has no per-type distinction.
	/// </summary>
	Task<EtcdOperationResult> RevokePermissionAsync(string roleName, string key, PermissionScope scope, CancellationToken ct = default);
}
