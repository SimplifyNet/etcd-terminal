using EtcdTerminal.Localization;
using EtcdTerminal.Permissions;

namespace EtcdTerminal.App.Screens.Roles;

public static class PermissionScopeText
{
	public static string For(PermissionScope scope, ILocalization localization) => scope switch
	{
		PermissionScope.Key => localization.ScopeKey,
		PermissionScope.Prefix => localization.ScopePrefix,
		_ => localization.ScopeRange
	};
}
