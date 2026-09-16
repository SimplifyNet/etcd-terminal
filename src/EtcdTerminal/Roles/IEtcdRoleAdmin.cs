using EtcdTerminal.Permissions;

namespace EtcdTerminal.Roles;

public interface IEtcdRoleAdmin
{
	Task<IReadOnlyList<EtcdRole>> GetRolesAsync(CancellationToken ct = default);
	Task<EtcdRole?> GetRoleAsync(string roleName, CancellationToken ct = default);
	Task<bool> CreateRoleAsync(string roleName, CancellationToken ct = default);
	Task<bool> DeleteRoleAsync(string roleName, CancellationToken ct = default);
	Task GrantPermissionAsync(string roleName, PermissionType permissionType, string keyPrefix, CancellationToken ct = default);
	Task RevokePermissionAsync(string roleName, PermissionType permissionType, string keyPrefix, CancellationToken ct = default);
}
