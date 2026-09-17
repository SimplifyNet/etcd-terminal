using EtcdTerminal.Permissions;
using EtcdTerminal.Roles;
using EtcdTerminal.Security;
using EtcdTerminal.Users;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class UserCapabilitiesProviderTests
{
	[Test]
	public async Task AuthDisabled_IsUnrestricted()
	{
		var provider = CreateProvider(authEnabled: false, users: new(), roles: new());

		var capabilities = await provider.GetCapabilitiesAsync("bob");

		Assert.Multiple(() =>
		{
			Assert.That(capabilities.IsRoot, Is.True);
			Assert.That(capabilities.CanManageAuth, Is.True);
		});
	}

	[Test]
	public async Task RootUser_IsUnrestricted()
	{
		var provider = CreateProvider(authEnabled: true,
			users: new() { ["root-user"] = new EtcdUser { Username = "root-user", Roles = ["root"] } },
			roles: new());

		var capabilities = await provider.GetCapabilitiesAsync("root-user");

		Assert.That(capabilities.IsRoot, Is.True);
	}

	[Test]
	public async Task NonRootUser_CollectsRolePermissions()
	{
		var provider = CreateProvider(authEnabled: true,
			users: new() { ["dev"] = new EtcdUser { Username = "dev", Roles = ["dev-role"] } },
			roles: new()
			{
				["dev-role"] = new EtcdRole
				{
					Name = "dev-role",
					Permissions = [new EtcdPermission { Type = PermissionType.ReadWrite, KeyPrefix = "/a" }]
				}
			});

		var capabilities = await provider.GetCapabilitiesAsync("dev");

		Assert.Multiple(() =>
		{
			Assert.That(capabilities.IsRoot, Is.False);
			Assert.That(capabilities.CanManageAuth, Is.False);
			Assert.That(capabilities.CanReadKeys, Is.True);
			Assert.That(capabilities.CanWriteKeys, Is.True);
			Assert.That(capabilities.CanWriteKey("/a/1"), Is.True);
			Assert.That(capabilities.CanWriteKey("/b/1"), Is.False);
		});
	}

	[Test]
	public async Task ReadOnlyRole_CannotWrite()
	{
		var provider = CreateProvider(authEnabled: true,
			users: new() { ["viewer"] = new EtcdUser { Username = "viewer", Roles = ["viewer-role"] } },
			roles: new()
			{
				["viewer-role"] = new EtcdRole
				{
					Name = "viewer-role",
					Permissions = [new EtcdPermission { Type = PermissionType.Read, KeyPrefix = "/a" }]
				}
			});

		var capabilities = await provider.GetCapabilitiesAsync("viewer");

		Assert.Multiple(() =>
		{
			Assert.That(capabilities.CanReadKeys, Is.True);
			Assert.That(capabilities.CanWriteKeys, Is.False);
			Assert.That(capabilities.CanReadKey("/a/1"), Is.True);
		});
	}

	[Test]
	public async Task UnknownUser_HasNoCapabilities()
	{
		var provider = CreateProvider(authEnabled: true, users: new(), roles: new());

		var capabilities = await provider.GetCapabilitiesAsync("ghost");

		Assert.Multiple(() =>
		{
			Assert.That(capabilities.IsRoot, Is.False);
			Assert.That(capabilities.CanReadKeys, Is.False);
			Assert.That(capabilities.CanWriteKeys, Is.False);
		});
	}

	private static UserCapabilitiesProvider CreateProvider(bool authEnabled, Dictionary<string, EtcdUser> users, Dictionary<string, EtcdRole> roles) => new(
		new StubUserAdmin(users),
		new StubRoleAdmin(roles),
		new StubAuthAdmin(authEnabled));

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
