using EtcdTerminal.App.Components;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Users;

namespace EtcdTerminal.App.Screens.Users.Commands;

public sealed class DeleteUserCommand(IEtcdUserAdmin _userAdmin, UserInput _input, Message _message, ILocalization _localization) : IMenuCommand<UserMenuAction>
{
	public UserMenuAction Action => UserMenuAction.DeleteUser;

	public async Task ExecuteAsync()
	{
		var username = _input.Ask(_localization.EnterUsernameToDelete);

		if (username is null)
			return;

		var result = await _userAdmin.DeleteUserAsync(username);

		_message.ShowResult(result, _localization.UserDeleted, _localization.FailedDeleteUser);
	}
}
