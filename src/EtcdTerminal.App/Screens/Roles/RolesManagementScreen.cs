using EtcdTerminal.App.Components;
using EtcdTerminal.App.Screens.MainMenu;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Security;

namespace EtcdTerminal.App.Screens.Roles;

public sealed class RolesManagementScreen(MenuScreen _menuScreen, IEnumerable<IMenuCommand<RoleMenuAction>> _commands, ILocalization _localization) : IMainMenuEntry
{
	public MainMenuAction Action => MainMenuAction.ManageRoles;

	public bool IsAvailable(UserCapabilities capabilities) => capabilities.CanManageAuth;

	public async Task ShowAsync() =>
		await _menuScreen.RunAsync<RoleMenuAction>(_localization.RolesManagement,
		[
			new(RoleMenuAction.ListRoles, _localization.ListRoles),
			new(RoleMenuAction.CreateRole, _localization.CreateRole),
			new(RoleMenuAction.DeleteRole, _localization.DeleteRole),
			new(RoleMenuAction.GrantPermission, _localization.GrantPermission),
			new(RoleMenuAction.RevokePermission, _localization.RevokePermission)
		],
		action => _commands.Single(c => c.Action == action).ExecuteAsync());
}
