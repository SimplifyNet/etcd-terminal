using EtcdTerminal.App.Components;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Users;

namespace EtcdTerminal.App.Screens.Users.Commands;

public sealed class CreateUserCommand(IEtcdUserAdmin _userAdmin, UserInput _input, Message _message, ILocalization _localization) : IMenuCommand<UserMenuAction>
{
	public UserMenuAction Action => UserMenuAction.CreateUser;

	public async Task ExecuteAsync()
	{
		var username = _input.Ask(_localization.EnterUsernamePrompt);

		if (username is null)
			return;

		var password = _input.Secret(_localization.EnterPasswordPrompt);

		if (password is null)
			return;

		var result = await _userAdmin.CreateUserAsync(username, password);

		_message.ShowResult(result, _localization.UserCreated, _localization.FailedCreateUser);
	}
}
