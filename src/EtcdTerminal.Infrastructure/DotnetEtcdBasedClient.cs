using System.Text;
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

public sealed class DotnetEtcdBasedClient(Func<string, string?, string?, Action<GrpcChannelOptions>, dotnet_etcd.interfaces.IEtcdClient> _createTransport) : IEtcdClient
{
	private dotnet_etcd.interfaces.IEtcdClient? _client;

	private dotnet_etcd.interfaces.IEtcdClient Client => _client ?? throw new InvalidOperationException("Not connected to etcd.");

	private EtcdClient ConcreteClient => Client as EtcdClient ?? throw new InvalidOperationException("Connected transport does not expose connection access.");

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
			? _createTransport(connectionString, config.Username, config.Password, configureChannel)
			: _createTransport(connectionString, null, null, configureChannel);

		try
		{
			await ProbeAsync(ct);
		}
		catch (RpcException ex) when (IsCancellation(ex))
		{
			Disconnect();

			throw;
		}
		catch (RpcException ex)
		{
			Disconnect();

			throw Translate(ex);
		}
		catch
		{
			Disconnect();

			throw;
		}
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
		try
		{
			var response = await Client.GetAsync(key, cancellationToken: ct);

			if (response.Kvs.Count == 0)
				return null;

			return MapKeyValue(response.Kvs[0]);
		}
		catch (RpcException ex) when (IsCancellation(ex))
		{
			throw;
		}
		catch (RpcException ex)
		{
			throw Translate(ex);
		}
	}

	public async Task<IReadOnlyList<EtcdKeyValue>> GetKeysByPrefixAsync(string prefix, CancellationToken ct = default)
	{
		// The vendor prefix helper is not byte-exact (e.g. for a prefix ending in
		// U+D7FF), so the [prefix, successor) bounds are built here explicitly.
		// An empty prefix reads all keys as [zero-byte, zero-byte).
		try
		{
			var start = prefix.Length == 0 || prefix == PermissionRange.AllKeys
				? [0]
				: Encoding.UTF8.GetBytes(prefix);

			var response = await Client.GetAsync(new RangeRequest
			{
				Key = ByteString.CopyFrom(start),
				RangeEnd = ByteString.CopyFrom(PermissionRange.PrefixRangeEndBytes(start))
			}, cancellationToken: ct);

			return [.. response.Kvs.Select(MapKeyValue)];
		}
		catch (RpcException ex) when (IsCancellation(ex))
		{
			throw;
		}
		catch (RpcException ex)
		{
			throw Translate(ex);
		}
	}

	public async Task<IReadOnlyList<EtcdKeyValue>> GetKeysByRangeAsync(string start, string endExclusive, CancellationToken ct = default)
	{
		if (endExclusive.Length == 0)
			throw new ArgumentException("A range read requires an explicit exclusive end.", nameof(endExclusive));

		try
		{
			var response = await Client.GetAsync(new RangeRequest
			{
				Key = ByteString.CopyFromUtf8(start),
				RangeEnd = endExclusive == PermissionRange.AllKeys
					? ByteString.CopyFrom(0x00)
					: ByteString.CopyFromUtf8(endExclusive)
			}, cancellationToken: ct);

			return [.. response.Kvs.Select(MapKeyValue)];
		}
		catch (RpcException ex) when (IsCancellation(ex))
		{
			throw;
		}
		catch (RpcException ex)
		{
			throw Translate(ex);
		}
	}

	public async Task<bool> CreateKeyAsync(string key, string value, CancellationToken ct = default)
	{
		try
		{
			var response = await Client.TransactionAsync(new TxnRequest
			{
				Compare = { VersionIs(key, 0, Compare.Types.CompareResult.Equal) },
				Success = { PutValue(key, value, ignoreLease: false) }
			}, cancellationToken: ct);

			return response.Succeeded;
		}
		catch (RpcException ex) when (IsCancellation(ex))
		{
			throw;
		}
		catch (RpcException ex)
		{
			throw Translate(ex);
		}
	}

	public async Task<bool> UpdateKeyAsync(string key, string value, CancellationToken ct = default)
	{
		try
		{
			var response = await Client.TransactionAsync(new TxnRequest
			{
				Compare = { VersionIs(key, 0, Compare.Types.CompareResult.Greater) },
				Success = { PutValue(key, value, ignoreLease: true) }
			}, cancellationToken: ct);

			return response.Succeeded;
		}
		catch (RpcException ex) when (IsCancellation(ex))
		{
			throw;
		}
		catch (RpcException ex)
		{
			throw Translate(ex);
		}
	}

	public async Task<bool> DeleteKeyAsync(string key, CancellationToken ct = default)
	{
		try
		{
			var response = await Client.DeleteAsync(key, cancellationToken: ct);

			return response.Deleted > 0;
		}
		catch (RpcException ex) when (IsCancellation(ex))
		{
			throw;
		}
		catch (RpcException ex)
		{
			throw Translate(ex);
		}
	}

	public async Task<IReadOnlyList<EtcdUser>> GetUsersAsync(CancellationToken ct = default)
	{
		try
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
		catch (RpcException ex) when (IsCancellation(ex))
		{
			throw;
		}
		catch (RpcException ex)
		{
			throw Translate(ex);
		}
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
		catch (RpcException ex) when (IsCancellation(ex))
		{
			throw;
		}
		catch (RpcException ex) when (IsMissingUser(ex))
		{
			return null;
		}
		catch (RpcException ex)
		{
			throw Translate(ex);
		}
	}

	public async Task<EtcdOperationResult> CreateUserAsync(string username, string password, CancellationToken ct = default)
	{
		try
		{
			await Client.UserAddAsync(
				new AuthUserAddRequest { Name = username, Password = password }, cancellationToken: ct);

			return EtcdOperationResult.Ok();
		}
		catch (RpcException ex) when (IsCancellation(ex))
		{
			throw;
		}
		catch (RpcException ex)
		{
			return RpcFail(ex);
		}
	}

	public async Task<EtcdOperationResult> DeleteUserAsync(string username, CancellationToken ct = default)
	{
		try
		{
			await Client.UserDeleteAsync(
				new AuthUserDeleteRequest { Name = username }, cancellationToken: ct);

			return EtcdOperationResult.Ok();
		}
		catch (RpcException ex) when (IsCancellation(ex))
		{
			throw;
		}
		catch (RpcException ex)
		{
			return RpcFail(ex);
		}
	}

	public async Task<EtcdOperationResult> ChangeUserPasswordAsync(string username, string newPassword, CancellationToken ct = default)
	{
		try
		{
			await Client.UserChangePasswordAsync(
				new AuthUserChangePasswordRequest { Name = username, Password = newPassword }, cancellationToken: ct);

			return EtcdOperationResult.Ok();
		}
		catch (RpcException ex) when (IsCancellation(ex))
		{
			throw;
		}
		catch (RpcException ex)
		{
			return RpcFail(ex);
		}
	}

	public async Task<IReadOnlyList<EtcdRole>> GetRolesAsync(CancellationToken ct = default)
	{
		try
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
		catch (RpcException ex) when (IsCancellation(ex))
		{
			throw;
		}
		catch (RpcException ex)
		{
			throw Translate(ex);
		}
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
		catch (RpcException ex) when (IsCancellation(ex))
		{
			throw;
		}
		catch (RpcException ex) when (IsMissingRole(ex))
		{
			return null;
		}
		catch (RpcException ex)
		{
			throw Translate(ex);
		}
	}

	public async Task<EtcdOperationResult> CreateRoleAsync(string roleName, CancellationToken ct = default)
	{
		try
		{
			await Client.RoleAddAsync(
				new AuthRoleAddRequest { Name = roleName }, cancellationToken: ct);

			return EtcdOperationResult.Ok();
		}
		catch (RpcException ex) when (IsCancellation(ex))
		{
			throw;
		}
		catch (RpcException ex)
		{
			return RpcFail(ex);
		}
	}

	public async Task<EtcdOperationResult> DeleteRoleAsync(string roleName, CancellationToken ct = default)
	{
		try
		{
			await Client.RoleDeleteAsync(
				new AuthRoleDeleteRequest { Role = roleName }, cancellationToken: ct);

			return EtcdOperationResult.Ok();
		}
		catch (RpcException ex) when (IsCancellation(ex))
		{
			throw;
		}
		catch (RpcException ex)
		{
			return RpcFail(ex);
		}
	}

	public async Task<EtcdOperationResult> GrantRoleToUserAsync(string username, string roleName, CancellationToken ct = default)
	{
		try
		{
			await Client.UserGrantRoleAsync(
				new AuthUserGrantRoleRequest { User = username, Role = roleName }, cancellationToken: ct);

			return EtcdOperationResult.Ok();
		}
		catch (RpcException ex) when (IsCancellation(ex))
		{
			throw;
		}
		catch (RpcException ex)
		{
			return RpcFail(ex);
		}
	}

	public async Task<EtcdOperationResult> RevokeRoleFromUserAsync(string username, string roleName, CancellationToken ct = default)
	{
		try
		{
			await Client.UserRevokeRoleAsync(
				new AuthUserRevokeRoleRequest { Name = username, Role = roleName }, cancellationToken: ct);

			return EtcdOperationResult.Ok();
		}
		catch (RpcException ex) when (IsCancellation(ex))
		{
			throw;
		}
		catch (RpcException ex)
		{
			return RpcFail(ex);
		}
	}

	public async Task<EtcdOperationResult> GrantPermissionAsync(string roleName, PermissionType permissionType, string key, PermissionScope scope, CancellationToken ct = default)
	{
		if (scope is PermissionScope.Range)
			throw new ArgumentException("A range grant requires an explicit range end.", nameof(scope));

		try
		{
			var permType = MapPermissionType(permissionType);

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
		}
		catch (RpcException ex) when (IsCancellation(ex))
		{
			throw;
		}
		catch (RpcException ex)
		{
			return RpcFail(ex);
		}
	}

	public async Task<EtcdOperationResult> RevokePermissionAsync(string roleName, PermissionType permissionType, string key, PermissionScope scope, CancellationToken ct = default)
	{
		if (scope is PermissionScope.Range)
			throw new ArgumentException("A range revocation requires an explicit range end.", nameof(scope));

		try
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
		}
		catch (RpcException ex) when (IsCancellation(ex))
		{
			throw;
		}
		catch (RpcException ex)
		{
			return RpcFail(ex);
		}
	}

	public async Task<bool> IsAuthenticationEnabledAsync(CancellationToken ct = default)
	{
		try
		{
			var call = ConcreteClient.GetConnection().AuthClient.AuthStatusAsync(new AuthStatusRequest(), null, null, ct);
			var response = await call.ResponseAsync;

			return response.Enabled;
		}
		catch (RpcException ex) when (ex.StatusCode == StatusCode.PermissionDenied)
		{
			// Only an authenticated session can be denied here, so auth is definitely on.
			return true;
		}
		catch (RpcException ex) when (IsCancellation(ex))
		{
			throw;
		}
		catch (RpcException ex)
		{
			throw Translate(ex);
		}
	}

	/// <summary>
	/// Verifies connectivity and credentials. MemberList is used instead of a key read because it
	/// requires no key permissions, so non-root accounts can connect too.
	/// </summary>
	private Task ProbeAsync(CancellationToken ct) =>
		Client.MemberListAsync(new MemberListRequest(), cancellationToken: ct);

	private static EtcdOperationResult RpcFail(RpcException ex) =>
		EtcdOperationResult.Fail(DetailOrMessage(ex));

	private static bool IsCancellation(RpcException ex) =>
		ex.StatusCode == StatusCode.Cancelled;

	private static EtcdOperationException Translate(RpcException ex) => ex.StatusCode switch
	{
		StatusCode.PermissionDenied => AccessDenied(ex),
		StatusCode.Unauthenticated => AccessDenied(ex),
		StatusCode.Unavailable => Failure(EtcdOperationFailureKind.Unavailable, ex),
		StatusCode.DeadlineExceeded => Failure(EtcdOperationFailureKind.Unconfirmed, ex),
		StatusCode.InvalidArgument when IsAuthenticationFailure(ex) => AccessDenied(ex),
		_ => Failure(EtcdOperationFailureKind.TransportError, ex)
	};

	private static EtcdOperationException AccessDenied(RpcException ex) =>
		Failure(EtcdOperationFailureKind.AccessDenied, ex);

	private static EtcdOperationException Failure(EtcdOperationFailureKind kind, RpcException ex) =>
		new(kind, $"etcd operation failed ({ex.StatusCode}): {DetailOrMessage(ex)}", ex);

	private static string DetailOrMessage(RpcException ex) =>
		string.IsNullOrEmpty(ex.Status.Detail) ? ex.Message : ex.Status.Detail;

	// Verified against etcd 3.7.1: missing users and roles surface as
	// FailedPrecondition with a not-found detail, not as gRPC NotFound.
	private static bool IsMissingUser(RpcException ex) =>
		ex.StatusCode == StatusCode.FailedPrecondition && ex.Status.Detail.Contains("user name not found", StringComparison.Ordinal);

	private static bool IsMissingRole(RpcException ex) =>
		ex.StatusCode == StatusCode.FailedPrecondition && ex.Status.Detail.Contains("role name not found", StringComparison.Ordinal);

	private static bool IsAuthenticationFailure(RpcException ex) =>
		ex.Status.Detail.Contains("authentication failed", StringComparison.Ordinal);

	private static Compare VersionIs(string key, long version, Compare.Types.CompareResult result) => new()
	{
		Result = result,
		Target = Compare.Types.CompareTarget.Version,
		Key = ByteString.CopyFromUtf8(key),
		Version = version
	};

	private static RequestOp PutValue(string key, string value, bool ignoreLease) => new()
	{
		RequestPut = new PutRequest
		{
			Key = ByteString.CopyFromUtf8(key),
			Value = ByteString.CopyFromUtf8(value),
			IgnoreLease = ignoreLease
		}
	};

	private static EtcdKeyValue MapKeyValue(KeyValue kv) => new()
	{
		Key = kv.Key.ToStringUtf8(),
		Value = kv.Value.ToStringUtf8(),
		Version = kv.Version,
		CreateRevision = kv.CreateRevision,
		ModRevision = kv.ModRevision,
		Lease = kv.Lease
	};

	// Text bounds (including the zero-byte sentinel) round-trip through UTF-8 exactly.
	// Non-text server bounds decode with replacement characters: display-safe and
	// self-consistent, but not byte-faithful. There is no binary key editor.
	private static EtcdPermission MapPermission(Permission perm) => new()
	{
		Type = perm.PermType switch
		{
			Permission.Types.Type.Read => PermissionType.Read,
			Permission.Types.Type.Write => PermissionType.Write,
			Permission.Types.Type.Readwrite => PermissionType.ReadWrite,
			_ => PermissionType.Read
		},
		KeyPrefix = perm.Key.ToStringUtf8(),
		RangeEnd = perm.RangeEnd.ToStringUtf8()
	};

	private static Permission.Types.Type MapPermissionType(PermissionType type) => type switch
	{
		PermissionType.Read => Permission.Types.Type.Read,
		PermissionType.Write => Permission.Types.Type.Write,
		PermissionType.ReadWrite => Permission.Types.Type.Readwrite,
		_ => Permission.Types.Type.Read
	};
}
