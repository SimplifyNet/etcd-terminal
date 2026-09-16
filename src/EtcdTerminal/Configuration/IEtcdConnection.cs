namespace EtcdTerminal.Configuration;

public interface IEtcdConnection
{
	bool IsConnected { get; }

	Task ConnectAsync(EtcdConnectionConfig config, CancellationToken ct = default);
	Task<bool> PingAsync(CancellationToken ct = default);
	Task DisconnectAsync();
}
