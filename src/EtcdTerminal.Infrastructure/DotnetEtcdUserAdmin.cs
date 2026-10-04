using Etcdserverpb;
using EtcdTerminal.Users;
using Grpc.Core;

namespace EtcdTerminal.Infrastructure;

public sealed class DotnetEtcdUserAdmin(EtcdConnectionHandle _handle) : IEtcdUserAdmin
{
	private dotnet_etcd.interfaces.IEtcdClient Client => _handle.Client;

	public Task<IReadOnlyList<EtcdUser>> GetUsersAsync(CancellationToken ct = default) =>
		_handle.Guard<IReadOnlyList<EtcdUser>>(async () =>
		{
			var response = await Client.UserListAsync(new AuthUserListRequest(), cancellationToken: ct);
			List<EtcdUser> users = [];

			foreach (var user in response.Users)
			{
				var userInfo = await Client.UserGetAsync(new AuthUserGetRequest { Name = user }, cancellationToken: ct);

				users.Add(new EtcdUser
				{
					Username = user,
					Roles = [.. userInfo.Roles]
				});
			}

			return users;
		});

	public Task<EtcdUser?> GetUserAsync(string username, CancellationToken ct = default) =>
		_handle.Guard<EtcdUser?>(async () =>
		{
			try
			{
				var response = await Client.UserGetAsync(
					new AuthUserGetRequest { Name = username }, cancellationToken: ct);

				return new EtcdUser
				{
					Username = username,
					Roles = [.. response.Roles]
				};
			}
			catch (RpcException ex) when (GrpcErrorTranslator.IsMissingUser(ex))
			{
				return null;
			}
		});

	public Task<EtcdOperationResult> CreateUserAsync(string username, string password, CancellationToken ct = default) =>
		_handle.Guard<EtcdOperationResult>(async () =>
		{
			await Client.UserAddAsync(
				new AuthUserAddRequest { Name = username, Password = password }, cancellationToken: ct);

			return EtcdOperationResult.Ok();
		}, GrpcErrorTranslator.RpcFail);

	public Task<EtcdOperationResult> DeleteUserAsync(string username, CancellationToken ct = default) =>
		_handle.Guard<EtcdOperationResult>(async () =>
		{
			await Client.UserDeleteAsync(
				new AuthUserDeleteRequest { Name = username }, cancellationToken: ct);

			return EtcdOperationResult.Ok();
		}, GrpcErrorTranslator.RpcFail);

	public Task<EtcdOperationResult> ChangeUserPasswordAsync(string username, string newPassword, CancellationToken ct = default) =>
		_handle.Guard<EtcdOperationResult>(async () =>
		{
			await Client.UserChangePasswordAsync(
				new AuthUserChangePasswordRequest { Name = username, Password = newPassword }, cancellationToken: ct);

			return EtcdOperationResult.Ok();
		}, GrpcErrorTranslator.RpcFail);

	public Task<EtcdOperationResult> GrantRoleToUserAsync(string username, string roleName, CancellationToken ct = default) =>
		_handle.Guard<EtcdOperationResult>(async () =>
		{
			await Client.UserGrantRoleAsync(
				new AuthUserGrantRoleRequest { User = username, Role = roleName }, cancellationToken: ct);

			return EtcdOperationResult.Ok();
		}, GrpcErrorTranslator.RpcFail);

	public Task<EtcdOperationResult> RevokeRoleFromUserAsync(string username, string roleName, CancellationToken ct = default) =>
		_handle.Guard<EtcdOperationResult>(async () =>
		{
			await Client.UserRevokeRoleAsync(
				new AuthUserRevokeRoleRequest { Name = username, Role = roleName }, cancellationToken: ct);

			return EtcdOperationResult.Ok();
		}, GrpcErrorTranslator.RpcFail);
}
