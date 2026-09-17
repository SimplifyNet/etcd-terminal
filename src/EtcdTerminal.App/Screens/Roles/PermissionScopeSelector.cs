using EtcdTerminal.App.Engine;
using EtcdTerminal.Permissions;
using EtcdTerminal.Localization;

namespace EtcdTerminal.App.Screens.Roles;

public sealed class PermissionScopeSelector(Menu _menu)
{
	public PermissionScope? Select()
	{
		var key = LocalizationStore.Current.ScopeKey;
		var prefix = LocalizationStore.Current.ScopePrefix;

		return _menu.Show<PermissionScope>(LocalizationStore.Current.SelectPermissionScope,
		[
			new(PermissionScope.Prefix, prefix),
			new(PermissionScope.Key, key)
		])?.Id;
	}
}
