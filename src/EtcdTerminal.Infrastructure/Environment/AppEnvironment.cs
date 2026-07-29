namespace EtcdTerminal.Infrastructure.Environment;

public sealed class AppEnvironment : IAppEnvironment
{
	private const string ConfigDir = ".config/etcd-terminal";
	private const string ConfigFile = "config.json";
	private const string KeyFile = ".key";

	public string ConfigDirectoryPath { get; }
	public string ConfigFilePath { get; }
	public string KeyFilePath { get; }

	public AppEnvironment()
	{
		var home = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);

		ConfigDirectoryPath = Path.Combine(home, ConfigDir);
		ConfigFilePath = Path.Combine(ConfigDirectoryPath, ConfigFile);
		KeyFilePath = Path.Combine(ConfigDirectoryPath, KeyFile);
	}
}
