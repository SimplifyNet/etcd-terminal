namespace EtcdTerminal.Configuration;

public interface IAppSettingsStore
{
	IAppSettings Current { get; }

	void Update(IAppSettings settings);
}
