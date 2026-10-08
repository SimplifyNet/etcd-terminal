using EtcdTerminal.App.Components;
using EtcdTerminal.App.Screens.MainMenu;
using EtcdTerminal.Security;

namespace EtcdTerminal.App.Screens.Permissions;

/// <summary>
/// The permission page: every permission of every role of every user as one
/// filterable, paginated list between the banner and the footer. View-only by
/// design — it holds no cursor and offers no actions.
/// </summary>
public sealed class PermissionListScreen(PermissionSourcesLoader _loader, PermissionListLayout _layout, ListBrowser _browser, Screen _screen) : IMainMenuEntry
{
	public MainMenuAction Action => MainMenuAction.ListPermissions;

	public bool IsAvailable(UserCapabilities capabilities) => capabilities.CanManageAuth;

	public async Task ShowAsync()
	{
		_screen.Open();

		var sources = await _loader.LoadAsync();

		if (sources is null)
			return;

		_browser.Show(
			_layout.Headers(),
			_layout.Rows(sources.Users, sources.Roles),
			_layout.Empty,
			_layout.Total);
	}
}
