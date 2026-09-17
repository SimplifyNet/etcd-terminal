namespace EtcdTerminal.Keys;

public sealed class KeyImporter(IEtcdKeyStore _keyStore) : IKeyImporter
{
	public async Task<KeyImportResult> ImportAsync(IReadOnlyList<KeyValuePair<string, string>> entries, CancellationToken ct)
	{
		var created = 0;
		var overwritten = 0;
		var failed = 0;

		foreach (var (Key, Value) in entries)
		{
			var existing = await _keyStore.GetKeyAsync(Key, ct);

			if (existing is not null)
			{
				var updated = await _keyStore.UpdateKeyAsync(Key, Value, ct);

				if (updated)
					overwritten++;
				else
					failed++;
			}
			else
			{
				var result = await _keyStore.CreateKeyAsync(Key, Value, ct);

				if (result)
					created++;
				else
					failed++;
			}
		}

		return new(created, overwritten, failed);
	}
}
