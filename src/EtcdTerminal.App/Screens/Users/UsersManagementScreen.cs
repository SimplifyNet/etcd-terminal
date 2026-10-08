using EtcdTerminal.App.Components;
using EtcdTerminal.App.Screens.MainMenu;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Security;

namespace EtcdTerminal.App.Screens.Users;

public sealed class UsersManagementScreen(MenuScreen _menuScreen, IEnumerable<IMenuCommand<UserMenuAction>> _commands, ILocalization _localization) : IMainMenuEntry
{
	public MainMenuAction Action => MainMenuAction.ManageUsers;

	public bool IsAvailable(UserCapabilities capabilities) => capabilities.CanManageAuth;

	public async Task ShowAsync() =>
		await _menuScreen.RunAsync<UserMenuAction>(_localization.UsersManagement,
		[
			new(UserMenuAction.ListUsers, _localization.ListUsers),
			new(UserMenuAction.CreateUser, _localization.CreateUser),
			new(UserMenuAction.DeleteUser, _localization.DeleteUser),
			new(UserMenuAction.ChangePassword, _localization.ChangePassword),
			new(UserMenuAction.AssignRole, _localization.AssignRole),
			new(UserMenuAction.RemoveRole, _localization.RemoveRole)
		],
		action => _commands.Single(c => c.Action == action).ExecuteAsync());
}
