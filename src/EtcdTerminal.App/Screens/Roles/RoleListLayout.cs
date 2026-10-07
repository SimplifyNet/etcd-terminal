using EtcdTerminal.Permissions;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Presentation;
using EtcdTerminal.Roles;

namespace EtcdTerminal.App.Screens.Roles;

/// <summary>
/// Builds the body of the role list: a section title and the permissions of
/// every role. Literal text and semantic roles only; the geometry of the list
/// belongs to Infrastructure.
/// </summary>
public sealed class RoleListLayout(ILocalization _localization)
{
	public IReadOnlyList<Block> Body(IReadOnlyList<EtcdRole> roles)
	{
		if (roles.Count == 0)
			return [Notice(_localization.NoRolesFound)];

		List<Block> body = [];

		foreach (var role in roles)
		{
			body.Add(Title($"{_localization.Role}: {role.Name}"));
			body.Add(Permissions(role));
		}

		return body;
	}

	private static TitleBlock Title(string text) =>
		new(new StyledText(text, TextRole.Primary));

	private static TextBlock Notice(string text) =>
		TextBlock.Line(new StyledText(text, TextRole.Warning));

	private Block Permissions(EtcdRole role)
	{
		List<IReadOnlyList<StyledText>> rows = [];

		if (role.Permissions.Count == 0)
			rows.Add([new StyledText(_localization.NoPermissions, TextRole.Muted)]);
		else
			foreach (var permission in role.Permissions)
				rows.Add([new StyledText(PermissionDisplay.For(permission, _localization), TextRole.Primary)]);

		return new TableBlock([new StyledText(_localization.Permissions, TextRole.Muted)], rows) { IsFramed = true };
	}
}
