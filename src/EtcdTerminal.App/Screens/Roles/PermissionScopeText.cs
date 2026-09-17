using EtcdTerminal.Localization;
using EtcdTerminal.Permissions;

namespace EtcdTerminal.App.Screens.Roles;

public static class PermissionScopeText
{
	public static string For(PermissionScope scope) => scope switch
	{
		PermissionScope.Key => LocalizationStore.Current.ScopeKey,
		PermissionScope.Prefix => LocalizationStore.Current.ScopePrefix,
		_ => LocalizationStore.Current.ScopeRange
	};
}
