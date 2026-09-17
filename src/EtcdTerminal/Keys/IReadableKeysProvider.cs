using EtcdTerminal.Security;

namespace EtcdTerminal.Keys;

public interface IReadableKeysProvider
{
	Task<IReadOnlyList<EtcdKeyValue>> GetReadableKeysAsync(UserCapabilities capabilities, CancellationToken ct = default);
}
