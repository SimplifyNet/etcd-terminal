using EtcdTerminal.Configuration;
using EtcdTerminal.Environment;
using EtcdTerminal.Infrastructure;
using EtcdTerminal.Infrastructure.Configuration;
using EtcdTerminal.Security;
using EtcdTerminal.Session;
using Simplify.DI;

namespace EtcdTerminal.App.Setup;

public static class ConfigurationRegistrations
{
	public static IDIRegistrator RegisterConfiguration(this IDIRegistrator registrator) => registrator
		.Register<JsonConfigFile>(c => new(c.Resolve<IAppEnvironment>()), LifetimeType.Singleton)

		.Register(c =>
			new ProtectedConfigRepository(
				new JsonBasedConnectionConfigRepository(c.Resolve<JsonConfigFile>()),
				c.Resolve<IConfigProtector>()),
			LifetimeType.Singleton)

		.Register<IConnectionConfigRepository>(c => c.Resolve<ProtectedConfigRepository>(), LifetimeType.Singleton)
		.Register<IDecryptFailureSource>(c => c.Resolve<ProtectedConfigRepository>(), LifetimeType.Singleton)

		.Register<IAppSettingsRepository, JsonBasedSettingsRepository>(LifetimeType.Singleton)
		.Register<IAppSettingsStore, AppSettingsStore>(LifetimeType.Singleton);

	public static IDIRegistrator RegisterSession(this IDIRegistrator registrator) => registrator
		.Register<IConnectionSession, ConnectionSession>(LifetimeType.Singleton)
		.Register<IConnectionWorkflow, ConnectionWorkflow>(LifetimeType.Transient);

	public static IDIRegistrator RegisterClient(this IDIRegistrator registrator) => registrator
		.Register(c => new EtcdConnectionHandle(DotnetEtcdTransportFactory.Create), LifetimeType.Singleton)
		.Register<IEtcdConnection>(c => c.Resolve<EtcdConnectionHandle>(), LifetimeType.Singleton);
}
