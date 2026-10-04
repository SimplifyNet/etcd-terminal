namespace EtcdTerminal.Configuration;

public interface IAppSettingsStore
{
	AppSettings Current { get; }

	void Reload();

	void Save(AppSettings settings);
}
