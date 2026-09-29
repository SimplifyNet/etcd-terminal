using EtcdTerminal.Localization;
using EtcdTerminal.Presentation;
using EtcdTerminal.Users;

namespace EtcdTerminal.App.Screens.Users;

/// <summary>
/// Builds the body of the user list. Literal text and semantic roles only;
/// column widths and the table geometry belong to Infrastructure.
/// </summary>
public sealed class UserListLayout(ILocalization _localization)
{
	public IReadOnlyList<PanelModel> Body(IReadOnlyList<EtcdUser> users)
	{
		if (users.Count == 0)
			return [Notice(_localization.NoUsersFound)];

		List<PanelLine> rows = [Header(_localization.Username, _localization.Roles)];

		foreach (var user in users)
		{
			var roles = user.Roles.Count > 0
				? string.Join(", ", user.Roles)
				: _localization.None;

			rows.Add(new PanelLine(
			[
				new StyledText(user.Username, TextRole.Primary),
				new StyledText(roles, TextRole.Primary)
			]));
		}

		return [new PanelModel(rows, PanelKind.Table)];
	}

	private PanelLine Header(string username, string roles) =>
		new([new StyledText(username, TextRole.Muted), new StyledText(roles, TextRole.Muted)]);

	private PanelModel Notice(string text) =>
		new([new PanelLine([new StyledText(text, TextRole.Warning)])]);
}
