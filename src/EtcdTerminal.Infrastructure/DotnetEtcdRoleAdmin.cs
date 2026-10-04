using System.Text;
using Authpb;
using Etcdserverpb;
using EtcdTerminal.Permissions;
using EtcdTerminal.Roles;
using Google.Protobuf;
using Grpc.Core;

namespace EtcdTerminal.Infrastructure;

public sealed class DotnetEtcdRoleAdmin(EtcdConnectionHandle _handle) : IEtcdRoleAdmin
{
	private dotnet_etcd.interfaces.IEtcdClient Client => _handle.Client;

	public Task<IReadOnlyList<EtcdRole>> GetRolesAsync(CancellationToken ct = default) =>
		_handle.Guard<IReadOnlyList<EtcdRole>>(async () =>
		{
			var response = await Client.RoleListAsync(new AuthRoleListRequest(), cancellationToken: ct);
			List<EtcdRole> roles = [];

			foreach (var role in response.Roles)
			{
				var roleInfo = await Client.RoleGetAsync(
					new AuthRoleGetRequest { Role = role }, cancellationToken: ct);

				roles.Add(new EtcdRole
				{
					Name = role,
					Permissions = [.. roleInfo.Perm.Select(EtcdProtoMapper.MapPermission)]
				});
			}

			return roles;
		});

	public Task<EtcdRole?> GetRoleAsync(string roleName, CancellationToken ct = default) =>
		_handle.Guard<EtcdRole?>(async () =>
		{
			try
			{
				var response = await Client.RoleGetAsync(
					new AuthRoleGetRequest { Role = roleName }, cancellationToken: ct);

				return new EtcdRole
				{
					Name = roleName,
					Permissions = [.. response.Perm.Select(EtcdProtoMapper.MapPermission)]
				};
			}
			catch (RpcException ex) when (GrpcErrorTranslator.IsMissingRole(ex))
			{
				return null;
			}
		});

	public Task<EtcdOperationResult> CreateRoleAsync(string roleName, CancellationToken ct = default) =>
		_handle.Guard<EtcdOperationResult>(async () =>
		{
			await Client.RoleAddAsync(
				new AuthRoleAddRequest { Name = roleName }, cancellationToken: ct);

			return EtcdOperationResult.Ok();
		}, GrpcErrorTranslator.RpcFail);

	public Task<EtcdOperationResult> DeleteRoleAsync(string roleName, CancellationToken ct = default) =>
		_handle.Guard<EtcdOperationResult>(async () =>
		{
			await Client.RoleDeleteAsync(
				new AuthRoleDeleteRequest { Role = roleName }, cancellationToken: ct);

			return EtcdOperationResult.Ok();
		}, GrpcErrorTranslator.RpcFail);

	public Task<EtcdOperationResult> GrantPermissionAsync(string roleName, PermissionType permissionType, string key, PermissionScope scope, CancellationToken ct = default)
	{
		if (scope is PermissionScope.Range)
			throw new ArgumentException("A range grant requires an explicit range end.", nameof(scope));

		return _handle.Guard<EtcdOperationResult>(async () =>
		{
			var permType = EtcdProtoMapper.MapPermissionType(permissionType);

			var permission = new Permission
			{
				PermType = permType,
				Key = ByteString.CopyFromUtf8(key)
			};

			if (scope is PermissionScope.Prefix)
				permission.RangeEnd = ByteString.CopyFrom(PermissionRange.PrefixRangeEndBytes(Encoding.UTF8.GetBytes(key)));

			await Client.RoleGrantPermissionAsync(
				new AuthRoleGrantPermissionRequest
				{
					Name = roleName,
					Perm = permission
				}, cancellationToken: ct);

			return EtcdOperationResult.Ok();
		}, GrpcErrorTranslator.RpcFail);
	}

	public Task<EtcdOperationResult> RevokePermissionAsync(string roleName, string key, PermissionScope scope, CancellationToken ct = default)
	{
		if (scope is PermissionScope.Range)
			throw new ArgumentException("A range revocation requires an explicit range end.", nameof(scope));

		return _handle.Guard<EtcdOperationResult>(async () =>
		{
			var request = new AuthRoleRevokePermissionRequest
			{
				Role = roleName,
				Key = ByteString.CopyFromUtf8(key)
			};

			if (scope is PermissionScope.Prefix)
				request.RangeEnd = ByteString.CopyFrom(PermissionRange.PrefixRangeEndBytes(Encoding.UTF8.GetBytes(key)));

			await Client.RoleRevokePermissionAsync(request, cancellationToken: ct);

			return EtcdOperationResult.Ok();
		}, GrpcErrorTranslator.RpcFail);
	}
}
