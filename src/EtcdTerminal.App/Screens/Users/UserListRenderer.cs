using EtcdTerminal.Models;
using Spectre.Console;

namespace EtcdTerminal.App.Screens.Users;

public static class UserListRenderer
{
	private const string NoUsersFound = "[yellow]No users found.[/]";

	public static void Render(IReadOnlyList<EtcdUser> users)
	{
		if (users.Count == 0)
		{
			AnsiConsole.MarkupLine(NoUsersFound);

			return;
		}

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
}
