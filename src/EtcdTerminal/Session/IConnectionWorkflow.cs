using EtcdTerminal.Configuration;

namespace EtcdTerminal.Session;

public interface IConnectionWorkflow
{
	Task ConnectAsync(EtcdConnectionConfig config, CancellationToken ct);
	Task DisconnectAsync();
}
