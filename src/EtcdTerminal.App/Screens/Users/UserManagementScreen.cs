using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.Configuration;
using EtcdTerminal.Localization;
using Spectre.Console;

namespace EtcdTerminal.App.Screens.Users;

public sealed class UserManagementScreen(IEtcdClient _etcdClient)
{
	public async Task ShowAsync(EtcdConnectionConfig config) =>
		await MenuScreen.RunAsync(LocalizationStore.Current.UserManagement, [LocalizationStore.Current.ListUsers, LocalizationStore.Current.CreateUser, LocalizationStore.Current.DeleteUser, LocalizationStore.Current.ChangePassword, LocalizationStore.Current.AssignRole, LocalizationStore.Current.RemoveRole], config, HandleChoiceAsync);

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

		UserListRenderer.Render(users);

		PressAnyKeyPrompt.Show();
	}

	private async Task CreateUserAsync()
	{
		var username = Prompt.Ask(LocalizationStore.Current.EnterUsernamePrompt);

		if (username is null)
			return;

		var password = Prompt.Secret(LocalizationStore.Current.EnterPasswordPrompt);

		if (password is null)
			return;

		var result = await _etcdClient.CreateUserAsync(username, password);

		if (result)
			AnsiConsole.MarkupLine(LocalizationStore.Current.UserCreated);
		else
			AnsiConsole.MarkupLine(LocalizationStore.Current.FailedCreateUser);

		PressAnyKeyPrompt.Show();
	}

	private async Task DeleteUserAsync()
	{
		var username = Prompt.Ask(LocalizationStore.Current.EnterUsernameToDelete);

		if (username is null)
			return;

		var confirm = Prompt.Confirm(string.Format(LocalizationStore.Current.DeleteUserConfirm, username));

		if (confirm is not true)
			return;

		var result = await _etcdClient.DeleteUserAsync(username);

		if (result)
			AnsiConsole.MarkupLine(LocalizationStore.Current.UserDeleted);
		else
			AnsiConsole.MarkupLine(LocalizationStore.Current.FailedDeleteUser);

		PressAnyKeyPrompt.Show();
	}

	private async Task ChangePasswordAsync()
	{
		var username = Prompt.Ask(LocalizationStore.Current.EnterUsernamePrompt);

		if (username is null)
			return;

		var newPassword = Prompt.Secret(LocalizationStore.Current.EnterNewPassword);

		if (newPassword is null)
			return;

		var result = await _etcdClient.ChangeUserPasswordAsync(username, newPassword);

		if (result)
			AnsiConsole.MarkupLine(LocalizationStore.Current.PasswordChanged);
		else
			AnsiConsole.MarkupLine(LocalizationStore.Current.FailedChangePassword);

		PressAnyKeyPrompt.Show();
	}

	private async Task AssignRoleAsync()
	{
		var username = Prompt.Ask(LocalizationStore.Current.EnterUsernamePrompt);

		if (username is null)
			return;

		var roleName = Prompt.Ask(LocalizationStore.Current.EnterRoleName);

		if (roleName is null)
			return;

		try
		{
			await _etcdClient.GrantRoleToUserAsync(username, roleName);
			AnsiConsole.MarkupLine(LocalizationStore.Current.RoleAssigned);
		}
		catch
		{
			AnsiConsole.MarkupLine(LocalizationStore.Current.FailedAssignRole);
		}

		PressAnyKeyPrompt.Show();
	}

	private async Task RevokeRoleAsync()
	{
		var username = Prompt.Ask(LocalizationStore.Current.EnterUsernamePrompt);

		if (username is null)
			return;

		var roleName = Prompt.Ask(LocalizationStore.Current.EnterRoleNameToRemove);

		if (roleName is null)
			return;

		try
		{
			await _etcdClient.RevokeRoleFromUserAsync(username, roleName);
			AnsiConsole.MarkupLine(LocalizationStore.Current.RoleRemoved);
		}
		catch
		{
			AnsiConsole.MarkupLine(LocalizationStore.Current.FailedRemoveRole);
		}

		PressAnyKeyPrompt.Show();
	}
}