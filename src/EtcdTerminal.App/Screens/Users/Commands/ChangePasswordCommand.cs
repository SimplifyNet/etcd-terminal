using EtcdTerminal.App.Components;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Users;

namespace EtcdTerminal.App.Screens.Users.Commands;

public sealed class ChangePasswordCommand(IEtcdUserAdmin _userAdmin, UserInput _input, Message _message, ILocalizationCatalog _localizations) : IMenuCommand<UserMenuAction>
{
	public UserMenuAction Action => UserMenuAction.ChangePassword;

	public async Task ExecuteAsync()
	{
		var username = _input.Ask(_localizations.Current.EnterUsernamePrompt);

		if (username is null)
			return;

		var newPassword = _input.Secret(_localizations.Current.EnterNewPassword);

		if (newPassword is null)
			return;

		var result = await _userAdmin.ChangeUserPasswordAsync(username, newPassword);

		_message.ShowResult(result, _localizations.Current.PasswordChanged, _localizations.Current.FailedChangePassword);
	}
}
