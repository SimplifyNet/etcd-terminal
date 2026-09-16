using Authpb;
using dotnet_etcd;
using dotnet_etcd.interfaces;
using Etcdserverpb;
using EtcdTerminal.Configuration;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Mvccpb;
using EtcdTerminal.Keys;
using EtcdTerminal.Permissions;
using EtcdTerminal.Roles;
using EtcdTerminal.Users;

namespace EtcdTerminal.Infrastructure;

public sealed class DotnetEtcdBasedClient : IEtcdClient
{
	private EtcdClient? _client;

	public bool IsConnected => _client is not null;

	private EtcdClient Client => _client ?? throw new InvalidOperationException("Not connected to etcd.");

	public async Task ConnectAsync(EtcdConnectionConfig config, CancellationToken ct = default)
	{
		Disconnect();

		var connectionString = config.ConnectionString;
		var useSsl = connectionString.StartsWith("https", StringComparison.OrdinalIgnoreCase);

		void configureChannel(GrpcChannelOptions options)
		{
			options.Credentials = useSsl
				? ChannelCredentials.SecureSsl
				: ChannelCredentials.Insecure;
		}

		_client = config.IsAuthenticationEnabled
			? new EtcdClient(connectionString, config.Username!, config.Password!,
				configureChannelOptions: configureChannel)
			: new EtcdClient(connectionString, configureChannelOptions: configureChannel);

		try
		{
			await Client.GetAsync("\0", cancellationToken: ct);
		}
		catch
		{
			Disconnect();

			throw;
		}
	}

	public async Task<bool> PingAsync(CancellationToken ct = default)
	{
		if (_client is null) return false;

		await Client.GetAsync("\0", cancellationToken: ct);

		return true;
	}

	public Task DisconnectAsync()
	{
		Disconnect();

		return Task.CompletedTask;
	}

	public void Disconnect()
	{
		_client?.Dispose();

		_client = null;
	}

	public void Dispose() => Disconnect();

	public async Task<EtcdKeyValue?> GetKeyAsync(string key, CancellationToken ct = default)
	{
		var response = await Client.GetAsync(key, cancellationToken: ct);

		if (response.Kvs.Count == 0)
			return null;

		return MapKeyValue(response.Kvs[0]);
	}

	public async Task<IReadOnlyList<EtcdKeyValue>> GetKeysByPrefixAsync(string prefix, CancellationToken ct = default)
	{
		var response = await Client.GetRangeAsync(prefix, cancellationToken: ct);

		return [.. response.Kvs.Select(MapKeyValue)];
	}

	public async Task<bool> CreateKeyAsync(string key, string value, CancellationToken ct = default)
	{
		var existing = await GetKeyAsync(key, ct);

		if (existing is not null)
			return false;

		await Client.PutAsync(key, value, cancellationToken: ct);

		return true;
	}

	public async Task<bool> UpdateKeyAsync(string key, string value, CancellationToken ct = default)
	{
		var existing = await GetKeyAsync(key, ct);

		if (existing is null)
			return false;

		await Client.PutAsync(key, value, cancellationToken: ct);

		return true;
	}

	public async Task<bool> DeleteKeyAsync(string key, CancellationToken ct = default)
	{
		var response = await Client.DeleteAsync(key, cancellationToken: ct);

		return response.Deleted > 0;
	}

	public async Task<IReadOnlyList<EtcdUser>> GetUsersAsync(CancellationToken ct = default)
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
	}

	public async Task<EtcdUser?> GetUserAsync(string username, CancellationToken ct = default)
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
		catch (RpcException)
		{
			return null;
		}
	}

	public async Task<bool> CreateUserAsync(string username, string password, CancellationToken ct = default)
	{
		try
		{
			await Client.UserAddAsync(
				new AuthUserAddRequest { Name = username, Password = password }, cancellationToken: ct);

			return true;
		}
		catch (RpcException)
		{
			return false;
		}
	}

	public async Task<bool> DeleteUserAsync(string username, CancellationToken ct = default)
	{
		try
		{
			await Client.UserDeleteAsync(
				new AuthUserDeleteRequest { Name = username }, cancellationToken: ct);

			return true;
		}
		catch (RpcException)
		{
			return false;
		}
	}

	public async Task<bool> ChangeUserPasswordAsync(string username, string newPassword, CancellationToken ct = default)
	{
		try
		{
			await Client.UserChangePasswordAsync(
				new AuthUserChangePasswordRequest { Name = username, Password = newPassword }, cancellationToken: ct);

			return true;
		}
		catch (RpcException)
		{
			return false;
		}
	}

	public async Task<IReadOnlyList<EtcdRole>> GetRolesAsync(CancellationToken ct = default)
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
				Permissions = [.. roleInfo.Perm.Select(MapPermission)]
			});
		}

		return roles;
	}

	public async Task<EtcdRole?> GetRoleAsync(string roleName, CancellationToken ct = default)
	{
		try
		{
			var response = await Client.RoleGetAsync(
				new AuthRoleGetRequest { Role = roleName }, cancellationToken: ct);

			return new EtcdRole
			{
				Name = roleName,
				Permissions = [.. response.Perm.Select(MapPermission)]
			};
		}
		catch (RpcException)
		{
			return null;
		}
	}

	public async Task<bool> CreateRoleAsync(string roleName, CancellationToken ct = default)
	{
		try
		{
			await Client.RoleAddAsync(
				new AuthRoleAddRequest { Name = roleName }, cancellationToken: ct);

			return true;
		}
		catch (RpcException)
		{
			return false;
		}
	}

	public async Task<bool> DeleteRoleAsync(string roleName, CancellationToken ct = default)
	{
		try
		{
			await Client.RoleDeleteAsync(
				new AuthRoleDeleteRequest { Role = roleName }, cancellationToken: ct);

			return true;
		}
		catch (RpcException)
		{
			return false;
		}
	}

	public async Task GrantRoleToUserAsync(string username, string roleName, CancellationToken ct = default) =>
		await Client.UserGrantRoleAsync(
			new AuthUserGrantRoleRequest { User = username, Role = roleName }, cancellationToken: ct);

	public async Task RevokeRoleFromUserAsync(string username, string roleName, CancellationToken ct = default) =>
		await Client.UserRevokeRoleAsync(
			new AuthUserRevokeRoleRequest { Name = username, Role = roleName }, cancellationToken: ct);

	public async Task GrantPermissionAsync(string roleName, PermissionType permissionType, string keyPrefix, CancellationToken ct = default)
	{
		var permType = MapPermissionType(permissionType);

		await Client.RoleGrantPermissionAsync(
			new AuthRoleGrantPermissionRequest
			{
				Name = roleName,
				Perm = new Permission
				{
					PermType = permType,
					Key = ByteString.CopyFromUtf8(keyPrefix)
				}
			}, cancellationToken: ct);
	}

	public async Task RevokePermissionAsync(string roleName, PermissionType permissionType, string keyPrefix, CancellationToken ct = default) =>
		await Client.RoleRevokePermissionAsync(
			new AuthRoleRevokePermissionRequest
			{
				Role = roleName,
				Key = ByteString.CopyFromUtf8(keyPrefix)
			}, cancellationToken: ct);

	public async Task<bool> IsAuthenticationEnabledAsync(CancellationToken ct = default)
	{
		var call = Client.GetConnection().AuthClient.AuthStatusAsync(new AuthStatusRequest(), null, null, ct);
		var response = await call.ResponseAsync;

		return response.Enabled;
	}

	public async Task<bool> EnableAuthenticationAsync(CancellationToken ct = default)
	{
		try
		{
			await Client.AuthEnableAsync(new AuthEnableRequest(), cancellationToken: ct);

			return true;
		}
		catch (RpcException)
		{
			return false;
		}
	}

	public async Task<bool> DisableAuthenticationAsync(CancellationToken ct = default)
	{
		try
		{
			await Client.AuthDisableAsync(new AuthDisableRequest(), cancellationToken: ct);

			return true;
		}
		catch (RpcException)
		{
			return false;
		}
	}

	private static EtcdKeyValue MapKeyValue(KeyValue kv) => new()
	{
		Key = kv.Key.ToStringUtf8(),
		Value = kv.Value.ToStringUtf8(),
		Version = kv.Version,
		CreateRevision = kv.CreateRevision,
		ModRevision = kv.ModRevision,
		Lease = kv.Lease
	};

	private static EtcdPermission MapPermission(Permission perm) => new()
	{
		Type = perm.PermType switch
		{
			Permission.Types.Type.Read => PermissionType.Read,
			Permission.Types.Type.Write => PermissionType.Write,
			Permission.Types.Type.Readwrite => PermissionType.ReadWrite,
			_ => PermissionType.Read
		},
		KeyPrefix = perm.Key.ToStringUtf8()
	};

	private static Permission.Types.Type MapPermissionType(PermissionType type) => type switch
	{
		PermissionType.Read => Permission.Types.Type.Read,
		PermissionType.Write => Permission.Types.Type.Write,
		PermissionType.ReadWrite => Permission.Types.Type.Readwrite,
		_ => Permission.Types.Type.Read
	};
}
