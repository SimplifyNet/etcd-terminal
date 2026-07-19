using EtcdTerminal.Console.Screens;
using EtcdTerminal.Infrastructure.Configuration;
using EtcdTerminal.Infrastructure.Etcd;
using Simplify.DI;

namespace EtcdTerminal.Console.Setup;

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
		.Register<IConnectionConfigRepository, JsonBasedConfigRepository>(LifetimeType.Singleton);

	public static IDIRegistrator RegisterIEtcdClient(this IDIRegistrator registrator) => registrator
		.Register<IEtcdClient, DotnetEtcdBasedClient>(LifetimeType.Singleton);

	public static IDIRegistrator RegisterScreens(this IDIRegistrator registrator) => registrator
		.Register<InstanceSelectionScreen>(LifetimeType.Transient)
		.Register<MainScreen>(LifetimeType.Transient)
		.Register<KeyBrowseScreen>(LifetimeType.Transient)
		.Register<KeyCreateScreen>(LifetimeType.Transient)
		.Register<UserManagementScreen>(LifetimeType.Transient)
		.Register<RoleManagementScreen>(LifetimeType.Transient)
		.Register<PermissionViewScreen>(LifetimeType.Transient);
}
