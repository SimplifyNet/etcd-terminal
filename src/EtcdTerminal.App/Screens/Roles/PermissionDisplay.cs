using EtcdTerminal.App.Components;
using EtcdTerminal.Permissions;
using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Screens.Roles;

/// <summary>
/// Single permission formatter shared by the role and permission views.
/// Every scope renders distinctly: exact keys, prefixes, bounded ranges with
/// exclusive endpoints, open-ended ranges, and the all-keys grant.
/// </summary>
public static class PermissionDisplay
{
	public static string For(EtcdPermission permission, ILocalization localization)
	{
		var type = PermissionTypeText.For(permission.Type, localization);
		var scope = PermissionScopeText.For(permission.Scope, localization);
		var key = DisplayText.Sanitize(permission.KeyPrefix);

		if (permission.Scope is PermissionScope.Prefix && EtcdPermission.NormalizeKey(permission.KeyPrefix).Length == 0)
			return $"{type} [{scope}]: {localization.AllKeys}";

		return permission.Scope switch
		{
			PermissionScope.Key or PermissionScope.Prefix => $"{type} [{scope}]: {key}",
			_ => permission.RangeEnd == PermissionRange.AllKeys
				? $"{type} [{scope}]: [{key}, \u221E)"
				: $"{type} [{scope}]: [{key}, {DisplayText.Sanitize(permission.RangeEnd)})"
		};
	}
}
