using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Presentation;
using EtcdTerminal.Users;

namespace EtcdTerminal.App.Screens.Users;

/// <summary>
/// Builds the headers and rows of the user page. Literal text and semantic
/// roles only; column widths and the table geometry belong to Infrastructure.
/// </summary>
public sealed class UserListLayout(ILocalization _localization)
{
	public IReadOnlyList<StyledText> Headers() =>
	[
		new StyledText(_localization.Username, TextRole.Accent),
		new StyledText(_localization.Roles, TextRole.Accent)
	];

	public IReadOnlyList<IReadOnlyList<StyledText>> Rows(IReadOnlyList<EtcdUser> users)
	{
		List<IReadOnlyList<StyledText>> rows = [];

		foreach (var user in users)
			rows.Add(Row(user));

		return rows;
	}

	private IReadOnlyList<StyledText> Row(EtcdUser user) =>
	[
		new StyledText(user.Username, TextRole.Primary),
		new StyledText(user.Roles.Count > 0 ? string.Join(", ", user.Roles) : _localization.None, TextRole.Primary)
	];
}
