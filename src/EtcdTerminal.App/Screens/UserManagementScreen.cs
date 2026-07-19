using EtcdTerminal;
using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Modules;
using EtcdTerminal.Models;
using Spectre.Console;

namespace EtcdTerminal.App.Screens;

public sealed class UserManagementScreen(IEtcdClient _etcdClient)
{
	private const string Title = "User Management";
	private const string ListUsers = "List Users";
	private const string CreateUser = "Create User";
	private const string DeleteUser = "Delete User";
	private const string ChangePassword = "Change Password";
	private const string AssignRole = "Assign Role to User";
	private const string RemoveRole = "Remove Role from User";
	private const string NoUsersFound = "[yellow]No users found.[/]";
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

	public async Task ShowAsync(EtcdConnectionConfig config)
	{
		while (true)
		{
			AnsiConsole.Clear();
			ConnectionStatusBar.Render(config);

			var choice = Menu.Show(Title, new[]
			{
				ListUsers,
				CreateUser,
				DeleteUser,
				ChangePassword,
				AssignRole,
				RemoveRole
			});

			if (choice is null)
				break;

			switch (choice)
			{
				case "List Users":
					await ListUsersAsync();
					break;
				case "Create User":
					await CreateUserAsync();
					break;
				case "Delete User":
					await DeleteUserAsync();
					break;
				case "Change Password":
					await ChangePasswordAsync();
					break;
				case "Assign Role to User":
					await AssignRoleAsync();
					break;
				case "Remove Role from User":
					await RevokeRoleAsync();
					break;
			}
		}
	}

	private async Task ListUsersAsync()
	{
		var users = await _etcdClient.GetUsersAsync();

		if (users.Count == 0)
			AnsiConsole.MarkupLine(NoUsersFound);
		else
		{
			var table = new Table();
			table.AddColumn("Username");
			table.AddColumn("Roles");

			foreach (var user in users)
			{
				var roles = user.Roles.Count > 0
					? string.Join(", ", user.Roles)
					: "[grey]none[/]";
				table.AddRow(Markup.Escape(user.Username), roles);
			}

			AnsiConsole.Write(table);
		}

		AnsiConsole.MarkupLine(Prompt.PressAnyKeyMarkup);
		Console.ReadKey(true);
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

		AnsiConsole.MarkupLine(Prompt.PressAnyKeyMarkup);
		Console.ReadKey(true);
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

		AnsiConsole.MarkupLine(Prompt.PressAnyKeyMarkup);
		Console.ReadKey(true);
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

		AnsiConsole.MarkupLine(Prompt.PressAnyKeyMarkup);
		Console.ReadKey(true);
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

		AnsiConsole.MarkupLine(Prompt.PressAnyKeyMarkup);
		Console.ReadKey(true);
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

		AnsiConsole.MarkupLine(Prompt.PressAnyKeyMarkup);
		Console.ReadKey(true);
	}
}
