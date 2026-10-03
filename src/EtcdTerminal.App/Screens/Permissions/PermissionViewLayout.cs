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
	public IReadOnlyList<Block> Body(IReadOnlyList<EtcdUser> users, IReadOnlyList<EtcdRole> roles)
	{
		if (users.Count == 0 && roles.Count == 0)
			return [Notice(_localization.NoUsersOrRoles)];

		List<Block> body = [];

		foreach (var user in users)
		{
			body.Add(Title($"{_localization.User}: {user.Username}"));
			body.Add(Permissions(user, roles));
		}

		return body;
	}

	private static TitleBlock Title(string text) =>
		new(new StyledText(text, TextRole.Primary));

	private static TextBlock Notice(string text) =>
		TextBlock.Line(new StyledText(text, TextRole.Warning));

	private Block Permissions(EtcdUser user, IReadOnlyList<EtcdRole> roles)
	{
		List<IReadOnlyList<StyledText>> rows = [];

		if (user.Roles.Count == 0)
		{
			rows.Add(
			[
				new StyledText(_localization.NoRoles, TextRole.Muted),
				new StyledText("-", TextRole.Muted)
			]);

			return new TableBlock(ColumnHeaders(), rows);
		}

		foreach (var roleName in user.Roles)
		{
			var role = roles.FirstOrDefault(candidate => candidate.Name == roleName);
			var permissions = role is not null && role.Permissions.Count > 0
				? string.Join("\n", role.Permissions.Select(permission => PermissionDisplay.For(permission, _localization)))
				: _localization.NoPermissions;

			rows.Add(
			[
				new StyledText(roleName, TextRole.Primary),
				new StyledText(permissions, TextRole.Primary)
			]);
		}

		return new TableBlock(ColumnHeaders(), rows);
	}

	private IReadOnlyList<StyledText> ColumnHeaders() =>
	[
		new StyledText(_localization.Role, TextRole.Muted),
		new StyledText(_localization.Permissions, TextRole.Muted)
	];
}
