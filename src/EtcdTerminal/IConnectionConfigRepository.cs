using EtcdTerminal.Models;

namespace EtcdTerminal;

public interface IConnectionConfigRepository
{
	IReadOnlyList<EtcdConnectionConfig> LoadInstances();
	void AddInstance(EtcdConnectionConfig config);
	void RemoveInstance(string name);
	void MoveUp(string name);
	void MoveDown(string name);
}
