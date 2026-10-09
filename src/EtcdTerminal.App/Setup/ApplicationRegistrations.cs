using EtcdTerminal.Environment;
using EtcdTerminal.Infrastructure.Environment;
using Simplify.DI;

namespace EtcdTerminal.App.Setup;

public static class ApplicationRegistrations
{
	public static IDIRegistrator RegisterEnvironment(this IDIRegistrator registrator) => registrator
		.Register<IAppEnvironment, AppEnvironment>(LifetimeType.Singleton)
		.Register<IAppInfo, AppInfo>(LifetimeType.Singleton);

	public static IDIRegistrator RegisterApplication(this IDIRegistrator registrator) => registrator
		.Register<AppRunner>(LifetimeType.Singleton)
		.Register<PreferencesLoader>(LifetimeType.Transient);
}
