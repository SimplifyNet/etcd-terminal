namespace EtcdTerminal.Users;

public interface IEtcdUserAdmin
{
	Task<IReadOnlyList<EtcdUser>> GetUsersAsync(CancellationToken ct = default);
	Task<EtcdUser?> GetUserAsync(string username, CancellationToken ct = default);
	Task<EtcdOperationResult> CreateUserAsync(string username, string password, CancellationToken ct = default);
	Task<EtcdOperationResult> DeleteUserAsync(string username, CancellationToken ct = default);
	Task<EtcdOperationResult> ChangeUserPasswordAsync(string username, string newPassword, CancellationToken ct = default);
	Task GrantRoleToUserAsync(string username, string roleName, CancellationToken ct = default);
	Task RevokeRoleFromUserAsync(string username, string roleName, CancellationToken ct = default);
}
