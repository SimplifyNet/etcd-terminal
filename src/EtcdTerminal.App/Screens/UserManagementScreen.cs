using EtcdTerminal;
using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Modules;
using EtcdTerminal.Models;
using Spectre.Console;

namespace EtcdTerminal.App.Screens;

public sealed class UserManagementScreen(IEtcdClient _etcdClient)
{
	public async Task ShowAsync(EtcdConnectionConfig config)
	{
		while (true)
		{
			AnsiConsole.Clear();
			StatusBar.Render(config);

			var choice = Menu.Show("User Management", new[]
			{
				"List Users",
				"Create User",
				"Delete User",
				"Change Password",
				"Assign Role to User",
				"Remove Role from User"
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
			AnsiConsole.MarkupLine("[yellow]No users found.[/]");
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

		AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
		Console.ReadKey(true);
	}

	private async Task CreateUserAsync()
	{
		var username = Prompt.Ask("Enter username:");

		if (username is null)
			return;

		var password = Prompt.Secret("Enter password:");

		if (password is null)
			return;

		var result = await _etcdClient.CreateUserAsync(username, password);

		if (result)
			AnsiConsole.MarkupLine("[green]User created successfully![/]");
		else
			AnsiConsole.MarkupLine("[red]Failed to create user.[/]");

		AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
		Console.ReadKey(true);
	}

	private async Task DeleteUserAsync()
	{
		var username = Prompt.Ask("Enter username to delete:");

		if (username is null)
			return;

		var confirm = Prompt.Confirm($"Are you sure you want to delete user {username}?");

		if (confirm is not true)
			return;

		var result = await _etcdClient.DeleteUserAsync(username);

		if (result)
			AnsiConsole.MarkupLine("[green]User deleted successfully![/]");
		else
			AnsiConsole.MarkupLine("[red]Failed to delete user.[/]");

		AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
		Console.ReadKey(true);
	}

	private async Task ChangePasswordAsync()
	{
		var username = Prompt.Ask("Enter username:");

		if (username is null)
			return;

		var newPassword = Prompt.Secret("Enter new password:");

		if (newPassword is null)
			return;

		var result = await _etcdClient.ChangeUserPasswordAsync(username, newPassword);

		if (result)
			AnsiConsole.MarkupLine("[green]Password changed successfully![/]");
		else
			AnsiConsole.MarkupLine("[red]Failed to change password.[/]");

		AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
		Console.ReadKey(true);
	}

	private async Task AssignRoleAsync()
	{
		var username = Prompt.Ask("Enter username:");

		if (username is null)
			return;

		var roleName = Prompt.Ask("Enter role name:");

		if (roleName is null)
			return;

		try
		{
			await _etcdClient.GrantRoleToUserAsync(username, roleName);
			AnsiConsole.MarkupLine("[green]Role assigned successfully![/]");
		}
		catch
		{
			AnsiConsole.MarkupLine("[red]Failed to assign role.[/]");
		}

		AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
		Console.ReadKey(true);
	}

	private async Task RevokeRoleAsync()
	{
		var username = Prompt.Ask("Enter username:");

		if (username is null)
			return;

		var roleName = Prompt.Ask("Enter role name to remove:");

		if (roleName is null)
			return;

		try
		{
			await _etcdClient.RevokeRoleFromUserAsync(username, roleName);
			AnsiConsole.MarkupLine("[green]Role removed successfully![/]");
		}
		catch
		{
			AnsiConsole.MarkupLine("[red]Failed to remove role.[/]");
		}

		AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
		Console.ReadKey(true);
	}
}
