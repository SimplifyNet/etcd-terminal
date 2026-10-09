using EtcdTerminal.App.Engine;
using EtcdTerminal.Permissions;
using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Screens.Roles;

public sealed class PermissionScopeSelector(Menu _menu, ILocalizationCatalog _localizations)
{
	public PermissionScope? Select()
	{
		var key = _localizations.Current.ScopeKey;
		var prefix = _localizations.Current.ScopePrefix;

		return _menu.Ask<PermissionScope>(_localizations.Current.SelectPermissionScope,
		[
			new(PermissionScope.Prefix, prefix),
			new(PermissionScope.Key, key)
		])?.Id;
	}
}
