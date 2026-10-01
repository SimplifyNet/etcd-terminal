using EtcdTerminal.App.Screens.Roles;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Presentation;
using EtcdTerminal.Roles;
using EtcdTerminal.Users;

namespace EtcdTerminal.App.Screens.Permissions;

/// <summary>
/// Builds the body of the permission view: a section title and a table for
/// every user. It carries literal text and semantic roles only; columns, their
/// widths and the empty-state wording policy belong to the screen and the
/// mapper.
/// </summary>
public sealed class PermissionViewLayout(ILocalization _localization)
{
	public IReadOnlyList<PanelModel> Body(IReadOnlyList<EtcdUser> users, IReadOnlyList<EtcdRole> roles)
	{
		if (users.Count == 0 && roles.Count == 0)
			return [Notice(_localization.NoUsersOrRoles)];

		List<PanelModel> body = [];

		foreach (var user in users)
		{
			body.Add(Title($"{_localization.User}: {user.Username}"));
			body.Add(Permissions(user, roles));
		}

		return body;
	}

	private PanelModel Title(string text) =>
		new([new PanelLine([new StyledText(text, TextRole.Primary)])], PanelKind.Title);

	private PanelModel Notice(string text) =>
		new([new PanelLine([new StyledText(text, TextRole.Warning)])]);

	private PanelModel Permissions(EtcdUser user, IReadOnlyList<EtcdRole> roles)
	{
		List<PanelLine> rows = [Header(_localization.Role, _localization.Permissions)];

		if (user.Roles.Count == 0)
		{
			rows.Add(new PanelLine(
			[
				new StyledText(_localization.NoRoles, TextRole.Muted),
				new StyledText("-", TextRole.Muted)
			]));

			return new PanelModel(rows, PanelKind.Table);
		}

		foreach (var roleName in user.Roles)
		{
			var role = roles.FirstOrDefault(candidate => candidate.Name == roleName);
			var permissions = role is not null && role.Permissions.Count > 0
				? string.Join("\n", role.Permissions.Select(permission => PermissionDisplay.For(permission, _localization)))
				: _localization.NoPermissions;

			rows.Add(new PanelLine(
			[
				new StyledText(roleName, TextRole.Primary),
				new StyledText(permissions, TextRole.Primary)
			]));
		}

		return new PanelModel(rows, PanelKind.Table);
	}

	private PanelLine Header(string role, string permissions) =>
		new([new StyledText(role, TextRole.Muted), new StyledText(permissions, TextRole.Muted)]);
}
