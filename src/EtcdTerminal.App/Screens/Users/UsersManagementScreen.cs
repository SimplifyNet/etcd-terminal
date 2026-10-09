using EtcdTerminal.App.Components;
using EtcdTerminal.App.Screens.MainMenu;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Security;

namespace EtcdTerminal.App.Screens.Users;

public sealed class UsersManagementScreen(MenuScreen _menuScreen, IEnumerable<IMenuCommand<UserMenuAction>> _commands, ILocalizationCatalog _localizations) : IMainMenuEntry
{
	public MainMenuAction Action => MainMenuAction.ManageUsers;

	public bool IsAvailable(UserCapabilities capabilities) => capabilities.CanManageAuth;

	public async Task ShowAsync() =>
		await _menuScreen.RunAsync<UserMenuAction>(_localizations.Current.UsersManagement,
		[
			new(UserMenuAction.ListUsers, _localizations.Current.ListUsers),
			new(UserMenuAction.CreateUser, _localizations.Current.CreateUser),
			new(UserMenuAction.DeleteUser, _localizations.Current.DeleteUser),
			new(UserMenuAction.ChangePassword, _localizations.Current.ChangePassword),
			new(UserMenuAction.AssignRole, _localizations.Current.AssignRole),
			new(UserMenuAction.RemoveRole, _localizations.Current.RemoveRole)
		],
		action => _commands.Single(c => c.Action == action).ExecuteAsync());
}
