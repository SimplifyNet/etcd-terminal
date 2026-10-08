using EtcdTerminal.App.Components;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Roles;

namespace EtcdTerminal.App.Screens.Roles.Commands;

public sealed class CreateRoleCommand(IEtcdRoleAdmin _roleAdmin, UserInput _input, Message _message, ILocalization _localization) : IMenuCommand<RoleMenuAction>
{
	public RoleMenuAction Action => RoleMenuAction.CreateRole;

	public async Task ExecuteAsync()
	{
		var roleName = _input.Ask(_localization.EnterRoleNamePrompt);

		if (roleName is null)
			return;

		var result = await _roleAdmin.CreateRoleAsync(roleName);

		_message.ShowResult(result, _localization.RoleCreated, _localization.FailedCreateRole);
	}
}
