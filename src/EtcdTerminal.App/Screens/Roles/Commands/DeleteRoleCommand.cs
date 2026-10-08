using EtcdTerminal.App.Components;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Roles;

namespace EtcdTerminal.App.Screens.Roles.Commands;

public sealed class DeleteRoleCommand(IEtcdRoleAdmin _roleAdmin, UserInput _input, Message _message, ILocalization _localization) : IMenuCommand<RoleMenuAction>
{
	public RoleMenuAction Action => RoleMenuAction.DeleteRole;

	public async Task ExecuteAsync()
	{
		var roleName = _input.Ask(_localization.EnterRoleNameToDelete);

		if (roleName is null)
			return;

		var result = await _roleAdmin.DeleteRoleAsync(roleName);

		_message.ShowResult(result, _localization.RoleDeleted, _localization.FailedDeleteRole);
	}
}
