namespace EtcdTerminal.Configuration;

public interface IConnectionConfigRepository
{
	IReadOnlyList<EtcdConnectionConfig> LoadInstances();
	IReadOnlyList<string> TakeDecryptFailures();
	void AddInstance(EtcdConnectionConfig config);
	void UpdateInstance(string originalName, EtcdConnectionConfig config);
	void RemoveInstance(string name);
	void MoveUp(string name);
	void MoveDown(string name);
}