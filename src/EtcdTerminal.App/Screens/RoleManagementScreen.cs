using EtcdTerminal;
using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Modules;
using EtcdTerminal.Models;
using Spectre.Console;

namespace EtcdTerminal.App.Screens;

public sealed class RoleManagementScreen(IEtcdClient _etcdClient)
{
	private const string Title = "Role Management";
	private const string ListRoles = "List Roles";
	private const string CreateRole = "Create Role";
	private const string DeleteRole = "Delete Role";
	private const string GrantPermission = "Grant Permission";
	private const string RevokePermission = "Revoke Permission";
	private const string NoRolesFound = "[yellow]No roles found.[/]";
	private const string EnterRoleName = "Enter role name:";
	private const string RoleCreated = "[green]Role created successfully![/]";
	private const string FailedCreateRole = "[red]Failed to create role (may already exist).[/]";
	private const string EnterRoleNameToDelete = "Enter role name to delete:";
	private const string DeleteRoleConfirm = "Are you sure you want to delete role {0}?";
	private const string RoleDeleted = "[green]Role deleted successfully![/]";
	private const string FailedDeleteRole = "[red]Failed to delete role.[/]";
	private const string EnterKeyPrefix = "Enter key prefix:";
	private const string SelectPermissionType = "Select permission type:";
	private const string PermissionGranted = "[green]Permission granted successfully![/]";
	private const string FailedGrantPermission = "[red]Failed to grant permission.[/]";
	private const string PermissionRevoked = "[green]Permission revoked successfully![/]";
	private const string FailedRevokePermission = "[red]Failed to revoke permission.[/]";

	public async Task ShowAsync(EtcdConnectionConfig config)
	{
		while (true)
		{
			AnsiConsole.Clear();
			ConnectionStatusBar.Render(config);

			var choice = Menu.Show(Title, new[]
			{
				ListRoles,
				CreateRole,
				DeleteRole,
				GrantPermission,
				RevokePermission
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
			AnsiConsole.MarkupLine(NoRolesFound);
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

		AnsiConsole.MarkupLine(Prompt.PressAnyKeyMarkup);
		Console.ReadKey(true);
	}

	private async Task CreateRoleAsync()
	{
		var roleName = Prompt.Ask(EnterRoleName);

		if (roleName is null)
			return;

		var result = await _etcdClient.CreateRoleAsync(roleName);

		if (result)
			AnsiConsole.MarkupLine(RoleCreated);
		else
			AnsiConsole.MarkupLine(FailedCreateRole);

		AnsiConsole.MarkupLine(Prompt.PressAnyKeyMarkup);
		Console.ReadKey(true);
	}

	private async Task DeleteRoleAsync()
	{
		var roleName = Prompt.Ask(EnterRoleNameToDelete);

		if (roleName is null)
			return;

		var confirm = Prompt.Confirm(string.Format(DeleteRoleConfirm, roleName));

		if (confirm is not true)
			return;

		var result = await _etcdClient.DeleteRoleAsync(roleName);

		if (result)
			AnsiConsole.MarkupLine(RoleDeleted);
		else
			AnsiConsole.MarkupLine(FailedDeleteRole);

		AnsiConsole.MarkupLine(Prompt.PressAnyKeyMarkup);
		Console.ReadKey(true);
	}

	private async Task GrantPermissionAsync()
	{
		var roleName = Prompt.Ask(EnterRoleName);

		if (roleName is null)
			return;

		var keyPrefix = Prompt.Ask(EnterKeyPrefix);

		if (keyPrefix is null)
			return;

		var permStr = Menu.Show(SelectPermissionType, new[] { "Read", "Write", "ReadWrite" });

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
			AnsiConsole.MarkupLine(PermissionGranted);
		}
		catch
		{
			AnsiConsole.MarkupLine(FailedGrantPermission);
		}

		AnsiConsole.MarkupLine(Prompt.PressAnyKeyMarkup);
		Console.ReadKey(true);
	}

	private async Task RevokePermissionAsync()
	{
		var roleName = Prompt.Ask(EnterRoleName);

		if (roleName is null)
			return;

		var keyPrefix = Prompt.Ask(EnterKeyPrefix);

		if (keyPrefix is null)
			return;

		var permStr = Menu.Show(SelectPermissionType, new[] { "Read", "Write", "ReadWrite" });

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
			AnsiConsole.MarkupLine(PermissionRevoked);
		}
		catch
		{
			AnsiConsole.MarkupLine(FailedRevokePermission);
		}

		AnsiConsole.MarkupLine(Prompt.PressAnyKeyMarkup);
		Console.ReadKey(true);
	}
}
