using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.Configuration;
using EtcdTerminal.Localization;
using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Screens.Users;

public sealed class UserManagementScreen(ITerminal _terminal, IEtcdClient _etcdClient, MenuScreen _menuScreen, PressAnyKeyPrompt _pressAnyKey, Prompt _prompt)
{
	public async Task ShowAsync(EtcdConnectionConfig config) =>
		await _menuScreen.RunAsync(LocalizationStore.Current.UserManagement, [LocalizationStore.Current.ListUsers, LocalizationStore.Current.CreateUser, LocalizationStore.Current.DeleteUser, LocalizationStore.Current.ChangePassword, LocalizationStore.Current.AssignRole, LocalizationStore.Current.RemoveRole], config, HandleChoiceAsync);

	private async Task HandleChoiceAsync(string choice)
	{
		switch (choice)
		{
			case var _ when choice == LocalizationStore.Current.ListUsers:
				await ListUsersAsync();
				break;
			case var _ when choice == LocalizationStore.Current.CreateUser:
				await CreateUserAsync();
				break;
			case var _ when choice == LocalizationStore.Current.DeleteUser:
				await DeleteUserAsync();
				break;
			case var _ when choice == LocalizationStore.Current.ChangePassword:
				await ChangePasswordAsync();
				break;
			case var _ when choice == LocalizationStore.Current.AssignRole:
				await AssignRoleAsync();
				break;
			case var _ when choice == LocalizationStore.Current.RemoveRole:
				await RevokeRoleAsync();
				break;
		}
	}

	private async Task ListUsersAsync()
	{
		var users = await _etcdClient.GetUsersAsync();

		UserListRenderer.Render(_terminal, users);

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

		var result = await _etcdClient.CreateUserAsync(username, password);

		_terminal.WriteLine();

		if (result)
			_terminal.WriteIndentedLine(LocalizationStore.Current.UserCreated, TerminalColor.Success);
		else
			_terminal.WriteIndentedLine(LocalizationStore.Current.FailedCreateUser, TerminalColor.Error);

		_terminal.WriteLine();
		_pressAnyKey.Show();
	}

	private async Task DeleteUserAsync()
	{
		var username = _prompt.Ask(LocalizationStore.Current.EnterUsernameToDelete);

		if (username is null)
			return;

		_terminal.WriteLine();

		var confirm = _prompt.Confirm(string.Format(LocalizationStore.Current.DeleteUserConfirm, username));

		if (confirm is not true)
			return;

		var result = await _etcdClient.DeleteUserAsync(username);

		_terminal.WriteLine();

		if (result)
			_terminal.WriteIndentedLine(LocalizationStore.Current.UserDeleted, TerminalColor.Success);
		else
			_terminal.WriteIndentedLine(LocalizationStore.Current.FailedDeleteUser, TerminalColor.Error);

		_terminal.WriteLine();
		_pressAnyKey.Show();
	}

	private async Task ChangePasswordAsync()
	{
		var username = _prompt.Ask(LocalizationStore.Current.EnterUsernamePrompt);

		if (username is null)
			return;

		var newPassword = _prompt.Secret(LocalizationStore.Current.EnterNewPassword);

		if (newPassword is null)
			return;

		var result = await _etcdClient.ChangeUserPasswordAsync(username, newPassword);

		_terminal.WriteLine();

		if (result)
			_terminal.WriteIndentedLine(LocalizationStore.Current.PasswordChanged, TerminalColor.Success);
		else
			_terminal.WriteIndentedLine(LocalizationStore.Current.FailedChangePassword, TerminalColor.Error);

		_terminal.WriteLine();
		_pressAnyKey.Show();
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
			await _etcdClient.GrantRoleToUserAsync(username, roleName);

			_terminal.WriteLine();
			_terminal.WriteIndentedLine(LocalizationStore.Current.RoleAssigned, TerminalColor.Success);
		}
		catch
		{
			_terminal.WriteLine();
			_terminal.WriteIndentedLine(LocalizationStore.Current.FailedAssignRole, TerminalColor.Error);
		}

		_terminal.WriteLine();
		_pressAnyKey.Show();
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
			await _etcdClient.RevokeRoleFromUserAsync(username, roleName);

			_terminal.WriteLine();
			_terminal.WriteIndentedLine(LocalizationStore.Current.RoleRemoved, TerminalColor.Success);
		}
		catch
		{
			_terminal.WriteLine();
			_terminal.WriteIndentedLine(LocalizationStore.Current.FailedRemoveRole, TerminalColor.Error);
		}

		_terminal.WriteLine();
		_pressAnyKey.Show();
	}
}
