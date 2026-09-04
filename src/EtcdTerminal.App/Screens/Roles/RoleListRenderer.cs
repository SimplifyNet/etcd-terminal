using EtcdTerminal.Terminal;
using EtcdTerminal.Roles;
using EtcdTerminal.Localization;
using Spectre.Console;

namespace EtcdTerminal.App.Screens.Roles;

public static class RoleListRenderer
{
	public static void Render(ITerminal terminal, IReadOnlyList<EtcdRole> roles)
	{
		if (roles.Count == 0)
		{
			terminal.WriteLine(LocalizationStore.Current.NoRolesFound, TerminalColor.Warning);

			return;
		}

		foreach (var role in roles)
		{
			var table = new Table
			{
				Title = new TableTitle($"[bold]Role: {role.Name}[/]")
			};

			table.AddColumn(LocalizationStore.Current.PermissionType);
			table.AddColumn(LocalizationStore.Current.KeyPrefix);

			if (role.Permissions.Count == 0)
				table.AddRow(LocalizationStore.Current.None, LocalizationStore.Current.None);
			else
				foreach (var perm in role.Permissions)
					table.AddRow(perm.Type.ToString(), Markup.Escape(perm.KeyPrefix));

			AnsiConsole.Write(table);
			AnsiConsole.WriteLine();
		}
	}
}
