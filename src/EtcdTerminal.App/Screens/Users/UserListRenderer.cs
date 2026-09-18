using EtcdTerminal.Terminal;
using EtcdTerminal.Users;
using EtcdTerminal.Localization;

namespace EtcdTerminal.App.Screens.Users;

public static class UserListRenderer
{
	public static void Render(ITerminal terminal, ILocalization localization, IReadOnlyList<EtcdUser> users)
	{
		if (users.Count == 0)
		{
			terminal.WriteIndentedLine(localization.NoUsersFound, TerminalColor.Warning);

			return;
		}

		List<IReadOnlyList<string>> rows = [];

		foreach (var user in users)
		{
			var roles = user.Roles.Count > 0
				? string.Join(", ", user.Roles)
				: localization.None;

			rows.Add([user.Username, roles]);
		}

		terminal.WriteTable(new TableData(
			[localization.Username, localization.Roles],
			rows));
	}
}
