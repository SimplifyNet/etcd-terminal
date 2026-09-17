using EtcdTerminal.Configuration;
using EtcdTerminal.Security;

namespace EtcdTerminal.Session;

public interface IConnectionSession
{
	EtcdConnectionConfig? Active { get; }
	UserCapabilities Capabilities { get; }
	void Start(EtcdConnectionConfig config, UserCapabilities capabilities);
	void End();
}
