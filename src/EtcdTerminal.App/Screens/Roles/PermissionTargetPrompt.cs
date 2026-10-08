using EtcdTerminal.App.Components;
using EtcdTerminal.Permissions;
using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Screens.Roles;

public sealed class PermissionTargetPrompt(UserInput _input, PermissionScopeSelector _scope, PermissionTypeSelector _type, ILocalization _localization)
{
	public PermissionTarget? AskTarget()
	{
		var roleName = _input.Ask(_localization.EnterRoleNamePrompt);

		if (roleName is null)
			return null;

		var scope = _scope.Select();

		if (scope is null)
			return null;

		var keyPromptText = scope is PermissionScope.Prefix
			? _localization.EnterKeyPrefix
			: _localization.EnterExactKey;

		var key = _input.Ask(keyPromptText);

		if (key is null)
			return null;

		return new(roleName, key, scope.Value);
	}

	/// Target first, then the permission type - the order the user saw before.
	public PermissionGrant? AskGrant()
	{
		var target = AskTarget();

		if (target is null)
			return null;

		var type = _type.Select();

		return type is null ? null : new(target, type.Value);
	}
}
