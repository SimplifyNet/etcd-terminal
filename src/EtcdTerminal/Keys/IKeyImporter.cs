namespace EtcdTerminal.Keys;

public interface IKeyImporter
{
	/// <summary>
	/// Imports entries sequentially and stops on the first operational failure.
	/// <paramref name="progress"/> receives a snapshot after every confirmed
	/// outcome; entries after an interruption are never attempted. Failures and
	/// cancellations propagate: the returned result only covers confirmed outcomes.
	/// </summary>
	Task<KeyImportResult> ImportAsync(IReadOnlyList<KeyValuePair<string, string>> entries, Action<KeyImportResult>? progress, CancellationToken ct);
}
