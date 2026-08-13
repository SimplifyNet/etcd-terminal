namespace EtcdTerminal.Infrastructure.Environment;

public sealed class AppEnvironment : IAppEnvironment
{
	private const string ConfigDir = ".config/etcd-terminal";
	private const string ConfigFile = "config.json";
	private const string SettingsFile = "settings.json";
	private const string KeyFile = ".key";

	public AppEnvironment()
	{
		var home = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);

		ConfigDirectoryPath = Path.Combine(home, ConfigDir);
		ConfigFilePath = Path.Combine(ConfigDirectoryPath, ConfigFile);
		SettingsFilePath = Path.Combine(ConfigDirectoryPath, SettingsFile);
		KeyFilePath = Path.Combine(ConfigDirectoryPath, KeyFile);
	}

	public string ConfigDirectoryPath { get; }
	public string ConfigFilePath { get; }
	public string SettingsFilePath { get; }
	public string KeyFilePath { get; }
}
