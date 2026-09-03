namespace EtcdTerminal.Configuration;

public interface IAppSettingsRepository
{
	IAppSettings Load();
	void Save(IAppSettings settings);
}