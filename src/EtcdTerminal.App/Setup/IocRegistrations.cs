using EtcdTerminal.App.Screens;
using EtcdTerminal.App.Screens.Keys;
using EtcdTerminal.App.Screens.Permissions;
using EtcdTerminal.App.Screens.Roles;
using EtcdTerminal.App.Screens.Users;
using EtcdTerminal.Security;
using EtcdTerminal.Configuration;
using EtcdTerminal.Session;
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
		provider.RegisterTerminal()
			   .RegisterConfiguration()
			   .RegisterSession()
			   .RegisterClient()
			   .RegisterKeys()
			   .RegisterUsers()
			   .RegisterRoles()
			   .RegisterSecurity()
			   .RegisterEnvironment()
			   .RegisterEngine()
			   .RegisterComponents()
			   .RegisterScreens();

		return provider;
	}

	public static IDIRegistrator RegisterTerminal(this IDIRegistrator registrator) => registrator
		.Register<ITerminal, ConsoleTerminal>(LifetimeType.Singleton)
		.Register<ITextInput, SpectreTextInput>(LifetimeType.Singleton);

	public static IDIRegistrator RegisterConfiguration(this IDIRegistrator registrator) => registrator
		.Register<JsonConfigFile>(c => new JsonConfigFile(c.Resolve<IAppEnvironment>()), LifetimeType.Singleton)
		.Register<ProtectedConfigRepository>(c =>
			new ProtectedConfigRepository(
				new JsonBasedConnectionConfigRepository(c.Resolve<JsonConfigFile>()),
				c.Resolve<IConfigProtector>()),
			LifetimeType.Singleton)
		.Register<IConnectionConfigRepository>(c => c.Resolve<ProtectedConfigRepository>(), LifetimeType.Singleton)
		.Register<IDecryptFailureSource>(c => c.Resolve<ProtectedConfigRepository>(), LifetimeType.Singleton)

		.Register<IAppSettingsRepository, JsonBasedSettingsRepository>(LifetimeType.Singleton)
		.Register<IEtcdConnection>(c => c.Resolve<IEtcdClient>(), LifetimeType.Singleton);

	public static IDIRegistrator RegisterSession(this IDIRegistrator registrator) => registrator
		.Register<IConnectionSession, ConnectionSession>(LifetimeType.Singleton);

	public static IDIRegistrator RegisterClient(this IDIRegistrator registrator) => registrator
		.Register<IEtcdClient, DotnetEtcdBasedClient>(LifetimeType.Singleton);

	public static IDIRegistrator RegisterKeys(this IDIRegistrator registrator) => registrator
		.Register<IEtcdKeyStore>(c => c.Resolve<IEtcdClient>(), LifetimeType.Singleton)
		.Register<IReadableKeysProvider, ReadableKeysProvider>(LifetimeType.Transient)
		.Register<IKeyImporter, KeyImporter>(LifetimeType.Transient);

	public static IDIRegistrator RegisterUsers(this IDIRegistrator registrator) => registrator
		.Register<IEtcdUserAdmin>(c => c.Resolve<IEtcdClient>(), LifetimeType.Singleton);

	public static IDIRegistrator RegisterRoles(this IDIRegistrator registrator) => registrator
		.Register<IEtcdRoleAdmin>(c => c.Resolve<IEtcdClient>(), LifetimeType.Singleton);

	public static IDIRegistrator RegisterSecurity(this IDIRegistrator registrator) => registrator
		.Register<IEtcdAuthAdmin>(c => c.Resolve<IEtcdClient>(), LifetimeType.Singleton)
		.Register<IUserCapabilitiesProvider, UserCapabilitiesProvider>(LifetimeType.Transient)
		.Register<IConfigProtector, ConfigProtector>(LifetimeType.Singleton);

	public static IDIRegistrator RegisterEnvironment(this IDIRegistrator registrator) => registrator
		.Register<IAppEnvironment, AppEnvironment>(LifetimeType.Singleton)
		.Register<IAppInfo, AppInfo>(LifetimeType.Singleton);

	public static IDIRegistrator RegisterEngine(this IDIRegistrator registrator) => registrator
		.Register<Menu>(LifetimeType.Transient)
		.Register<Prompt>(LifetimeType.Transient);

	public static IDIRegistrator RegisterComponents(this IDIRegistrator registrator) => registrator
		.Register<StatusBar>(LifetimeType.Transient)
		.Register<Header>(LifetimeType.Transient)
		.Register<MenuScreen>(LifetimeType.Transient)
		.Register<ScreenLayout>(LifetimeType.Transient)
		.Register<PressAnyKeyPrompt>(LifetimeType.Transient)
		.Register<Message>(LifetimeType.Transient)
		.Register<MultiLinePasteReader>(LifetimeType.Transient)
		.Register<Spinner>(LifetimeType.Transient);

	public static IDIRegistrator RegisterScreens(this IDIRegistrator registrator) => registrator
		.Register<InstanceSelectionScreen>(LifetimeType.Transient)
		.Register<ManageConnectionsScreen>(LifetimeType.Transient)
		.Register<MainScreen>(LifetimeType.Transient)
		.Register<KeyBrowseScreen>(LifetimeType.Transient)
		.Register<KeyCreateScreen>(LifetimeType.Transient)
		.Register<KeyImportJsonScreen>(LifetimeType.Transient)
		.Register<UserManagementScreen>(LifetimeType.Transient)
		.Register<RoleManagementScreen>(LifetimeType.Transient)
		.Register<PermissionViewScreen>(LifetimeType.Transient)
		.Register<SettingsScreen>(LifetimeType.Transient)
		.Register<PermissionTypeSelector>(LifetimeType.Transient)
		.Register<PermissionScopeSelector>(LifetimeType.Transient)
		.Register<KeyBrowseControl>(LifetimeType.Transient)
		.Register<KeyBrowseLayout>(LifetimeType.Transient);
}
