namespace EtcdTerminal.Configuration;

public interface IAppSettingsRepository
{
	AppSettings Load();
	void Save(AppSettings settings);
}
