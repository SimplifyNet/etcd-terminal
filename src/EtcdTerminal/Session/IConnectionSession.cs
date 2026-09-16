using EtcdTerminal.Configuration;

namespace EtcdTerminal.Session;

public interface IConnectionSession
{
	EtcdConnectionConfig? Active { get; }
	void Start(EtcdConnectionConfig config);
	void End();
}
