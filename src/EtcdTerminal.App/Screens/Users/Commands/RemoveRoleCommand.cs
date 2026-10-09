using EtcdTerminal.App.Components;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Users;

namespace EtcdTerminal.App.Screens.Users.Commands;

public sealed class RemoveRoleCommand(IEtcdUserAdmin _userAdmin, UserInput _input, Message _message, ILocalizationCatalog _localizations) : IMenuCommand<UserMenuAction>
{
	public UserMenuAction Action => UserMenuAction.RemoveRole;

	public async Task ExecuteAsync()
	{
		var username = _input.Ask(_localizations.Current.EnterUsernamePrompt);

		if (username is null)
			return;

		var roleName = _input.Ask(_localizations.Current.EnterRoleNameToRemove);

		if (roleName is null)
			return;

		var revokeResult = await _userAdmin.RevokeRoleFromUserAsync(username, roleName);

		_message.ShowResult(revokeResult, _localizations.Current.RoleRemoved, _localizations.Current.FailedRemoveRole);
	}
}
