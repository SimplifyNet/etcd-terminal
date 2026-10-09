using EtcdTerminal.App.Components;
using EtcdTerminal.App.Screens.MainMenu;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Security;

namespace EtcdTerminal.App.Screens.Roles;

public sealed class RolesManagementScreen(MenuScreen _menuScreen, IEnumerable<IMenuCommand<RoleMenuAction>> _commands, ILocalizationCatalog _localizations) : IMainMenuEntry
{
	public MainMenuAction Action => MainMenuAction.ManageRoles;

	public bool IsAvailable(UserCapabilities capabilities) => capabilities.CanManageAuth;

	public async Task ShowAsync() =>
		await _menuScreen.RunAsync<RoleMenuAction>(_localizations.Current.RolesManagement,
		[
			new(RoleMenuAction.ListRoles, _localizations.Current.ListRoles),
			new(RoleMenuAction.CreateRole, _localizations.Current.CreateRole),
			new(RoleMenuAction.DeleteRole, _localizations.Current.DeleteRole),
			new(RoleMenuAction.GrantPermission, _localizations.Current.GrantPermission),
			new(RoleMenuAction.RevokePermission, _localizations.Current.RevokePermission)
		],
		action => _commands.Single(c => c.Action == action).ExecuteAsync());
}
