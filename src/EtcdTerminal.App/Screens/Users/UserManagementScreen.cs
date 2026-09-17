using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.Localization;
using EtcdTerminal.Terminal;
using EtcdTerminal.Users;

namespace EtcdTerminal.App.Screens.Users;

public sealed class UserManagementScreen(ITerminal _terminal, IEtcdUserAdmin _userAdmin, MenuScreen _menuScreen, PressAnyKeyPrompt _pressAnyKey, Prompt _prompt, Spinner _spinner, Message _message)
{
	public async Task ShowAsync() =>
		await _menuScreen.RunAsync<UserMenuAction>(LocalizationStore.Current.UserManagement,
		[
			new(UserMenuAction.ListUsers, LocalizationStore.Current.ListUsers),
			new(UserMenuAction.CreateUser, LocalizationStore.Current.CreateUser),
			new(UserMenuAction.DeleteUser, LocalizationStore.Current.DeleteUser),
			new(UserMenuAction.ChangePassword, LocalizationStore.Current.ChangePassword),
			new(UserMenuAction.AssignRole, LocalizationStore.Current.AssignRole),
			new(UserMenuAction.RemoveRole, LocalizationStore.Current.RemoveRole)
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

		var loaded = await _spinner.RunAsync(LocalizationStore.Current.LoadingUsers, async ct =>
		{
			users = await _userAdmin.GetUsersAsync(ct);
		});

		if (!loaded)
		{
			_message.ShowWarning(LocalizationStore.Current.OperationCancelled);

			return;
		}

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

		_message.ShowResult(result.Success, LocalizationStore.Current.UserCreated, result.ErrorMessage ?? LocalizationStore.Current.FailedCreateUser);
	}

	private async Task DeleteUserAsync()
	{
		var username = _prompt.Ask(LocalizationStore.Current.EnterUsernameToDelete);

		if (username is null)
			return;

		var result = await _userAdmin.DeleteUserAsync(username);

		_message.ShowResult(result.Success, LocalizationStore.Current.UserDeleted, result.ErrorMessage ?? LocalizationStore.Current.FailedDeleteUser);
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

		_message.ShowResult(result.Success, LocalizationStore.Current.PasswordChanged, result.ErrorMessage ?? LocalizationStore.Current.FailedChangePassword);
	}

	private async Task AssignRoleAsync()
	{
		var username = _prompt.Ask(LocalizationStore.Current.EnterUsernamePrompt);

		if (username is null)
			return;

		var roleName = _prompt.Ask(LocalizationStore.Current.EnterRoleName);

		if (roleName is null)
			return;

		var grantResult = await _userAdmin.GrantRoleToUserAsync(username, roleName);

		_message.ShowResult(grantResult.Success, LocalizationStore.Current.RoleAssigned, grantResult.ErrorMessage ?? LocalizationStore.Current.FailedAssignRole);
	}

	private async Task RevokeRoleAsync()
	{
		var username = _prompt.Ask(LocalizationStore.Current.EnterUsernamePrompt);

		if (username is null)
			return;

		var roleName = _prompt.Ask(LocalizationStore.Current.EnterRoleNameToRemove);

		if (roleName is null)
			return;

		var revokeResult = await _userAdmin.RevokeRoleFromUserAsync(username, roleName);

		_message.ShowResult(revokeResult.Success, LocalizationStore.Current.RoleRemoved, revokeResult.ErrorMessage ?? LocalizationStore.Current.FailedRemoveRole);
	}
}
