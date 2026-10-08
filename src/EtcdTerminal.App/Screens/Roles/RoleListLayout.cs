using EtcdTerminal.Permissions;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Presentation;
using EtcdTerminal.Roles;

namespace EtcdTerminal.App.Screens.Roles;

/// <summary>
/// Builds the headers and the flat role/permission rows of the role page.
/// Literal text and semantic roles only; the geometry of the list belongs to
/// Infrastructure.
/// </summary>
public sealed class RoleListLayout(ILocalization _localization)
{
	public string Loading => _localization.LoadingRoles;

	public string Empty => _localization.NoRolesFound;

	public string Total => _localization.TotalPermissions;

	public IReadOnlyList<StyledText> Headers() =>
	[
		new StyledText(_localization.Role, TextRole.Accent),
		new StyledText(_localization.Permission, TextRole.Accent)
	];

	/// One row per permission, so the page can be filtered by role or by
	/// permission; a role without permissions still gets its row so it stays
	/// visible and findable.
	public IReadOnlyList<IReadOnlyList<StyledText>> Rows(IReadOnlyList<EtcdRole> roles)
	{
		List<IReadOnlyList<StyledText>> rows = [];

		foreach (var role in roles)
		{
			if (role.Permissions.Count == 0)
				rows.Add(Row(role.Name, _localization.NoPermissions));
			else
				foreach (var permission in role.Permissions)
					rows.Add(Row(role.Name, PermissionDisplay.For(permission, _localization)));
		}

		return rows;
	}

	private static IReadOnlyList<StyledText> Row(string role, string permission) =>
	[
		new StyledText(role, TextRole.Primary),
		new StyledText(permission, TextRole.Primary)
	];
}
