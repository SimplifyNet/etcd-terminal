using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Presentation;
using EtcdTerminal.Users;

namespace EtcdTerminal.App.Screens.Users;

/// <summary>
/// Builds the body of the user list. Literal text and semantic roles only;
/// column widths and the table geometry belong to Infrastructure.
/// </summary>
public sealed class UserListLayout(ILocalization _localization)
{
	public IReadOnlyList<Block> Body(IReadOnlyList<EtcdUser> users)
	{
		if (users.Count == 0)
			return [Notice(_localization.NoUsersFound)];

		List<IReadOnlyList<StyledText>> rows = [];

		foreach (var user in users)
		{
			var roles = user.Roles.Count > 0
				? string.Join(", ", user.Roles)
				: _localization.None;

			rows.Add(
			[
				new StyledText(user.Username, TextRole.Primary),
				new StyledText(roles, TextRole.Primary)
			]);
		}

		return [new TableBlock(ColumnHeaders(), rows) { IsFramed = true }];
	}

	private IReadOnlyList<StyledText> ColumnHeaders() =>
	[
		new StyledText(_localization.Username, TextRole.Muted),
		new StyledText(_localization.Roles, TextRole.Muted)
	];

	private static TextBlock Notice(string text) =>
		TextBlock.Line(new StyledText(text, TextRole.Warning));
}
