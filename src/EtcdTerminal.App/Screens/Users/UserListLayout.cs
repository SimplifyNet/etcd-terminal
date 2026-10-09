using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Presentation;
using EtcdTerminal.Users;

namespace EtcdTerminal.App.Screens.Users;

/// <summary>
/// Builds the headers and rows of the user page. Literal text and semantic
/// roles only; column widths and the table geometry belong to Infrastructure.
/// </summary>
public sealed class UserListLayout(ILocalizationCatalog _localizations)
{
	public string Loading => _localizations.Current.LoadingUsers;

	public string Empty => _localizations.Current.NoUsersFound;

	public string Total => _localizations.Current.TotalUsers;

	public IReadOnlyList<StyledText> Headers() =>
	[
		new StyledText(_localizations.Current.Username, TextRole.Accent),
		new StyledText(_localizations.Current.Roles, TextRole.Accent)
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
		new StyledText(user.Roles.Count > 0 ? string.Join(", ", user.Roles) : _localizations.Current.None, TextRole.Primary)
	];
}
