using EtcdTerminal.App.Components;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Roles;

namespace EtcdTerminal.App.Screens.Roles.Commands;

public sealed class CreateRoleCommand(IEtcdRoleAdmin _roleAdmin, UserInput _input, Message _message, ILocalizationCatalog _localizations) : IMenuCommand<RoleMenuAction>
{
	public RoleMenuAction Action => RoleMenuAction.CreateRole;

	public async Task ExecuteAsync()
	{
		var roleName = _input.Ask(_localizations.Current.EnterRoleNamePrompt);

		if (roleName is null)
			return;

		var result = await _roleAdmin.CreateRoleAsync(roleName);

		_message.ShowResult(result, _localizations.Current.RoleCreated, _localizations.Current.FailedCreateRole);
	}
}
