namespace EtcdTerminal.Security;

public interface IUserCapabilitiesProvider
{
	Task<UserCapabilities> GetCapabilitiesAsync(string? username, CancellationToken ct = default);
}
