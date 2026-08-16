namespace EtcdTerminal.Environment;

public interface IAppEnvironment
{
	string ConfigDirectoryPath { get; }
	string ConfigFilePath { get; }
	string KeyFilePath { get; }
}
