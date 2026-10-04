using EtcdTerminal.Configuration;

namespace EtcdTerminal.Tests.Fakes;

public sealed class FakeSettingsRepository : IAppSettingsRepository
{
	public AppSettings Load() => new();

	public void Save(AppSettings settings)
	{
	}
}
