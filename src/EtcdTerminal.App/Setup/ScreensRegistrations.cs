using EtcdTerminal.App.Components;
using EtcdTerminal.App.Screens.Connections;
using EtcdTerminal.App.Screens.Keys;
using EtcdTerminal.App.Screens.MainMenu;
using EtcdTerminal.App.Screens.Permissions;
using EtcdTerminal.App.Screens.Roles;
using EtcdTerminal.App.Screens.Roles.Commands;
using EtcdTerminal.App.Screens.Settings;
using EtcdTerminal.App.Screens.Users;
using EtcdTerminal.App.Screens.Users.Commands;
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
		.Register<PermissionSourcesLoader>(LifetimeType.Transient)
		.Register<SettingsScreen>(LifetimeType.Transient)
		.Register<PermissionTypeSelector>(LifetimeType.Transient)
		.Register<PermissionScopeSelector>(LifetimeType.Transient)
		.Register<PermissionTargetPrompt>(LifetimeType.Transient)
		.Register<KeyBrowseControl>(LifetimeType.Transient)
		.Register<KeyBrowseLayout>(LifetimeType.Transient)
		.Register<KeyBrowseView>(LifetimeType.Transient)
		.Register<KeyBrowseList>(LifetimeType.Transient)
		.Register<KeyEditPrompt>(LifetimeType.Transient)
		.Register<KeyChanges>(LifetimeType.Transient)
		.Register<ListRolesCommand>(LifetimeType.Transient)
		.Register<CreateRoleCommand>(LifetimeType.Transient)
		.Register<DeleteRoleCommand>(LifetimeType.Transient)
		.Register<GrantRolePermissionCommand>(LifetimeType.Transient)
		.Register<RevokeRolePermissionCommand>(LifetimeType.Transient)
		.Register<ListUsersCommand>(LifetimeType.Transient)
		.Register<CreateUserCommand>(LifetimeType.Transient)
		.Register<DeleteUserCommand>(LifetimeType.Transient)
		.Register<ChangePasswordCommand>(LifetimeType.Transient)
		.Register<AssignRoleCommand>(LifetimeType.Transient)
		.Register<RemoveRoleCommand>(LifetimeType.Transient)
		.Register<IEnumerable<IMainMenuEntry>>(c =>
		[
			c.Resolve<KeyBrowseScreen>(),
			c.Resolve<KeyCreateScreen>(),
			c.Resolve<KeyImportJsonScreen>(),
			c.Resolve<UsersManagementScreen>(),
			c.Resolve<RolesManagementScreen>(),
			c.Resolve<PermissionListScreen>()
		], LifetimeType.Transient)
		.Register<IEnumerable<IMenuCommand<RoleMenuAction>>>(c =>
		[
			c.Resolve<ListRolesCommand>(),
			c.Resolve<CreateRoleCommand>(),
			c.Resolve<DeleteRoleCommand>(),
			c.Resolve<GrantRolePermissionCommand>(),
			c.Resolve<RevokeRolePermissionCommand>()
		], LifetimeType.Transient)
		.Register<IEnumerable<IMenuCommand<UserMenuAction>>>(c =>
		[
			c.Resolve<ListUsersCommand>(),
			c.Resolve<CreateUserCommand>(),
			c.Resolve<DeleteUserCommand>(),
			c.Resolve<ChangePasswordCommand>(),
			c.Resolve<AssignRoleCommand>(),
			c.Resolve<RemoveRoleCommand>()
		], LifetimeType.Transient);
}
