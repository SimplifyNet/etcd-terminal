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
	public IReadOnlyList<PanelModel> Body(IReadOnlyList<EtcdRole> roles)
	{
		if (roles.Count == 0)
			return [Notice(_localization.NoRolesFound)];

		List<PanelModel> body = [];

		foreach (var role in roles)
		{
			body.Add(Title($"{_localization.Role}: {role.Name}"));
			body.Add(Permissions(role));
		}

		return body;
	}

	private PanelModel Title(string text) =>
		new([new PanelLine([new StyledText(text, TextRole.Primary)])], PanelKind.Title);

	private PanelModel Notice(string text) =>
		new([new PanelLine([new StyledText(text, TextRole.Warning)])]);

	private PanelModel Permissions(EtcdRole role)
	{
		List<PanelLine> rows = [new([new StyledText(_localization.Permissions, TextRole.Muted)])];

		if (role.Permissions.Count == 0)
			rows.Add(new([new StyledText(_localization.NoPermissions, TextRole.Muted)]));
		else
			foreach (var permission in role.Permissions)
				rows.Add(new([new StyledText(PermissionDisplay.For(permission, _localization), TextRole.Primary)]));

		return new PanelModel(rows, PanelKind.Table);
	}
}
