using EtcdTerminal.App.Components;
using EtcdTerminal.App.Screens.MainMenu;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Roles;
using EtcdTerminal.Security;
using EtcdTerminal.Users;

namespace EtcdTerminal.App.Screens.Permissions;

/// <summary>
/// The permission page: every permission of every role of every user as one
/// filterable, paginated list between the banner and the footer. View-only by
/// design — it holds no cursor and offers no actions.
/// </summary>
public sealed class PermissionListScreen(
	IEtcdUserAdmin _userAdmin,
	IEtcdRoleAdmin _roleAdmin,
	PermissionListLayout _layout,
	ListBrowser _browser,
	Screen _screen,
	Spinner _spinner,
	Message _message,
	ILocalization _localization) : IMainMenuEntry
{
	public MainMenuAction Action => MainMenuAction.ListPermissions;

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

		_browser.Show(
			_layout.Headers(),
			_layout.Rows(users, roles),
			_localization.NoPermissionsFound,
			_localization.TotalPermissions);
	}
}
