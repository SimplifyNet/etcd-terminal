using EtcdTerminal.Configuration;

namespace EtcdTerminal.Session;

public sealed class ConnectionSession : IConnectionSession
{
	public EtcdConnectionConfig? Active { get; private set; }

	public void Start(EtcdConnectionConfig config) => Active = config;

	public void End() => Active = null;
}
