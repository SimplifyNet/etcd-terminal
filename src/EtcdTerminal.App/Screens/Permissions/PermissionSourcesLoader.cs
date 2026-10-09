using EtcdTerminal.App.Components;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Roles;
using EtcdTerminal.Users;

namespace EtcdTerminal.App.Screens.Permissions;

public sealed class PermissionSourcesLoader(IEtcdUserAdmin _userAdmin, IEtcdRoleAdmin _roleAdmin, CancellableLoad _load, ILocalizationCatalog _localizations)
{
	public Task<PermissionSources?> LoadAsync() =>
		_load.RunAsync(_localizations.Current.LoadingPermissions, async ct =>
			new PermissionSources(await _userAdmin.GetUsersAsync(ct), await _roleAdmin.GetRolesAsync(ct)));
}
