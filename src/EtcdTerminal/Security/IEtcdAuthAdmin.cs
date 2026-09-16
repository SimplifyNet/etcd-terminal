namespace EtcdTerminal.Security;

public interface IEtcdAuthAdmin
{
	Task<bool> IsAuthenticationEnabledAsync(CancellationToken ct = default);
	Task<EtcdOperationResult> EnableAuthenticationAsync(CancellationToken ct = default);
	Task<EtcdOperationResult> DisableAuthenticationAsync(CancellationToken ct = default);
}
