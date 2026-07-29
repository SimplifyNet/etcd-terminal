namespace EtcdTerminal;

public interface IAppEnvironment
{
	string ConfigDirectoryPath { get; }
	string ConfigFilePath { get; }
	string KeyFilePath { get; }
}
