using EtcdTerminal.App.Components;
using EtcdTerminal.Users;

namespace EtcdTerminal.App.Screens.Users.Commands;

/// <summary>
/// The view-only page: the list, filterable and paginated like the key
/// browser instead of a single framed screen.
/// </summary>
public sealed class ListUsersCommand(IEtcdUserAdmin _userAdmin, CancellableLoad _load, UserListLayout _layout, ListBrowser _browser) : IMenuCommand<UserMenuAction>
{
	public UserMenuAction Action => UserMenuAction.ListUsers;

	public async Task ExecuteAsync()
	{
		var users = await _load.RunAsync(_layout.Loading, _userAdmin.GetUsersAsync);

		if (users is null)
			return;

		_browser.Show(_layout.Headers(), _layout.Rows(users), _layout.Empty, _layout.Total);
	}
}
