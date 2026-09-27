using EtcdTerminal.Keys;
using Grpc.Core;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class KeyImporterTests
{
	[Test]
	public async Task ImportAsync_CountsCreatedOverwrittenFailed()
	{
		var store = new StubKeyStore(new() { ["/keep"] = "old", ["/stuck"] = "old" }, failingUpdates: ["/stuck"], failingCreates: ["/broken"]);
		var importer = new KeyImporter(store);

		IReadOnlyList<KeyValuePair<string, string>> entries =
		[
			new("/keep", "new"),
			new("/stuck", "new"),
			new("/fresh", "v"),
			new("/broken", "v")
		];

		var result = await importer.ImportAsync(entries, progress: null, CancellationToken.None);

		Assert.Multiple(() =>
		{
			Assert.That(result.Created, Is.EqualTo(1));
			Assert.That(result.Overwritten, Is.EqualTo(1));
			Assert.That(result.Failed, Is.EqualTo(2));
		});
	}

	[Test]
	public async Task ImportAsync_ReportsMonotonicSnapshots()
	{
		var store = new StubKeyStore(new() { ["/keep"] = "old" });
		var importer = new KeyImporter(store);
		var snapshots = new List<KeyImportResult>();

		IReadOnlyList<KeyValuePair<string, string>> entries = [new("/keep", "new"), new("/fresh", "v")];

		var result = await importer.ImportAsync(entries, snapshots.Add, CancellationToken.None);

		Assert.That(snapshots, Is.EqualTo(new[] { new KeyImportResult(0, 1, 0), new KeyImportResult(1, 1, 0) }));
		Assert.That(result, Is.EqualTo(new KeyImportResult(1, 1, 0)));
	}

	[Test]
	public void ImportAsync_DeniedWrite_StopsAndRetainsConfirmedProgress()
	{
		var store = new StubKeyStore(new(), errors: new() { ["/b"] = Denied() });
		var importer = new KeyImporter(store);
		var snapshots = new List<KeyImportResult>();

		IReadOnlyList<KeyValuePair<string, string>> entries = [new("/a", "v"), new("/b", "v"), new("/c", "v")];

		var ex = Assert.ThrowsAsync<EtcdOperationException>(() => importer.ImportAsync(entries, snapshots.Add, CancellationToken.None));

		Assert.That(ex!.Kind, Is.EqualTo(EtcdOperationFailureKind.AccessDenied));
		Assert.That(snapshots, Is.EqualTo(new[] { new KeyImportResult(1, 0, 0) }));
		Assert.That(store.Attempted, Does.Not.Contain("/c"));
	}

	[Test]
	public void ImportAsync_TransportFailure_StopsAndRetainsConfirmedProgress()
	{
		var store = new StubKeyStore(new() { ["/a"] = "old" }, errors: new() { ["/b"] = Unavailable() });
		var importer = new KeyImporter(store);
		var snapshots = new List<KeyImportResult>();

		IReadOnlyList<KeyValuePair<string, string>> entries = [new("/a", "v"), new("/b", "v"), new("/c", "v")];

		var ex = Assert.ThrowsAsync<EtcdOperationException>(() => importer.ImportAsync(entries, snapshots.Add, CancellationToken.None));

		Assert.That(ex!.Kind, Is.EqualTo(EtcdOperationFailureKind.Unavailable));
		Assert.That(snapshots, Is.EqualTo(new[] { new KeyImportResult(0, 1, 0) }));
		Assert.That(store.Attempted, Does.Not.Contain("/c"));
	}

	[Test]
	public void ImportAsync_UnconfirmedWrite_StopsWithoutCountingIt()
	{
		var store = new StubKeyStore(new(), errors: new() { ["/b"] = Unconfirmed() });
		var importer = new KeyImporter(store);
		var snapshots = new List<KeyImportResult>();

		IReadOnlyList<KeyValuePair<string, string>> entries = [new("/a", "v"), new("/b", "v"), new("/c", "v")];

		var ex = Assert.ThrowsAsync<EtcdOperationException>(() => importer.ImportAsync(entries, snapshots.Add, CancellationToken.None));

		Assert.That(ex!.Kind, Is.EqualTo(EtcdOperationFailureKind.Unconfirmed));
		Assert.That(snapshots, Is.EqualTo(new[] { new KeyImportResult(1, 0, 0) }));
		Assert.That(store.Attempted, Does.Not.Contain("/c"));
	}

	[Test]
	public void ImportAsync_CancelledInFlight_PropagatesWithoutCountingIt()
	{
		var store = new StubKeyStore(new(), errors: new() { ["/b"] = new OperationCanceledException() });
		var importer = new KeyImporter(store);
		var snapshots = new List<KeyImportResult>();

		IReadOnlyList<KeyValuePair<string, string>> entries = [new("/a", "v"), new("/b", "v"), new("/c", "v")];

		Assert.ThrowsAsync<OperationCanceledException>(() => importer.ImportAsync(entries, snapshots.Add, CancellationToken.None));
		Assert.That(snapshots, Is.EqualTo(new[] { new KeyImportResult(1, 0, 0) }));
		Assert.That(store.Attempted, Does.Not.Contain("/c"));
	}

	[Test]
	public void ImportAsync_PreCancelledToken_MakesNoAttempts()
	{
		var store = new StubKeyStore(new());
		var importer = new KeyImporter(store);
		var snapshots = new List<KeyImportResult>();
		using var cts = new CancellationTokenSource();

		cts.Cancel();

		IReadOnlyList<KeyValuePair<string, string>> entries = [new("/a", "v")];

		Assert.ThrowsAsync<OperationCanceledException>(() => importer.ImportAsync(entries, snapshots.Add, cts.Token));
		Assert.That(store.Attempted, Is.Empty);
		Assert.That(snapshots, Is.Empty);
	}

	private static EtcdOperationException Denied() =>
		new(EtcdOperationFailureKind.AccessDenied, "denied");

	private static EtcdOperationException Unavailable() =>
		new(EtcdOperationFailureKind.Unavailable, "unreachable");

	private static EtcdOperationException Unconfirmed() =>
		new(EtcdOperationFailureKind.Unconfirmed, "timed out");

	private sealed class StubKeyStore(
		Dictionary<string, string>? existing = null,
		HashSet<string>? failingUpdates = null,
		HashSet<string>? failingCreates = null,
		Dictionary<string, Exception>? errors = null) : IEtcdKeyStore
	{
		public HashSet<string> Attempted { get; } = [];

		public Task<EtcdKeyValue?> GetKeyAsync(string key, CancellationToken ct = default)
		{
			Attempted.Add(key);

			return Task.FromResult(existing?.TryGetValue(key, out var value) is true
				? new EtcdKeyValue { Key = key, Value = value }
				: null);
		}

		public Task<IReadOnlyList<EtcdKeyValue>> GetKeysByPrefixAsync(string prefix, CancellationToken ct = default) => throw new NotSupportedException();

		public Task<IReadOnlyList<EtcdKeyValue>> GetKeysByRangeAsync(string start, string endExclusive, CancellationToken ct = default) => throw new NotSupportedException();

		public Task<bool> CreateKeyAsync(string key, string value, CancellationToken ct = default)
		{
			Attempted.Add(key);

			if (errors?.TryGetValue(key, out var error) is true)
				return Task.FromException<bool>(error);

			return Task.FromResult(!(failingCreates?.Contains(key) is true));
		}

		public Task<bool> UpdateKeyAsync(string key, string value, CancellationToken ct = default)
		{
			Attempted.Add(key);

			if (errors?.TryGetValue(key, out var error) is true)
				return Task.FromException<bool>(error);

			return Task.FromResult(!(failingUpdates?.Contains(key) is true));
		}

		public Task<bool> DeleteKeyAsync(string key, CancellationToken ct = default) => throw new NotSupportedException();
	}
}
