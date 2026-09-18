using EtcdTerminal.Terminal;
using EtcdTerminal.Roles;
using EtcdTerminal.Localization;

namespace EtcdTerminal.App.Screens.Roles;

public static class RoleListRenderer
{
	public static void Render(ITerminal terminal, ILocalization localization, IReadOnlyList<EtcdRole> roles)
	{
		if (roles.Count == 0)
		{
			terminal.WriteIndentedLine(localization.NoRolesFound, TerminalColor.Warning);

			return;
		}

		foreach (var role in roles)
		{
			List<IReadOnlyList<string>> rows = [];

			if (role.Permissions.Count == 0)
				rows.Add([localization.None, localization.None, localization.None]);
			else
				foreach (var perm in role.Permissions)
					rows.Add([perm.Type.ToString(), PermissionScopeText.For(perm.Scope, localization), perm.KeyPrefix]);

			terminal.WriteTable(new TableData(
				[localization.PermissionType, localization.PermissionScope, localization.KeyPrefix],
				rows)
			{
				Title = $"Role: {role.Name}"
			});
		}
	}
}
