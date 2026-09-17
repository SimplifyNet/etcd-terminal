using EtcdTerminal.Configuration;
using EtcdTerminal.Security;

namespace EtcdTerminal.Session;

public sealed class ConnectionSession : IConnectionSession
{
	public EtcdConnectionConfig? Active { get; private set; }

	public UserCapabilities Capabilities { get; private set; } = new();

	public void Start(EtcdConnectionConfig config, UserCapabilities capabilities)
	{
		Active = config;
		Capabilities = capabilities;
	}

	public void End()
	{
		Active = null;
		Capabilities = new();
	}
}
