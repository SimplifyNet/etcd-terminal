using EtcdTerminal.Keys;
using EtcdTerminal.Permissions;
using EtcdTerminal.Security;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class ReadableKeysProviderTests
{
	[Test]
	public async Task RootCapabilities_ReturnsAllKeys()
	{
		var provider = CreateProvider(new()
		{
			[""] = [new EtcdKeyValue { Key = "/a/1", Value = "x" }, new EtcdKeyValue { Key = "/b/1", Value = "y" }]
		});

		var keys = await provider.GetReadableKeysAsync(UserCapabilities.Unrestricted);

		Assert.That(keys.Select(kv => kv.Key), Is.EquivalentTo(["/a/1", "/b/1"]));
	}

	[Test]
	public async Task ReadPermission_ReturnsOnlyPermittedKeys()
	{
		var provider = CreateProvider(new()
		{
			[""] = [new EtcdKeyValue { Key = "/a/1", Value = "x" }, new EtcdKeyValue { Key = "/b/1", Value = "y" }],
			["/a"] = [new EtcdKeyValue { Key = "/a/1", Value = "x" }]
		});

		var capabilities = new UserCapabilities { Permissions = [new EtcdPermission { Type = PermissionType.Read, KeyPrefix = "/a", RangeEnd = PermissionRange.PrefixRangeEnd("/a") }] };

		var keys = await provider.GetReadableKeysAsync(capabilities);

		Assert.That(keys.Select(kv => kv.Key), Is.EquivalentTo(["/a/1"]));
	}

	[Test]
	public async Task WriteOnlyPermission_ReturnsNothing()
	{
		var provider = CreateProvider(new() { [""] = [new EtcdKeyValue { Key = "/a/1", Value = "x" }] });

		var capabilities = new UserCapabilities { Permissions = [new EtcdPermission { Type = PermissionType.Write, KeyPrefix = "/a", RangeEnd = PermissionRange.PrefixRangeEnd("/a") }] };

		var keys = await provider.GetReadableKeysAsync(capabilities);

		Assert.That(keys, Is.Empty);
	}

	[Test]
	public async Task NullPrefixPermission_ReturnsAllKeys()
	{
		var provider = CreateProvider(new()
		{
			[""] = [new EtcdKeyValue { Key = "/a/1", Value = "x" }, new EtcdKeyValue { Key = "/b/1", Value = "y" }]
		});

		var capabilities = new UserCapabilities { Permissions = [new EtcdPermission { Type = PermissionType.ReadWrite, KeyPrefix = "\0", RangeEnd = "\0" }] };

		var keys = await provider.GetReadableKeysAsync(capabilities);

		Assert.That(keys.Select(kv => kv.Key), Is.EquivalentTo(["/a/1", "/b/1"]));
	}

	[Test]
	public async Task NoPermissions_ReturnsNothing()
	{
		var provider = CreateProvider(new() { [""] = [new EtcdKeyValue { Key = "/a/1", Value = "x" }] });

		var keys = await provider.GetReadableKeysAsync(new());

		Assert.That(keys, Is.Empty);
	}

	private static ReadableKeysProvider CreateProvider(Dictionary<string, IReadOnlyList<EtcdKeyValue>> byPrefix) =>
		new(new StubKeyStore(byPrefix));

	private sealed class StubKeyStore(Dictionary<string, IReadOnlyList<EtcdKeyValue>> byPrefix) : IEtcdKeyStore
	{
		public Task<EtcdKeyValue?> GetKeyAsync(string key, CancellationToken ct = default) => throw new NotSupportedException();

		public Task<IReadOnlyList<EtcdKeyValue>> GetKeysByPrefixAsync(string prefix, CancellationToken ct = default) =>
			Task.FromResult(byPrefix.TryGetValue(prefix, out var keys) ? keys : (IReadOnlyList<EtcdKeyValue>)[]);

		public Task<bool> CreateKeyAsync(string key, string value, CancellationToken ct = default) => throw new NotSupportedException();

		public Task<bool> UpdateKeyAsync(string key, string value, CancellationToken ct = default) => throw new NotSupportedException();

		public Task<bool> DeleteKeyAsync(string key, CancellationToken ct = default) => throw new NotSupportedException();
	}
}
