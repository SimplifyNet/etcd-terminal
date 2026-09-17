using EtcdTerminal.Terminal;
using EtcdTerminal.Roles;
using EtcdTerminal.Localization;

namespace EtcdTerminal.App.Screens.Roles;

public static class RoleListRenderer
{
	public static void Render(ITerminal terminal, IReadOnlyList<EtcdRole> roles)
	{
		if (roles.Count == 0)
		{
			terminal.WriteIndentedLine(LocalizationStore.Current.NoRolesFound, TerminalColor.Warning);

			return;
		}

		foreach (var role in roles)
		{
			List<IReadOnlyList<string>> rows = [];

			if (role.Permissions.Count == 0)
				rows.Add([LocalizationStore.Current.None, LocalizationStore.Current.None, LocalizationStore.Current.None]);
			else
				foreach (var perm in role.Permissions)
					rows.Add([perm.Type.ToString(), PermissionScopeText.For(perm.Scope), perm.KeyPrefix]);

			terminal.WriteTable(new TableData(
				[LocalizationStore.Current.PermissionType, LocalizationStore.Current.PermissionScope, LocalizationStore.Current.KeyPrefix],
				rows)
			{
				Title = $"Role: {role.Name}"
			});
		}
	}
}
