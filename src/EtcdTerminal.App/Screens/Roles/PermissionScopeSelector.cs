using EtcdTerminal.App.Engine;
using EtcdTerminal.Permissions;
using EtcdTerminal.Localization;

namespace EtcdTerminal.App.Screens.Roles;

public sealed class PermissionScopeSelector(Menu _menu, ILocalization _localization)
{
	public PermissionScope? Select()
	{
		var key = _localization.ScopeKey;
		var prefix = _localization.ScopePrefix;

		return _menu.Show<PermissionScope>(_localization.SelectPermissionScope,
		[
			new(PermissionScope.Prefix, prefix),
			new(PermissionScope.Key, key)
		])?.Id;
	}
}
