using EtcdTerminal.Permissions;
using EtcdTerminal.Roles;

namespace EtcdTerminal.Tests.Fakes;

/// <summary>
/// Records the administration calls a command makes, so a cancelled prompt can
/// be asserted to stop before the administration API is reached at all.
/// </summary>
public sealed class FakeRoleAdmin : IEtcdRoleAdmin
{
	public int Calls { get; private set; }

	public List<(string RoleName, PermissionType Type, string Key, PermissionScope Scope)> Granted { get; } = [];

	public IReadOnlyList<EtcdRole> Roles { get; init; } = [];

	/// Holds the role list until the load token fires, so a cancellation test
	/// cannot race a load that would finish before Escape is read.
	public bool HoldRoles { get; init; }

	public async Task<IReadOnlyList<EtcdRole>> GetRolesAsync(CancellationToken ct = default)
	{
		Calls++;

		if (HoldRoles)
			await Task.Delay(Timeout.Infinite, ct);

		return Roles;
	}

	public Task<EtcdRole?> GetRoleAsync(string roleName, CancellationToken ct = default) => throw new NotSupportedException();

	public Task<EtcdOperationResult> CreateRoleAsync(string roleName, CancellationToken ct = default)
	{
		Calls++;

		return Task.FromResult(EtcdOperationResult.Ok());
	}

	public Task<EtcdOperationResult> DeleteRoleAsync(string roleName, CancellationToken ct = default)
	{
		Calls++;

		return Task.FromResult(EtcdOperationResult.Ok());
	}

	public Task<EtcdOperationResult> GrantPermissionAsync(string roleName, PermissionType permissionType, string key, PermissionScope scope, CancellationToken ct = default)
	{
		Calls++;
		Granted.Add((roleName, permissionType, key, scope));

		return Task.FromResult(EtcdOperationResult.Ok());
	}

	public Task<EtcdOperationResult> RevokePermissionAsync(string roleName, string key, PermissionScope scope, CancellationToken ct = default)
	{
		Calls++;

		return Task.FromResult(EtcdOperationResult.Ok());
	}
}
