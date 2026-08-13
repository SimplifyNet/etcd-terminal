namespace EtcdTerminal.Configuration;

public interface IAppSettingsRepository
{
	void Load();
	void Save();
}