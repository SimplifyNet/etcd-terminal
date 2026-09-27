namespace EtcdTerminal.Keys;

public sealed class KeyImporter(IEtcdKeyStore _keyStore) : IKeyImporter
{
	public async Task<KeyImportResult> ImportAsync(IReadOnlyList<KeyValuePair<string, string>> entries, Action<KeyImportResult>? progress, CancellationToken ct)
	{
		var created = 0;
		var overwritten = 0;
		var failed = 0;

		foreach (var (Key, Value) in entries)
		{
			ct.ThrowIfCancellationRequested();

			var existing = await _keyStore.GetKeyAsync(Key, ct);

			if (existing is not null)
			{
				if (await _keyStore.UpdateKeyAsync(Key, Value, ct))
					overwritten++;
				else
					failed++;
			}
			else if (await _keyStore.CreateKeyAsync(Key, Value, ct))
				created++;
			else
				failed++;

			progress?.Invoke(new(created, overwritten, failed));
		}

		return new(created, overwritten, failed);
	}
}
