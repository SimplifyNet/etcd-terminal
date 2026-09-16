namespace EtcdTerminal.Keys;

public interface IReadableKeysProvider
{
	Task<IReadOnlyList<EtcdKeyValue>> GetReadableKeysAsync(string? username, CancellationToken ct = default);
}
