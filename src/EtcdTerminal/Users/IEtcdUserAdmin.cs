namespace EtcdTerminal.Users;

public interface IEtcdUserAdmin
{
	Task<IReadOnlyList<EtcdUser>> GetUsersAsync(CancellationToken ct = default);
	/// <summary>
	/// Returns the user or null when no such user exists. Failures are reported
	/// as <see cref="EtcdOperationException"/>, never as null.
	/// </summary>
	Task<EtcdUser?> GetUserAsync(string username, CancellationToken ct = default);
	Task<EtcdOperationResult> CreateUserAsync(string username, string password, CancellationToken ct = default);
	Task<EtcdOperationResult> DeleteUserAsync(string username, CancellationToken ct = default);
	Task<EtcdOperationResult> ChangeUserPasswordAsync(string username, string newPassword, CancellationToken ct = default);
	Task<EtcdOperationResult> GrantRoleToUserAsync(string username, string roleName, CancellationToken ct = default);
	Task<EtcdOperationResult> RevokeRoleFromUserAsync(string username, string roleName, CancellationToken ct = default);
}
