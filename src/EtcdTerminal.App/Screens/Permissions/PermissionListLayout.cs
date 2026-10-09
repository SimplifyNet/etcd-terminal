using EtcdTerminal.App.Screens.Roles;
using EtcdTerminal.Presentation;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Roles;
using EtcdTerminal.Users;

namespace EtcdTerminal.App.Screens.Permissions;

/// <summary>
/// Builds the rows of the permission page: one row per permission of every
/// role a user holds, so the page can be filtered by user, role or
/// permission. A user without roles and a role without permissions still get
/// their row so both stay visible and findable. Literal text and semantic
/// roles only; column widths and the table geometry belong to Infrastructure.
/// </summary>
public sealed class PermissionListLayout(ILocalizationCatalog _localizations)
{
	public string Empty => _localizations.Current.NoPermissionsFound;

	public string Total => _localizations.Current.TotalPermissions;

	public IReadOnlyList<StyledText> Headers() =>
	[
		new StyledText(_localizations.Current.User, TextRole.Accent),
		new StyledText(_localizations.Current.Role, TextRole.Accent),
		new StyledText(_localizations.Current.Permission, TextRole.Accent)
	];

	public IReadOnlyList<IReadOnlyList<StyledText>> Rows(IReadOnlyList<EtcdUser> users, IReadOnlyList<EtcdRole> roles)
	{
		List<IReadOnlyList<StyledText>> rows = [];

		foreach (var user in users)
			rows.AddRange(UserRows(user, roles));

		return rows;
	}

	private IEnumerable<IReadOnlyList<StyledText>> UserRows(EtcdUser user, IReadOnlyList<EtcdRole> roles)
	{
		if (user.Roles.Count == 0)
		{
			yield return Row(user.Username, _localizations.Current.NoRoles, "-");

			yield break;
		}

		foreach (var roleName in user.Roles)
		{
			var role = roles.FirstOrDefault(candidate => candidate.Name == roleName);

			if (role is null || role.Permissions.Count == 0)
				yield return Row(user.Username, roleName, _localizations.Current.NoPermissions);
			else
				foreach (var permission in role.Permissions)
					yield return Row(user.Username, roleName, PermissionDisplay.For(permission, _localizations.Current));
		}
	}

	private static IReadOnlyList<StyledText> Row(string user, string role, string permission) =>
	[
		new StyledText(user, TextRole.Primary),
		new StyledText(role, TextRole.Primary),
		new StyledText(permission, TextRole.Primary)
	];
}
