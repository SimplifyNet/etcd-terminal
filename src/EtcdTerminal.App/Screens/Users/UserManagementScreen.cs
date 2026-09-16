using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.Configuration;
using EtcdTerminal.Localization;
using EtcdTerminal.Terminal;
using EtcdTerminal.Users;

namespace EtcdTerminal.App.Screens.Users;

public sealed class UserManagementScreen(ITerminal _terminal, IEtcdUserAdmin _userAdmin, MenuScreen _menuScreen, PressAnyKeyPrompt _pressAnyKey, Prompt _prompt, Message _message)
{
	public async Task ShowAsync(EtcdConnectionConfig config) =>
		await _menuScreen.RunAsync<UserMenuAction>(LocalizationStore.Current.UserManagement,
		[
			new(UserMenuAction.ListUsers, LocalizationStore.Current.ListUsers),
			new(UserMenuAction.CreateUser, LocalizationStore.Current.CreateUser),
			new(UserMenuAction.DeleteUser, LocalizationStore.Current.DeleteUser),
			new(UserMenuAction.ChangePassword, LocalizationStore.Current.ChangePassword),
			new(UserMenuAction.AssignRole, LocalizationStore.Current.AssignRole),
			new(UserMenuAction.RemoveRole, LocalizationStore.Current.RemoveRole)
		],
		config,
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
		var users = await _userAdmin.GetUsersAsync();

		UserListRenderer.Render(_terminal, users);

		_terminal.WriteLine();
		_pressAnyKey.Show();
	}

	private async Task CreateUserAsync()
	{
		var username = _prompt.Ask(LocalizationStore.Current.EnterUsernamePrompt);

		if (username is null)
			return;

		var password = _prompt.Secret(LocalizationStore.Current.EnterPasswordPrompt);

		if (password is null)
			return;

		var result = await _userAdmin.CreateUserAsync(username, password);

		_message.ShowResult(result, LocalizationStore.Current.UserCreated, LocalizationStore.Current.FailedCreateUser);
	}

	private async Task DeleteUserAsync()
	{
		var username = _prompt.Ask(LocalizationStore.Current.EnterUsernameToDelete);

		if (username is null)
			return;

		var result = await _userAdmin.DeleteUserAsync(username);

		_message.ShowResult(result, LocalizationStore.Current.UserDeleted, LocalizationStore.Current.FailedDeleteUser);
	}

	private async Task ChangePasswordAsync()
	{
		var username = _prompt.Ask(LocalizationStore.Current.EnterUsernamePrompt);

		if (username is null)
			return;

		var newPassword = _prompt.Secret(LocalizationStore.Current.EnterNewPassword);

		if (newPassword is null)
			return;

		var result = await _userAdmin.ChangeUserPasswordAsync(username, newPassword);

		_message.ShowResult(result, LocalizationStore.Current.PasswordChanged, LocalizationStore.Current.FailedChangePassword);
	}

	private async Task AssignRoleAsync()
	{
		var username = _prompt.Ask(LocalizationStore.Current.EnterUsernamePrompt);

		if (username is null)
			return;

		var roleName = _prompt.Ask(LocalizationStore.Current.EnterRoleName);

		if (roleName is null)
			return;

		try
		{
			await _userAdmin.GrantRoleToUserAsync(username, roleName);

			_message.ShowSuccess(LocalizationStore.Current.RoleAssigned);
		}
		catch
		{
			_message.ShowError(LocalizationStore.Current.FailedAssignRole);
		}
	}

	private async Task RevokeRoleAsync()
	{
		var username = _prompt.Ask(LocalizationStore.Current.EnterUsernamePrompt);

		if (username is null)
			return;

		var roleName = _prompt.Ask(LocalizationStore.Current.EnterRoleNameToRemove);

		if (roleName is null)
			return;

		try
		{
			await _userAdmin.RevokeRoleFromUserAsync(username, roleName);

			_message.ShowSuccess(LocalizationStore.Current.RoleRemoved);
		}
		catch
		{
			_message.ShowError(LocalizationStore.Current.FailedRemoveRole);
		}
	}
}
