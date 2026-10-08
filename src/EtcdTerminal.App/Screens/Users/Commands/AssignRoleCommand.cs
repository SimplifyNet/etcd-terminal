using EtcdTerminal.App.Components;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Users;

namespace EtcdTerminal.App.Screens.Users.Commands;

public sealed class AssignRoleCommand(IEtcdUserAdmin _userAdmin, UserInput _input, Message _message, ILocalization _localization) : IMenuCommand<UserMenuAction>
{
	public UserMenuAction Action => UserMenuAction.AssignRole;

	public async Task ExecuteAsync()
	{
		var username = _input.Ask(_localization.EnterUsernamePrompt);

		if (username is null)
			return;

		var roleName = _input.Ask(_localization.EnterRoleName);

		if (roleName is null)
			return;

		var grantResult = await _userAdmin.GrantRoleToUserAsync(username, roleName);

		_message.ShowResult(grantResult, _localization.RoleAssigned, _localization.FailedAssignRole);
	}
}
