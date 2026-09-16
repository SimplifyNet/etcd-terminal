using EtcdTerminal.App.Screens;
using EtcdTerminal.App.Screens.Keys;
using EtcdTerminal.App.Screens.Permissions;
using EtcdTerminal.App.Screens.Roles;
using EtcdTerminal.App.Screens.Users;
using EtcdTerminal.Security;
using EtcdTerminal.Configuration;
using EtcdTerminal.Keys;
using EtcdTerminal.Roles;
using EtcdTerminal.Users;
using EtcdTerminal.Infrastructure.Configuration;
using EtcdTerminal.Infrastructure.Environment;
using EtcdTerminal.Infrastructure.Security;
using EtcdTerminal.Infrastructure.Terminal;
using EtcdTerminal.Terminal;
using Simplify.DI;
using EtcdTerminal.Environment;
using EtcdTerminal.Infrastructure;
using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;

namespace EtcdTerminal.App.Setup;

public static class IocRegistrations
{
	public static IDIContainerProvider RegisterAll(this IDIContainerProvider provider)
	{
		provider.RegisterInfrastructure()
			   .RegisterConfiguration()
			   .RegisterIEtcdClient()
			   .RegisterComponents()
			   .RegisterScreens();

		return provider;
	}

	public static IDIRegistrator RegisterInfrastructure(this IDIRegistrator registrator) => registrator
		.Register<ITerminal, ConsoleTerminal>(LifetimeType.Singleton)
		.Register<ITextInput, SpectreTextInput>(LifetimeType.Singleton)
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
		.Register<IEtcdClient, DotnetEtcdBasedClient>(LifetimeType.Singleton)
		.Register<IEtcdConnection>(c => c.Resolve<IEtcdClient>(), LifetimeType.Singleton)
		.Register<IEtcdKeyStore>(c => c.Resolve<IEtcdClient>(), LifetimeType.Singleton)
		.Register<IEtcdUserAdmin>(c => c.Resolve<IEtcdClient>(), LifetimeType.Singleton)
		.Register<IEtcdRoleAdmin>(c => c.Resolve<IEtcdClient>(), LifetimeType.Singleton)
		.Register<IEtcdAuthAdmin>(c => c.Resolve<IEtcdClient>(), LifetimeType.Singleton);

	public static IDIRegistrator RegisterComponents(this IDIRegistrator registrator) => registrator
		.Register<StatusBar>(LifetimeType.Transient)
		.Register<Menu>(LifetimeType.Transient)
		.Register<MenuScreen>(LifetimeType.Transient)
		.Register<ScreenLayout>(LifetimeType.Transient)
		.Register<KeyBrowseLayout>(LifetimeType.Transient)
		.Register<PressAnyKeyPrompt>(LifetimeType.Transient)
		.Register<Message>(LifetimeType.Transient)
		.Register<Prompt>(LifetimeType.Transient)
		.Register<MultiLinePasteReader>(LifetimeType.Transient)
		.Register<Spinner>(LifetimeType.Transient);

	public static IDIRegistrator RegisterScreens(this IDIRegistrator registrator) => registrator
		.Register<InstanceSelectionScreen>(LifetimeType.Transient)
		.Register<MainScreen>(LifetimeType.Transient)
		.Register<KeyBrowseScreen>(LifetimeType.Transient)
		.Register<KeyCreateScreen>(LifetimeType.Transient)
		.Register<KeyImportJsonScreen>(LifetimeType.Transient)
		.Register<UserManagementScreen>(LifetimeType.Transient)
		.Register<RoleManagementScreen>(LifetimeType.Transient)
		.Register<PermissionViewScreen>(LifetimeType.Transient)
		.Register<SettingsScreen>(LifetimeType.Transient)
		.Register<PermissionTypeSelector>(LifetimeType.Transient)
		.Register<KeyBrowseControl>(LifetimeType.Transient);
}
