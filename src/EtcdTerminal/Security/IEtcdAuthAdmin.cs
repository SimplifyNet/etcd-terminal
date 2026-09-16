namespace EtcdTerminal.Security;

public interface IEtcdAuthAdmin
{
	Task<bool> IsAuthenticationEnabledAsync(CancellationToken ct = default);
	Task<bool> EnableAuthenticationAsync(CancellationToken ct = default);
	Task<bool> DisableAuthenticationAsync(CancellationToken ct = default);
}
