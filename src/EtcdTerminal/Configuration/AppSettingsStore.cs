namespace EtcdTerminal.Configuration;

public sealed class AppSettingsStore(IAppSettingsRepository _repository) : IAppSettingsStore
{
	public AppSettings Current { get; private set; } = new();

	public void Reload() => Current = _repository.Load();

	public void Save(AppSettings settings)
	{
		_repository.Save(settings);

		Current = settings;
	}
}
