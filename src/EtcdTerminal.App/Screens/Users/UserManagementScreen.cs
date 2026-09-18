using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.App.Screens;
using EtcdTerminal.Localization;
using EtcdTerminal.Security;
using EtcdTerminal.Terminal;
using EtcdTerminal.Users;

namespace EtcdTerminal.App.Screens.Users;

public sealed class UserManagementScreen(ITerminal _terminal, IEtcdUserAdmin _userAdmin, MenuScreen _menuScreen, PressAnyKeyPrompt _pressAnyKey, Prompt _prompt, 	Spinner _spinner, Message _message, ILocalization _localization) : IMainMenuEntry
{
	public MainMenuAction Action => MainMenuAction.ManageUsers;

	public string Label => _localization.ManageUsers;

	public bool IsAvailable(UserCapabilities capabilities) => capabilities.CanManageAuth;
	public async Task ShowAsync() =>
		await _menuScreen.RunAsync<UserMenuAction>(_localization.UserManagement,
		[
			new(UserMenuAction.ListUsers, _localization.ListUsers),
			new(UserMenuAction.CreateUser, _localization.CreateUser),
			new(UserMenuAction.DeleteUser, _localization.DeleteUser),
			new(UserMenuAction.ChangePassword, _localization.ChangePassword),
			new(UserMenuAction.AssignRole, _localization.AssignRole),
			new(UserMenuAction.RemoveRole, _localization.RemoveRole)
		],
		HandleChoiceAsync);

	private async Task HandleChoiceAsync(UserMenuAction action)
	{
		switch (action)
		{
			case UserMenuAction.ListUsers:
				await ListUsersAsync();
				break;
			case UserMenuAction.CreateUser:
				await CreateUserAsync();
				break;
			case UserMenuAction.DeleteUser:
				await DeleteUserAsync();
				break;
			case UserMenuAction.ChangePassword:
				await ChangePasswordAsync();
				break;
			case UserMenuAction.AssignRole:
				await AssignRoleAsync();
				break;
			case UserMenuAction.RemoveRole:
				await RevokeRoleAsync();
				break;
		}
	}

	private async Task ListUsersAsync()
	{
		IReadOnlyList<EtcdUser> users = [];

		var loaded = await _spinner.RunAsync(_localization.LoadingUsers, async ct =>
		{
			users = await _userAdmin.GetUsersAsync(ct);
		});

		if (!loaded)
		{
			_message.ShowWarning(_localization.OperationCancelled);

			return;
		}

		UserListRenderer.Render(_terminal, _localization, users);

		_terminal.WriteLine();
		_pressAnyKey.Show();
	}

	private async Task CreateUserAsync()
	{
		var username = _prompt.Ask(_localization.EnterUsernamePrompt);

		if (username is null)
			return;

		var password = _prompt.Secret(_localization.EnterPasswordPrompt);

		if (password is null)
			return;

		var result = await _userAdmin.CreateUserAsync(username, password);

		_message.ShowResult(result.Success, _localization.UserCreated, result.ErrorMessage ?? _localization.FailedCreateUser);
	}

	private async Task DeleteUserAsync()
	{
		var username = _prompt.Ask(_localization.EnterUsernameToDelete);

		if (username is null)
			return;

		var result = await _userAdmin.DeleteUserAsync(username);

		_message.ShowResult(result.Success, _localization.UserDeleted, result.ErrorMessage ?? _localization.FailedDeleteUser);
	}

	private async Task ChangePasswordAsync()
	{
		var username = _prompt.Ask(_localization.EnterUsernamePrompt);

		if (username is null)
			return;

		var newPassword = _prompt.Secret(_localization.EnterNewPassword);

		if (newPassword is null)
			return;

		var result = await _userAdmin.ChangeUserPasswordAsync(username, newPassword);

		_message.ShowResult(result.Success, _localization.PasswordChanged, result.ErrorMessage ?? _localization.FailedChangePassword);
	}

	private async Task AssignRoleAsync()
	{
		var username = _prompt.Ask(_localization.EnterUsernamePrompt);

		if (username is null)
			return;

		var roleName = _prompt.Ask(_localization.EnterRoleName);

		if (roleName is null)
			return;

		var grantResult = await _userAdmin.GrantRoleToUserAsync(username, roleName);

		_message.ShowResult(grantResult.Success, _localization.RoleAssigned, grantResult.ErrorMessage ?? _localization.FailedAssignRole);
	}

	private async Task RevokeRoleAsync()
	{
		var username = _prompt.Ask(_localization.EnterUsernamePrompt);

		if (username is null)
			return;

		var roleName = _prompt.Ask(_localization.EnterRoleNameToRemove);

		if (roleName is null)
			return;

		var revokeResult = await _userAdmin.RevokeRoleFromUserAsync(username, roleName);

		_message.ShowResult(revokeResult.Success, _localization.RoleRemoved, revokeResult.ErrorMessage ?? _localization.FailedRemoveRole);
	}
}
