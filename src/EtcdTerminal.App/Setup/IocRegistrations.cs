using EtcdTerminal.App.Screens;
using EtcdTerminal.App.Screens.Keys;
using EtcdTerminal.App.Screens.Permissions;
using EtcdTerminal.App.Screens.Roles;
using EtcdTerminal.App.Screens.Users;
using EtcdTerminal.Security;
using EtcdTerminal.Configuration;
using EtcdTerminal.Infrastructure.Configuration;
using EtcdTerminal.Infrastructure.Environment;
using EtcdTerminal.Infrastructure.Etcd;
using EtcdTerminal.Infrastructure.Security;
using Simplify.DI;
using EtcdTerminal.Environment;

namespace EtcdTerminal.App.Setup;

public static class IocRegistrations
{
	public static IDIContainerProvider RegisterAll(this IDIContainerProvider provider)
	{
		provider.RegisterInfrastructure()
			   .RegisterConfiguration()
			   .RegisterIEtcdClient()
			   .RegisterScreens();

		return provider;
	}

	public static IDIRegistrator RegisterInfrastructure(this IDIRegistrator registrator) => registrator
		.Register<IAppEnvironment, AppEnvironment>(LifetimeType.Singleton)
		.Register<IConfigProtector, ConfigProtector>(LifetimeType.Singleton);

	public static IDIRegistrator RegisterConfiguration(this IDIRegistrator registrator) => registrator
		.Register<IConnectionConfigRepository>(c =>
			new ProtectedConfigRepository(
				new JsonBasedConnectionConfigRepository(c.Resolve<IAppEnvironment>()),
				c.Resolve<IConfigProtector>()),
			LifetimeType.Singleton)

		.Register<IAppSettingsRepository, JsonBasedSettingsRepository>(LifetimeType.Singleton);

	public static IDIRegistrator RegisterIEtcdClient(this IDIRegistrator registrator) => registrator
		.Register<IEtcdClient, DotnetEtcdBasedClient>(LifetimeType.Singleton);

	public static IDIRegistrator RegisterScreens(this IDIRegistrator registrator) => registrator
		.Register<InstanceSelectionScreen>(LifetimeType.Transient)
		.Register<MainScreen>(LifetimeType.Transient)
		.Register<KeyBrowseScreen>(LifetimeType.Transient)
		.Register<KeyCreateScreen>(LifetimeType.Transient)
		.Register<UserManagementScreen>(LifetimeType.Transient)
		.Register<RoleManagementScreen>(LifetimeType.Transient)
		.Register<PermissionViewScreen>(LifetimeType.Transient)
		.Register<SettingsScreen>(LifetimeType.Transient);
}
