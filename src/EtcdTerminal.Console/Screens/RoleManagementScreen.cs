using EtcdTerminal.Console.Engine;
using EtcdTerminal.Console.Modules;
using EtcdTerminal.Models;
using Spectre.Console;

namespace EtcdTerminal.Console.Screens;

public sealed class RoleManagementScreen(IEtcdClient _etcdClient)
{
	public async Task ShowAsync(EtcdConnectionConfig config)
	{
		while (true)
		{
			AnsiConsole.Clear();
			StatusBar.Render(config);

			var choice = Menu.Show("Role Management", new[]
			{
				"List Roles",
				"Create Role",
				"Delete Role",
				"Grant Permission",
				"Revoke Permission"
			});

			if (choice is null)
				break;

			switch (choice)
			{
				case "List Roles":
					await ListRolesAsync();
					break;
				case "Create Role":
					await CreateRoleAsync();
					break;
				case "Delete Role":
					await DeleteRoleAsync();
					break;
				case "Grant Permission":
					await GrantPermissionAsync();
					break;
				case "Revoke Permission":
					await RevokePermissionAsync();
					break;
			}
		}
	}

	private async Task ListRolesAsync()
	{
		var roles = await _etcdClient.GetRolesAsync();

		if (roles.Count == 0)
			AnsiConsole.MarkupLine("[yellow]No roles found.[/]");
		else
		{
			foreach (var role in roles)
			{
				var table = new Table();
				table.Title = new TableTitle($"[bold]Role: {role.Name}[/]");
				table.AddColumn("Permission Type");
				table.AddColumn("Key Prefix");

				if (role.Permissions.Count == 0)
					table.AddRow("[grey]none[/]", "[grey]none[/]");
				else
				{
					foreach (var perm in role.Permissions)
					{
						table.AddRow(perm.Type.ToString(), Markup.Escape(perm.KeyPrefix));
					}
				}

				AnsiConsole.Write(table);
				AnsiConsole.WriteLine();
			}
		}

		AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
		System.Console.ReadKey(true);
	}

	private async Task CreateRoleAsync()
	{
		var roleName = Prompt.Ask("Enter role name:");

		if (roleName is null)
			return;

		var result = await _etcdClient.CreateRoleAsync(roleName);

		if (result)
			AnsiConsole.MarkupLine("[green]Role created successfully![/]");
		else
			AnsiConsole.MarkupLine("[red]Failed to create role (may already exist).[/]");

		AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
		System.Console.ReadKey(true);
	}

	private async Task DeleteRoleAsync()
	{
		var roleName = Prompt.Ask("Enter role name to delete:");

		if (roleName is null)
			return;

		var confirm = Prompt.Confirm($"Are you sure you want to delete role {roleName}?");

		if (confirm is not true)
			return;

		var result = await _etcdClient.DeleteRoleAsync(roleName);

		if (result)
			AnsiConsole.MarkupLine("[green]Role deleted successfully![/]");
		else
			AnsiConsole.MarkupLine("[red]Failed to delete role.[/]");

		AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
		System.Console.ReadKey(true);
	}

	private async Task GrantPermissionAsync()
	{
		var roleName = Prompt.Ask("Enter role name:");

		if (roleName is null)
			return;

		var keyPrefix = Prompt.Ask("Enter key prefix:");

		if (keyPrefix is null)
			return;

		var permStr = Menu.Show("Select permission type:", new[] { "Read", "Write", "ReadWrite" });

		if (permStr is null)
			return;

		var permType = permStr switch
		{
			"Read" => PermissionType.Read,
			"Write" => PermissionType.Write,
			_ => PermissionType.ReadWrite
		};

		try
		{
			await _etcdClient.GrantPermissionAsync(roleName, permType, keyPrefix);
			AnsiConsole.MarkupLine("[green]Permission granted successfully![/]");
		}
		catch
		{
			AnsiConsole.MarkupLine("[red]Failed to grant permission.[/]");
		}

		AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
		System.Console.ReadKey(true);
	}

	private async Task RevokePermissionAsync()
	{
		var roleName = Prompt.Ask("Enter role name:");

		if (roleName is null)
			return;

		var keyPrefix = Prompt.Ask("Enter key prefix:");

		if (keyPrefix is null)
			return;

		var permStr = Menu.Show("Select permission type:", new[] { "Read", "Write", "ReadWrite" });

		if (permStr is null)
			return;

		var permType = permStr switch
		{
			"Read" => PermissionType.Read,
			"Write" => PermissionType.Write,
			_ => PermissionType.ReadWrite
		};

		try
		{
			await _etcdClient.RevokePermissionAsync(roleName, permType, keyPrefix);
			AnsiConsole.MarkupLine("[green]Permission revoked successfully![/]");
		}
		catch
		{
			AnsiConsole.MarkupLine("[red]Failed to revoke permission.[/]");
		}

		AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
		System.Console.ReadKey(true);
	}
}
