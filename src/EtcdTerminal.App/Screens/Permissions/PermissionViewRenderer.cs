using EtcdTerminal.Models;
using Spectre.Console;

namespace EtcdTerminal.App.Screens.Permissions;

public static class PermissionViewRenderer
{
	private const string NoUsersOrRoles = "[yellow]No users or roles found.[/]";

	public static void Render(IReadOnlyList<EtcdUser> users, IReadOnlyList<EtcdRole> roles)
	{
		if (users.Count == 0 && roles.Count == 0)
		{
			AnsiConsole.MarkupLine(NoUsersOrRoles);

			return;
		}

		foreach (var user in users)
		{
			var table = new Table();
			table.Title = new TableTitle($"[bold]User: {user.Username}[/]");
			table.AddColumn("Role");
			table.AddColumn("Permissions");

			if (user.Roles.Count == 0)
				table.AddRow("[grey]no roles[/]", "[grey]-[/]");
			else
			{
				foreach (var roleName in user.Roles)
				{
					var role = roles.FirstOrDefault(r => r.Name == roleName);
					var permissions = role is not null && role.Permissions.Count > 0
						? string.Join("\n", role.Permissions.Select(p => $"{p.Type}: {p.KeyPrefix}"))
						: "[grey]no permissions[/]";

					table.AddRow(Markup.Escape(roleName), permissions);
				}
			}

			AnsiConsole.Write(table);
			AnsiConsole.WriteLine();
		}
	}
}
