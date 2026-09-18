namespace EtcdTerminal.Configuration;

public sealed class AppSettingsStore : IAppSettingsStore
{
	private IAppSettings _current = new AppSettings();

	public IAppSettings Current => _current;

	public void Update(IAppSettings settings) => _current = settings;
}
