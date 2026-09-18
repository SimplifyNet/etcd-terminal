namespace EtcdTerminal.Configuration;

public interface IEtcdConnection
{
	Task ConnectAsync(EtcdConnectionConfig config, CancellationToken ct = default);
	Task DisconnectAsync();
}
