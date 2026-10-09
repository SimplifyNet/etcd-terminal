using EtcdTerminal.App.Components;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Users;

namespace EtcdTerminal.App.Screens.Users.Commands;

public sealed class DeleteUserCommand(IEtcdUserAdmin _userAdmin, UserInput _input, Message _message, ILocalizationCatalog _localizations) : IMenuCommand<UserMenuAction>
{
	public UserMenuAction Action => UserMenuAction.DeleteUser;

	public async Task ExecuteAsync()
	{
		var username = _input.Ask(_localizations.Current.EnterUsernameToDelete);

		if (username is null)
			return;

		var result = await _userAdmin.DeleteUserAsync(username);

		_message.ShowResult(result, _localizations.Current.UserDeleted, _localizations.Current.FailedDeleteUser);
	}
}
