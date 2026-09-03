using Spectre.Console;
using EtcdTerminal.Roles;
using EtcdTerminal.Localization;

namespace EtcdTerminal.App.Screens.Roles;

public static class RoleListRenderer
{
	public static void Render(IReadOnlyList<EtcdRole> roles)
	{
		if (roles.Count == 0)
		{
			AnsiConsole.MarkupLine(LocalizationStore.Current.NoRolesFound);

			return;
		}

		foreach (var role in roles)
		{
			var table = new Table();
			table.Title = new TableTitle($"[bold]Role: {role.Name}[/]");
			table.AddColumn(LocalizationStore.Current.PermissionType);
			table.AddColumn(LocalizationStore.Current.KeyPrefix);

			if (role.Permissions.Count == 0)
				table.AddRow($"[grey]{LocalizationStore.Current.None}[/]", $"[grey]{LocalizationStore.Current.None}[/]");
			else
				foreach (var perm in role.Permissions)
					table.AddRow(perm.Type.ToString(), Markup.Escape(perm.KeyPrefix));

			AnsiConsole.Write(table);
			AnsiConsole.WriteLine();
		}
	}
}