using Spectre.Console;
using EtcdTerminal.Roles;

namespace EtcdTerminal.App.Screens.Roles;

public static class RoleListRenderer
{
	private const string NoRolesFound = "[yellow]No roles found.[/]";

	public static void Render(IReadOnlyList<EtcdRole> roles)
	{
		if (roles.Count == 0)
		{
			AnsiConsole.MarkupLine(NoRolesFound);

			return;
		}

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
					table.AddRow(perm.Type.ToString(), Markup.Escape(perm.KeyPrefix));
			}

			AnsiConsole.Write(table);
			AnsiConsole.WriteLine();
		}
	}
}
