using EtcdTerminal.Terminal;
using Spectre.Console;
using EtcdTerminal.Roles;
using EtcdTerminal.Users;
using EtcdTerminal.Localization;

namespace EtcdTerminal.App.Screens.Permissions;

public static class PermissionViewRenderer
{
	public static void Render(ITerminal terminal, IReadOnlyList<EtcdUser> users, IReadOnlyList<EtcdRole> roles)
	{
		if (users.Count == 0 && roles.Count == 0)
		{
			terminal.WriteLine(LocalizationStore.Current.NoUsersOrRoles, TerminalColor.Warning);

			return;
		}

		foreach (var user in users)
		{
			var table = new Table
			{
				Title = new TableTitle($"[bold]User: {user.Username}[/]")
			};

			table.AddColumn(LocalizationStore.Current.Role);
			table.AddColumn(LocalizationStore.Current.Permissions);

			if (user.Roles.Count == 0)
				table.AddRow(LocalizationStore.Current.NoRoles, "-");
			else
				foreach (var roleName in user.Roles)
				{
					var role = roles.FirstOrDefault(r => r.Name == roleName);
					var permissions = role is not null && role.Permissions.Count > 0
						? string.Join("\n", role.Permissions.Select(p => $"{p.Type}: {p.KeyPrefix}"))
						: LocalizationStore.Current.NoPermissions;

					table.AddRow(Markup.Escape(roleName), permissions);
				}

			AnsiConsole.Write(table);
			AnsiConsole.WriteLine();
		}
	}
}
