using EtcdTerminal.App.Screens.Connections;
using EtcdTerminal.App.Screens.Keys;
using EtcdTerminal.App.Screens.MainMenu;
using EtcdTerminal.App.Screens.Permissions;
using EtcdTerminal.App.Screens.Roles;
using EtcdTerminal.App.Screens.Settings;
using EtcdTerminal.App.Screens.Users;
using Simplify.DI;

namespace EtcdTerminal.App.Setup;

public static class ScreensRegistrations
{
	public static IDIRegistrator RegisterScreens(this IDIRegistrator registrator) => registrator
		.Register<InstanceSelectionScreen>(LifetimeType.Transient)
		.Register<ManageConnectionsScreen>(LifetimeType.Transient)
		.Register<MainScreen>(LifetimeType.Transient)
		.Register<MainMenuLabels>(LifetimeType.Transient)
		.Register<MainMenuItems>(LifetimeType.Transient)
		.Register<KeyBrowseScreen>(LifetimeType.Transient)
		.Register<KeyCreateScreen>(LifetimeType.Transient)
		.Register<KeyImportJsonScreen>(LifetimeType.Transient)
		.Register<UsersManagementScreen>(LifetimeType.Transient)
		.Register<RolesManagementScreen>(LifetimeType.Transient)
		.Register<PermissionListScreen>(LifetimeType.Transient)
		.Register<SettingsScreen>(LifetimeType.Transient)
		.Register<PermissionTypeSelector>(LifetimeType.Transient)
		.Register<PermissionScopeSelector>(LifetimeType.Transient)
		.Register<KeyBrowseControl>(LifetimeType.Transient)
		.Register<KeyBrowseLayout>(LifetimeType.Transient)
		.Register<IEnumerable<IMainMenuEntry>>(c =>
		[
			c.Resolve<KeyBrowseScreen>(),
			c.Resolve<KeyCreateScreen>(),
			c.Resolve<KeyImportJsonScreen>(),
			c.Resolve<UsersManagementScreen>(),
			c.Resolve<RolesManagementScreen>(),
			c.Resolve<PermissionListScreen>()
		], LifetimeType.Transient);
}
