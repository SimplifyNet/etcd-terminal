using EtcdTerminal;
using EtcdTerminal.Console.Screens;
using EtcdTerminal.Infrastructure.Configuration;
using EtcdTerminal.Infrastructure.Etcd;
using Simplify.DI;

namespace EtcdTerminal.Console.DI;

public static class IocRegistrations
{
	public static IDIContainerProvider RegisterAll(this IDIContainerProvider provider)
	{
		provider.RegisterConfiguration()
			   .RegisterIEtcdClient()
			   .RegisterScreens();

		return provider;
	}

	public static IDIRegistrator RegisterConfiguration(this IDIRegistrator registrator) => registrator
		.Register<IConnectionConfigRepository, ConnectionConfigRepository>(LifetimeType.Singleton);

	public static IDIRegistrator RegisterIEtcdClient(this IDIRegistrator registrator) => registrator
		.Register<IEtcdClient, EtcdClientAdapter>(LifetimeType.Singleton);

	public static IDIRegistrator RegisterScreens(this IDIRegistrator registrator) => registrator
		.Register<InstanceSelectionScreen>(LifetimeType.Transient)
		.Register<MainScreen>(LifetimeType.Transient)
		.Register<KeyBrowserScreen>(LifetimeType.Transient)
		.Register<KeySearchScreen>(LifetimeType.Transient)
		.Register<KeyCreateScreen>(LifetimeType.Transient)
		.Register<KeyEditScreen>(LifetimeType.Transient)
		.Register<UserManagementScreen>(LifetimeType.Transient)
		.Register<RoleManagementScreen>(LifetimeType.Transient)
		.Register<PermissionViewScreen>(LifetimeType.Transient);
}
