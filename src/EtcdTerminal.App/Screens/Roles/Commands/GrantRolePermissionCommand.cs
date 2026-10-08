using EtcdTerminal.App.Components;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Roles;

namespace EtcdTerminal.App.Screens.Roles.Commands;

public sealed class GrantRolePermissionCommand(IEtcdRoleAdmin _roleAdmin, PermissionTargetPrompt _targets, Message _message, ILocalization _localization) : IMenuCommand<RoleMenuAction>
{
	public RoleMenuAction Action => RoleMenuAction.GrantPermission;

	public async Task ExecuteAsync()
	{
		var grant = _targets.AskGrant();

		if (grant is null)
			return;

		var result = await _roleAdmin.GrantPermissionAsync(grant.Target.RoleName, grant.Type, grant.Target.Key, grant.Target.Scope);

		_message.ShowResult(result, _localization.PermissionGranted, _localization.FailedGrantPermission);
	}
}
