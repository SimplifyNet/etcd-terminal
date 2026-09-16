using EtcdTerminal;
using EtcdTerminal.Keys;
using EtcdTerminal.Permissions;
using EtcdTerminal.Roles;
using EtcdTerminal.Security;
using EtcdTerminal.Users;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class ReadableKeysProviderTests
{
	[Test]
	public async Task AuthDisabled_ReturnsAllKeys()
	{
		var provider = CreateProvider(authEnabled: false, users: new(), roles: new(), byPrefix: new()
		{
			[""] = [new EtcdKeyValue { Key = "/a/1", Value = "x" }, new EtcdKeyValue { Key = "/b/1", Value = "y" }]
		});

		var keys = await provider.GetReadableKeysAsync("bob");

		Assert.That(keys.Select(kv => kv.Key), Is.EquivalentTo(["/a/1", "/b/1"]));
	}

	[Test]
	public async Task RootUser_ReturnsAllKeys()
	{
		var provider = CreateProvider(authEnabled: true,
			users: new() { ["root-user"] = new EtcdUser { Username = "root-user", Roles = ["root"] } },
			roles: new(),
			byPrefix: new() { [""] = [new EtcdKeyValue { Key = "/a/1", Value = "x" }, new EtcdKeyValue { Key = "/b/1", Value = "y" }] });

		var keys = await provider.GetReadableKeysAsync("root-user");

		Assert.That(keys.Select(kv => kv.Key), Is.EquivalentTo(["/a/1", "/b/1"]));
	}

	[Test]
	public async Task ReadPermission_ReturnsOnlyPermittedKeys()
	{
		var provider = CreateProvider(authEnabled: true,
			users: new() { ["dev"] = new EtcdUser { Username = "dev", Roles = ["dev-role"] } },
			roles: new()
			{
				["dev-role"] = new EtcdRole
				{
					Name = "dev-role",
					Permissions = [new EtcdPermission { Type = PermissionType.Read, KeyPrefix = "/a" }]
				}
			},
			byPrefix: new()
			{
				[""] = [new EtcdKeyValue { Key = "/a/1", Value = "x" }, new EtcdKeyValue { Key = "/b/1", Value = "y" }],
				["/a"] = [new EtcdKeyValue { Key = "/a/1", Value = "x" }]
			});

		var keys = await provider.GetReadableKeysAsync("dev");

		Assert.That(keys.Select(kv => kv.Key), Is.EquivalentTo(["/a/1"]));
	}

	[Test]
	public async Task WriteOnlyPermission_ReturnsNothing()
	{
		var provider = CreateProvider(authEnabled: true,
			users: new() { ["writer"] = new EtcdUser { Username = "writer", Roles = ["writer-role"] } },
			roles: new()
			{
				["writer-role"] = new EtcdRole
				{
					Name = "writer-role",
					Permissions = [new EtcdPermission { Type = PermissionType.Write, KeyPrefix = "/a" }]
				}
			},
			byPrefix: new() { [""] = [new EtcdKeyValue { Key = "/a/1", Value = "x" }] });

		var keys = await provider.GetReadableKeysAsync("writer");

		Assert.That(keys, Is.Empty);
	}

	[Test]
	public async Task NullPrefixPermission_ReturnsAllKeys()
	{
		var provider = CreateProvider(authEnabled: true,
			users: new() { ["admin"] = new EtcdUser { Username = "admin", Roles = ["admin-role"] } },
			roles: new()
			{
				["admin-role"] = new EtcdRole
				{
					Name = "admin-role",
					Permissions = [new EtcdPermission { Type = PermissionType.ReadWrite, KeyPrefix = "\0" }]
				}
			},
			byPrefix: new() { [""] = [new EtcdKeyValue { Key = "/a/1", Value = "x" }, new EtcdKeyValue { Key = "/b/1", Value = "y" }] });

		var keys = await provider.GetReadableKeysAsync("admin");

		Assert.That(keys.Select(kv => kv.Key), Is.EquivalentTo(["/a/1", "/b/1"]));
	}

	private static ReadableKeysProvider CreateProvider(bool authEnabled, Dictionary<string, EtcdUser> users, Dictionary<string, EtcdRole> roles, Dictionary<string, IReadOnlyList<EtcdKeyValue>> byPrefix) => new(
		new StubKeyStore(byPrefix),
		new StubUserAdmin(users),
		new StubRoleAdmin(roles),
		new StubAuthAdmin(authEnabled));

	private sealed class StubKeyStore(Dictionary<string, IReadOnlyList<EtcdKeyValue>> byPrefix) : IEtcdKeyStore
	{
		public Task<EtcdKeyValue?> GetKeyAsync(string key, CancellationToken ct = default) => throw new NotSupportedException();

		public Task<IReadOnlyList<EtcdKeyValue>> GetKeysByPrefixAsync(string prefix, CancellationToken ct = default) =>
			Task.FromResult(byPrefix.TryGetValue(prefix, out var keys) ? keys : (IReadOnlyList<EtcdKeyValue>)[]);

		public Task<bool> CreateKeyAsync(string key, string value, CancellationToken ct = default) => throw new NotSupportedException();

		public Task<bool> UpdateKeyAsync(string key, string value, CancellationToken ct = default) => throw new NotSupportedException();

		public Task<bool> DeleteKeyAsync(string key, CancellationToken ct = default) => throw new NotSupportedException();
	}

	private sealed class StubUserAdmin(Dictionary<string, EtcdUser> users) : IEtcdUserAdmin
	{
		public Task<IReadOnlyList<EtcdUser>> GetUsersAsync(CancellationToken ct = default) => throw new NotSupportedException();

		public Task<EtcdUser?> GetUserAsync(string username, CancellationToken ct = default) =>
			Task.FromResult(users.TryGetValue(username, out var user) ? user : null);

		public Task<EtcdOperationResult> CreateUserAsync(string username, string password, CancellationToken ct = default) => throw new NotSupportedException();

		public Task<EtcdOperationResult> DeleteUserAsync(string username, CancellationToken ct = default) => throw new NotSupportedException();

		public Task<EtcdOperationResult> ChangeUserPasswordAsync(string username, string newPassword, CancellationToken ct = default) => throw new NotSupportedException();

		public Task<EtcdOperationResult> GrantRoleToUserAsync(string username, string roleName, CancellationToken ct = default) => throw new NotSupportedException();

		public Task<EtcdOperationResult> RevokeRoleFromUserAsync(string username, string roleName, CancellationToken ct = default) => throw new NotSupportedException();
	}

	private sealed class StubRoleAdmin(Dictionary<string, EtcdRole> roles) : IEtcdRoleAdmin
	{
		public Task<IReadOnlyList<EtcdRole>> GetRolesAsync(CancellationToken ct = default) => throw new NotSupportedException();

		public Task<EtcdRole?> GetRoleAsync(string roleName, CancellationToken ct = default) =>
			Task.FromResult(roles.TryGetValue(roleName, out var role) ? role : null);

		public Task<EtcdOperationResult> CreateRoleAsync(string roleName, CancellationToken ct = default) => throw new NotSupportedException();

		public Task<EtcdOperationResult> DeleteRoleAsync(string roleName, CancellationToken ct = default) => throw new NotSupportedException();

		public Task<EtcdOperationResult> GrantPermissionAsync(string roleName, PermissionType permissionType, string keyPrefix, CancellationToken ct = default) => throw new NotSupportedException();

		public Task<EtcdOperationResult> RevokePermissionAsync(string roleName, PermissionType permissionType, string keyPrefix, CancellationToken ct = default) => throw new NotSupportedException();
	}

	private sealed class StubAuthAdmin(bool enabled) : IEtcdAuthAdmin
	{
		public Task<bool> IsAuthenticationEnabledAsync(CancellationToken ct = default) => Task.FromResult(enabled);

		public Task<EtcdOperationResult> EnableAuthenticationAsync(CancellationToken ct = default) => throw new NotSupportedException();

		public Task<EtcdOperationResult> DisableAuthenticationAsync(CancellationToken ct = default) => throw new NotSupportedException();
	}
}
