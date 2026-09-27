using System.Text;
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
		var (provider, store) = Create(
			[new EtcdKeyValue { Key = "/a/1", Value = "x" }, new EtcdKeyValue { Key = "/b/1", Value = "y" }, new EtcdKeyValue { Key = "plain", Value = "z" }],
			[],
			allowAll: true);

		var keys = await provider.GetReadableKeysAsync(UserCapabilities.Unrestricted);

		Assert.That(keys.Select(kv => kv.Key), Is.EquivalentTo(["/a/1", "/b/1", "plain"]));
		Assert.That(store.Queries, Is.EqualTo(["prefix:"]));
	}

	[Test]
	public async Task ReadPermission_ReturnsOnlyPermittedKeys()
	{
		var (provider, store) = Create(
			[new EtcdKeyValue { Key = "/a/1", Value = "x" }, new EtcdKeyValue { Key = "/a/2", Value = "y" }, new EtcdKeyValue { Key = "/ab", Value = "z" }, new EtcdKeyValue { Key = "/b/1", Value = "w" }],
			[ReadPrefix("/a")]);

		var capabilities = new UserCapabilities { Permissions = [ReadPrefix("/a")] };

		var keys = await provider.GetReadableKeysAsync(capabilities);

		Assert.That(keys.Select(kv => kv.Key), Is.EquivalentTo(["/a/1", "/a/2", "/ab"]));
		Assert.That(store.Queries, Is.EqualTo(["prefix:/a"]));
	}

	[Test]
	public async Task ExactPermission_UsesExactLookup()
	{
		var (provider, store) = Create(
			[new EtcdKeyValue { Key = "/a", Value = "x" }, new EtcdKeyValue { Key = "/ab", Value = "y" }],
			[ReadExact("/a")]);

		var capabilities = new UserCapabilities { Permissions = [ReadExact("/a")] };

		var keys = await provider.GetReadableKeysAsync(capabilities);

		Assert.That(keys.Select(kv => kv.Key), Is.EqualTo(["/a"]));
		Assert.That(store.Queries, Is.EqualTo(["exact:/a"]));
	}

	[Test]
	public async Task BoundedRangePermission_UsesRangeLookup()
	{
		var (provider, store) = Create(
			[new EtcdKeyValue { Key = "a", Value = "x" }, new EtcdKeyValue { Key = "b", Value = "y" }, new EtcdKeyValue { Key = "c", Value = "z" }],
			[ReadRange("a", "c")]);

		var capabilities = new UserCapabilities { Permissions = [ReadRange("a", "c")] };

		var keys = await provider.GetReadableKeysAsync(capabilities);

		Assert.That(keys.Select(kv => kv.Key), Is.EquivalentTo(["a", "b"]));
		Assert.That(store.Queries, Is.EqualTo(["range:a-c"]));
	}

	[Test]
	public async Task OpenEndedRangePermission_ReadsTail()
	{
		var (provider, store) = Create(
			[new EtcdKeyValue { Key = "a", Value = "x" }, new EtcdKeyValue { Key = "m", Value = "y" }, new EtcdKeyValue { Key = "z", Value = "z" }],
			[ReadRange("m", "\0")]);

		var capabilities = new UserCapabilities { Permissions = [ReadRange("m", "\0")] };

		var keys = await provider.GetReadableKeysAsync(capabilities);

		Assert.That(keys.Select(kv => kv.Key), Is.EquivalentTo(["m", "z"]));
		Assert.That(store.Queries, Is.EqualTo(["range:m-\0"]));
	}

	[Test]
	public async Task OverlappingPermissions_Deduplicated()
	{
		var (provider, _) = Create(
			[new EtcdKeyValue { Key = "/a/1", Value = "x" }, new EtcdKeyValue { Key = "/a/2", Value = "y" }, new EtcdKeyValue { Key = "/ab", Value = "z" }],
			[ReadPrefix("/a"), ReadExact("/a/1")]);

		var capabilities = new UserCapabilities { Permissions = [ReadPrefix("/a"), ReadExact("/a/1")] };

		var keys = await provider.GetReadableKeysAsync(capabilities);

		Assert.That(keys.Select(kv => kv.Key), Is.EquivalentTo(["/a/1", "/a/2", "/ab"]));
	}

	[Test]
	public async Task WriteOnlyPermission_MakesNoQueries()
	{
		var (provider, store) = Create(
			[new EtcdKeyValue { Key = "/a/1", Value = "x" }],
			[]);

		var capabilities = new UserCapabilities { Permissions = [new EtcdPermission { Type = PermissionType.Write, KeyPrefix = "/a", RangeEnd = PermissionRange.PrefixRangeEnd("/a") }] };

		var keys = await provider.GetReadableKeysAsync(capabilities);

		Assert.That(keys, Is.Empty);
		Assert.That(store.Queries, Is.Empty);
	}

	[Test]
	public async Task NullPrefixPermission_ReturnsAllKeys()
	{
		var (provider, store) = Create(
			[new EtcdKeyValue { Key = "/a/1", Value = "x" }, new EtcdKeyValue { Key = "plain", Value = "y" }],
			[ReadPrefix("\0")]);

		var capabilities = new UserCapabilities { Permissions = [ReadPrefix("\0")] };

		var keys = await provider.GetReadableKeysAsync(capabilities);

		Assert.That(keys.Select(kv => kv.Key), Is.EquivalentTo(["/a/1", "plain"]));
		Assert.That(store.Queries, Is.EqualTo(["prefix:"]));
	}

	[Test]
	public async Task NoPermissions_ReturnsNothingWithoutQueries()
	{
		var (provider, store) = Create(
			[new EtcdKeyValue { Key = "/a/1", Value = "x" }],
			[]);

		var keys = await provider.GetReadableKeysAsync(new());

		Assert.That(keys, Is.Empty);
		Assert.That(store.Queries, Is.Empty);
	}

	private static EtcdPermission ReadPrefix(string prefix) =>
		new() { Type = PermissionType.Read, KeyPrefix = prefix, RangeEnd = PermissionRange.PrefixRangeEnd(prefix) };

	private static EtcdPermission ReadExact(string key) =>
		new() { Type = PermissionType.Read, KeyPrefix = key, RangeEnd = string.Empty };

	private static EtcdPermission ReadRange(string start, string endExclusive) =>
		new() { Type = PermissionType.Read, KeyPrefix = start, RangeEnd = endExclusive };

	private static (ReadableKeysProvider Provider, EnforcingKeyStore Store) Create(
		IReadOnlyList<EtcdKeyValue> keys, IReadOnlyList<EtcdPermission> readable, bool allowAll = false)
	{
		var store = new EnforcingKeyStore(keys, readable, allowAll);

		return (new ReadableKeysProvider(store), store);
	}

	/// <summary>
	/// Test server that enforces read permissions like etcd: any query reaching
	/// beyond the granted byte ranges fails instead of returning filtered data.
	/// </summary>
	private sealed class EnforcingKeyStore(IReadOnlyList<EtcdKeyValue> keys, IReadOnlyList<EtcdPermission> readable, bool allowAll) : IEtcdKeyStore
	{
		public List<string> Queries { get; } = [];

		public Task<EtcdKeyValue?> GetKeyAsync(string key, CancellationToken ct = default)
		{
			Queries.Add("exact:" + key);

			var encoded = Bytes(key);

			if (!allowAll && !GrantedRanges().Any(g => IsCovered(encoded, g)))
				throw Denied();

			return Task.FromResult(keys.FirstOrDefault(kv => kv.Key == key));
		}

		public Task<IReadOnlyList<EtcdKeyValue>> GetKeysByPrefixAsync(string prefix, CancellationToken ct = default)
		{
			Queries.Add("prefix:" + prefix);

			if (allowAll && prefix.Length == 0)
				return Task.FromResult(keys);

			var start = prefix.Length == 0 ? new byte[] { 0 } : Bytes(prefix);
			var end = prefix.Length == 0 ? new byte[] { 0 } : Successor(Bytes(prefix));

			if (!allowAll && !GrantedRanges().Any(g => IsRangeCovered(start, end, g)))
				throw Denied();

			if (prefix.Length == 0)
				return Task.FromResult(keys);

			return Task.FromResult((IReadOnlyList<EtcdKeyValue>)[.. keys.Where(kv => IsInRange(Bytes(kv.Key), start, end))]);
		}

		public Task<IReadOnlyList<EtcdKeyValue>> GetKeysByRangeAsync(string start, string endExclusive, CancellationToken ct = default)
		{
			Queries.Add($"range:{start}-{endExclusive}");

			var encodedStart = Bytes(start);
			var encodedEnd = endExclusive == "\0" ? null : Bytes(endExclusive);

			if (encodedEnd is not null && encodedEnd.Length == 0)
				throw new ArgumentException("A range read requires an explicit exclusive end.", nameof(endExclusive));

			if (!allowAll && !GrantedRanges().Any(g => IsRangeCovered(encodedStart, encodedEnd, g)))
				throw Denied();

			return Task.FromResult((IReadOnlyList<EtcdKeyValue>)[.. keys.Where(kv => IsInRange(Bytes(kv.Key), encodedStart, encodedEnd))]);
		}

		public Task<bool> CreateKeyAsync(string key, string value, CancellationToken ct = default) => throw new NotSupportedException();

		public Task<bool> UpdateKeyAsync(string key, string value, CancellationToken ct = default) => throw new NotSupportedException();

		public Task<bool> DeleteKeyAsync(string key, CancellationToken ct = default) => throw new NotSupportedException();

		private IEnumerable<(byte[] Start, byte[]? End)> GrantedRanges()
		{
			foreach (var permission in readable)
			{
				if (permission.Type is not (PermissionType.Read or PermissionType.ReadWrite))
					continue;

				if (permission.Scope is PermissionScope.Key)
				{
					var exact = Bytes(permission.KeyPrefix);

					yield return (exact, [.. exact, (byte)0]);
				}
				else if (permission.Scope is PermissionScope.Prefix && (permission.KeyPrefix.Length == 0 || permission.KeyPrefix == "\0"))
				{
					yield return ([0], null);
				}
				else if (permission.Scope is PermissionScope.Prefix)
				{
					var start = Bytes(permission.KeyPrefix);

					yield return (start, Successor(start));
				}
				else
				{
					var start = permission.KeyPrefix == "\0" ? new byte[] { 0 } : Bytes(permission.KeyPrefix);

					yield return (start, permission.RangeEnd == "\0" ? null : Bytes(permission.RangeEnd));
				}
			}
		}

		private static bool IsCovered(byte[] key, (byte[] Start, byte[]? End) granted) =>
			IsInRange(key, granted.Start, granted.End);

		private static bool IsRangeCovered(byte[] start, byte[]? end, (byte[] Start, byte[]? End) granted) =>
			Compare(granted.Start, start) <= 0 && (granted.End is null || (end is not null && Compare(end, granted.End) <= 0));

		private static bool IsInRange(byte[] key, byte[] start, byte[]? end) =>
			Compare(start, key) <= 0 && (end is null || Compare(key, end) < 0);

		private static int Compare(byte[] left, byte[] right) =>
			left.AsSpan().SequenceCompareTo(right);

		private static byte[] Bytes(string value) => Encoding.UTF8.GetBytes(value);

		private static byte[] Successor(byte[] key)
		{
			var end = (byte[])key.Clone();

			for (var i = end.Length - 1; i >= 0; i--)
			{
				if (end[i] < 0xFF)
				{
					end[i]++;

					return end[..(i + 1)];
				}
			}

			return [0];
		}

		private static EtcdOperationException Denied() =>
			new(EtcdOperationFailureKind.AccessDenied, "Permission denied by test server.");
	}
}
