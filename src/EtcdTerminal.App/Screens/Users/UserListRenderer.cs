using Spectre.Console;
using EtcdTerminal.Users;
using EtcdTerminal.Localization;

namespace EtcdTerminal.App.Screens.Users;

public static class UserListRenderer
{
	public static void Render(IReadOnlyList<EtcdUser> users)
	{
		if (users.Count == 0)
		{
			AnsiConsole.MarkupLine(LocalizationStore.Current.NoUsersFound);

			return;
		}

		var table = new Table();

		table.AddColumn(LocalizationStore.Current.Username);
		table.AddColumn(LocalizationStore.Current.Roles);

		foreach (var user in users)
		{
			var roles = user.Roles.Count > 0
				? string.Join(", ", user.Roles)
				: $"[grey]{LocalizationStore.Current.None}[/]";

			table.AddRow(Markup.Escape(user.Username), roles);
		}

		AnsiConsole.Write(table);
	}
}