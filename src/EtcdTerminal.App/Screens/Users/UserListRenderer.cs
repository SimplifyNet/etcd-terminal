using EtcdTerminal.Terminal;
using EtcdTerminal.Users;
using EtcdTerminal.Localization;
using Spectre.Console;

namespace EtcdTerminal.App.Screens.Users;

public static class UserListRenderer
{
	public static void Render(ITerminal terminal, IReadOnlyList<EtcdUser> users)
	{
		if (users.Count == 0)
		{
			terminal.WriteIndentedLine(LocalizationStore.Current.NoUsersFound, TerminalColor.Warning);

			return;
		}

		var table = new Table();

		table.AddColumn(LocalizationStore.Current.Username);
		table.AddColumn(LocalizationStore.Current.Roles);

		foreach (var user in users)
		{
			var roles = user.Roles.Count > 0
				? string.Join(", ", user.Roles)
				: LocalizationStore.Current.None;

			table.AddRow(Markup.Escape(user.Username), roles);
		}

		AnsiConsole.Write(table);
	}
}
