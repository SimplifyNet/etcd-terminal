using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Presentation;
using EtcdTerminal.Roles;

namespace EtcdTerminal.App.Screens.Roles;

/// <summary>
/// Builds the headers and the flat role/permission rows of the role page.
/// Literal text and semantic roles only; the geometry of the list belongs to
/// Infrastructure.
/// </summary>
public sealed class RoleListLayout(ILocalizationCatalog _localizations)
{
	public string Loading => _localizations.Current.LoadingRoles;

	public string Empty => _localizations.Current.NoRolesFound;

	public string Total => _localizations.Current.TotalPermissions;

	public IReadOnlyList<StyledText> Headers() =>
	[
		new StyledText(_localizations.Current.Role, TextRole.Accent),
		new StyledText(_localizations.Current.Permission, TextRole.Accent)
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
				rows.Add(Row(role.Name, _localizations.Current.NoPermissions));
			else
				foreach (var permission in role.Permissions)
					rows.Add(Row(role.Name, PermissionDisplay.For(permission, _localizations.Current)));
		}

		return rows;
	}

	private static IReadOnlyList<StyledText> Row(string role, string permission) =>
	[
		new StyledText(role, TextRole.Primary),
		new StyledText(permission, TextRole.Primary)
	];
}
