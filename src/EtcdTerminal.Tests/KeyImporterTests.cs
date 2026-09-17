using EtcdTerminal.Keys;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class KeyImporterTests
{
	[Test]
	public async Task ImportAsync_CountsCreatedOverwrittenFailed()
	{
		var store = new StubKeyStore(existing: new() { ["/keep"] = "old", ["/stuck"] = "old" }, failingUpdates: ["/stuck"], failingCreates: ["/broken"]);
		var importer = new KeyImporter(store);

		IReadOnlyList<KeyValuePair<string, string>> entries =
		[
			new("/keep", "new"),
			new("/stuck", "new"),
			new("/fresh", "v"),
			new("/broken", "v")
		];

		var result = await importer.ImportAsync(entries, CancellationToken.None);

		Assert.Multiple(() =>
		{
			Assert.That(result.Created, Is.EqualTo(1));
			Assert.That(result.Overwritten, Is.EqualTo(1));
			Assert.That(result.Failed, Is.EqualTo(2));
		});
	}

	private sealed class StubKeyStore(Dictionary<string, string> existing, HashSet<string> failingUpdates, HashSet<string> failingCreates) : IEtcdKeyStore
	{
		public Task<EtcdKeyValue?> GetKeyAsync(string key, CancellationToken ct = default) =>
			Task.FromResult(existing.TryGetValue(key, out var value) ? new EtcdKeyValue { Key = key, Value = value } : null);

		public Task<IReadOnlyList<EtcdKeyValue>> GetKeysByPrefixAsync(string prefix, CancellationToken ct = default) => throw new NotSupportedException();

		public Task<bool> CreateKeyAsync(string key, string value, CancellationToken ct = default) =>
			Task.FromResult(!failingCreates.Contains(key));

		public Task<bool> UpdateKeyAsync(string key, string value, CancellationToken ct = default) =>
			Task.FromResult(!failingUpdates.Contains(key));

		public Task<bool> DeleteKeyAsync(string key, CancellationToken ct = default) => throw new NotSupportedException();
	}
}
