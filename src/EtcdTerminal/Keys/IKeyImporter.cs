namespace EtcdTerminal.Keys;

public interface IKeyImporter
{
	Task<KeyImportResult> ImportAsync(IReadOnlyList<KeyValuePair<string, string>> entries, CancellationToken ct);
}
