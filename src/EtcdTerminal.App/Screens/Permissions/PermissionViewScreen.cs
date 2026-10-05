using EtcdTerminal.App.Components;
using EtcdTerminal.App.Screens.MainMenu;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Roles;
using EtcdTerminal.Security;
using EtcdTerminal.Users;

namespace EtcdTerminal.App.Screens.Permissions;

public sealed class PermissionViewScreen(IEtcdUserAdmin _userAdmin, IEtcdRoleAdmin _roleAdmin, Screen _screen, PermissionViewLayout _layout, PressAnyKeyPrompt _pressAnyKey, Spinner _spinner, Message _message, ILocalization _localization) : IMainMenuEntry
{
	public MainMenuAction Action => MainMenuAction.ViewPermissions;

	public string Label => _localization.ViewPermissions;

	public bool IsAvailable(UserCapabilities capabilities) => capabilities.CanManageAuth;
	public async Task ShowAsync()
	{
		_screen.Open();

		IReadOnlyList<EtcdUser> users = [];
		IReadOnlyList<EtcdRole> roles = [];

		var loaded = await _spinner.RunAsync(_localization.LoadingPermissions, async ct =>
		{
			users = await _userAdmin.GetUsersAsync(ct);
			roles = await _roleAdmin.GetRolesAsync(ct);
		});

		if (!loaded)
		{
			_message.ShowWarning(_localization.OperationCancelled);

			return;
		}

		_pressAnyKey.Show(_layout.Body(users, roles));
	}
}
