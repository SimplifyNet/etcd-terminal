using EtcdTerminal.App.Components;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Roles;

namespace EtcdTerminal.App.Screens.Roles.Commands;

public sealed class RevokeRolePermissionCommand(IEtcdRoleAdmin _roleAdmin, PermissionTargetPrompt _targets, Message _message, ILocalization _localization) : IMenuCommand<RoleMenuAction>
{
	public RoleMenuAction Action => RoleMenuAction.RevokePermission;

	public async Task ExecuteAsync()
	{
		var target = _targets.AskTarget();

		if (target is null)
			return;

		// Revocation removes the whole permission for the target interval,
		// so no permission type is requested here.
		var result = await _roleAdmin.RevokePermissionAsync(target.RoleName, target.Key, target.Scope);

		_message.ShowResult(result, _localization.PermissionRevoked, _localization.FailedRevokePermission);
	}
}
