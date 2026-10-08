using EtcdTerminal.App.Components;
using EtcdTerminal.Roles;

namespace EtcdTerminal.App.Screens.Roles.Commands;

/// <summary>
/// The view-only page: one row per permission, filterable and paginated
/// like the key browser instead of a sectioned framed screen.
/// </summary>
public sealed class ListRolesCommand(IEtcdRoleAdmin _roleAdmin, CancellableLoad _load, RoleListLayout _layout, ListBrowser _browser) : IMenuCommand<RoleMenuAction>
{
	public RoleMenuAction Action => RoleMenuAction.ListRoles;

	public async Task ExecuteAsync()
	{
		var roles = await _load.RunAsync(_layout.Loading, _roleAdmin.GetRolesAsync);

		if (roles is null)
			return;

		// A row of this page is one permission of a role, so the pagination
		// counts the permission rows it filters; "total roles" would promise
		// a count the rows do not hold.
		_browser.Show(_layout.Headers(), _layout.Rows(roles), _layout.Empty, _layout.Total);
	}
}
