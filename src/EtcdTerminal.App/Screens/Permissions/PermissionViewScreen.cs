using EtcdTerminal.App.Components;
using EtcdTerminal.App.Screens;
using EtcdTerminal.Localization;
using EtcdTerminal.Roles;
using EtcdTerminal.Security;
using EtcdTerminal.Terminal;
using EtcdTerminal.Users;

namespace EtcdTerminal.App.Screens.Permissions;

public sealed class PermissionViewScreen(ITerminal _terminal, IEtcdUserAdmin _userAdmin, IEtcdRoleAdmin _roleAdmin, ScreenLayout _screenLayout, PressAnyKeyPrompt _pressAnyKey, Spinner _spinner, Message _message, ILocalization _localization) : IMainMenuEntry
{
	public MainMenuAction Action => MainMenuAction.ViewPermissions;

	public string Label => _localization.ViewPermissions;

	public bool IsAvailable(UserCapabilities capabilities) => capabilities.CanManageAuth;
	public async Task ShowAsync()
	{
		_screenLayout.RenderHeader();

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

		PermissionViewRenderer.Render(_terminal, _localization, users, roles);

		_terminal.WriteLine();
		_pressAnyKey.Show();
	}
}
