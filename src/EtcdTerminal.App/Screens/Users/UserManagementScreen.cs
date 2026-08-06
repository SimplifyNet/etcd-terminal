using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.Models;
using Spectre.Console;

namespace EtcdTerminal.App.Screens.Users;

public sealed class UserManagementScreen(IEtcdClient _etcdClient)
{
	private const string Title = "User Management";
	private const string ListUsers = "List Users";
	private const string CreateUser = "Create User";
	private const string DeleteUser = "Delete User";
	private const string ChangePassword = "Change Password";
	private const string AssignRole = "Assign Role to User";
	private const string RemoveRole = "Remove Role from User";
	private const string EnterUsername = "Enter username:";
	private const string EnterPassword = "Enter password:";
	private const string UserCreated = "[green]User created successfully![/]";
	private const string FailedCreateUser = "[red]Failed to create user.[/]";
	private const string EnterUsernameToDelete = "Enter username to delete:";
	private const string DeleteUserConfirm = "Are you sure you want to delete user {0}?";
	private const string UserDeleted = "[green]User deleted successfully![/]";
	private const string FailedDeleteUser = "[red]Failed to delete user.[/]";
	private const string EnterNewPassword = "Enter new password:";
	private const string PasswordChanged = "[green]Password changed successfully![/]";
	private const string FailedChangePassword = "[red]Failed to change password.[/]";
	private const string EnterRoleName = "Enter role name:";
	private const string RoleAssigned = "[green]Role assigned successfully![/]";
	private const string FailedAssignRole = "[red]Failed to assign role.[/]";
	private const string EnterRoleNameToRemove = "Enter role name to remove:";
	private const string RoleRemoved = "[green]Role removed successfully![/]";
	private const string FailedRemoveRole = "[red]Failed to remove role.[/]";

	public async Task ShowAsync(EtcdConnectionConfig config) =>
		await MenuScreen.RunAsync(Title, [ListUsers, CreateUser, DeleteUser, ChangePassword, AssignRole, RemoveRole], config, HandleChoiceAsync);

	private async Task HandleChoiceAsync(string choice)
	{
		switch (choice)
		{
			case ListUsers:
				await ListUsersAsync();
				break;
			case CreateUser:
				await CreateUserAsync();
				break;
			case DeleteUser:
				await DeleteUserAsync();
				break;
			case ChangePassword:
				await ChangePasswordAsync();
				break;
			case AssignRole:
				await AssignRoleAsync();
				break;
			case RemoveRole:
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
		var username = Prompt.Ask(EnterUsername);

		if (username is null)
			return;

		var password = Prompt.Secret(EnterPassword);

		if (password is null)
			return;

		var result = await _etcdClient.CreateUserAsync(username, password);

		if (result)
			AnsiConsole.MarkupLine(UserCreated);
		else
			AnsiConsole.MarkupLine(FailedCreateUser);

		PressAnyKeyPrompt.Show();
	}

	private async Task DeleteUserAsync()
	{
		var username = Prompt.Ask(EnterUsernameToDelete);

		if (username is null)
			return;

		var confirm = Prompt.Confirm(string.Format(DeleteUserConfirm, username));

		if (confirm is not true)
			return;

		var result = await _etcdClient.DeleteUserAsync(username);

		if (result)
			AnsiConsole.MarkupLine(UserDeleted);
		else
			AnsiConsole.MarkupLine(FailedDeleteUser);

		PressAnyKeyPrompt.Show();
	}

	private async Task ChangePasswordAsync()
	{
		var username = Prompt.Ask(EnterUsername);

		if (username is null)
			return;

		var newPassword = Prompt.Secret(EnterNewPassword);

		if (newPassword is null)
			return;

		var result = await _etcdClient.ChangeUserPasswordAsync(username, newPassword);

		if (result)
			AnsiConsole.MarkupLine(PasswordChanged);
		else
			AnsiConsole.MarkupLine(FailedChangePassword);

		PressAnyKeyPrompt.Show();
	}

	private async Task AssignRoleAsync()
	{
		var username = Prompt.Ask(EnterUsername);

		if (username is null)
			return;

		var roleName = Prompt.Ask(EnterRoleName);

		if (roleName is null)
			return;

		try
		{
			await _etcdClient.GrantRoleToUserAsync(username, roleName);
			AnsiConsole.MarkupLine(RoleAssigned);
		}
		catch
		{
			AnsiConsole.MarkupLine(FailedAssignRole);
		}

		PressAnyKeyPrompt.Show();
	}

	private async Task RevokeRoleAsync()
	{
		var username = Prompt.Ask(EnterUsername);

		if (username is null)
			return;

		var roleName = Prompt.Ask(EnterRoleNameToRemove);

		if (roleName is null)
			return;

		try
		{
			await _etcdClient.RevokeRoleFromUserAsync(username, roleName);
			AnsiConsole.MarkupLine(RoleRemoved);
		}
		catch
		{
			AnsiConsole.MarkupLine(FailedRemoveRole);
		}

		PressAnyKeyPrompt.Show();
	}
}
