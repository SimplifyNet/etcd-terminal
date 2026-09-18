namespace EtcdTerminal.Security;

public interface IEtcdAuthAdmin
{
	Task<bool> IsAuthenticationEnabledAsync(CancellationToken ct = default);
}
