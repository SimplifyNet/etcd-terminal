using EtcdTerminal.App.Components;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Roles;
using EtcdTerminal.Users;

namespace EtcdTerminal.App.Screens.Permissions;

public sealed class PermissionSourcesLoader(IEtcdUserAdmin _userAdmin, IEtcdRoleAdmin _roleAdmin, CancellableLoad _load, ILocalization _localization)
{
	public Task<PermissionSources?> LoadAsync() =>
		_load.RunAsync(_localization.LoadingPermissions, async ct =>
			new PermissionSources(await _userAdmin.GetUsersAsync(ct), await _roleAdmin.GetRolesAsync(ct)));
}
