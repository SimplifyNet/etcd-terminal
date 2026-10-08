using System.Globalization;
using EtcdTerminal.Presentation;
using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Components;

/// <summary>
/// The two bands every browsable list shares: the filter line above the table
/// and the pagination line below it. The screen owns the list it shows; this
/// component only composes the literal lines with their semantic roles.
/// </summary>
public sealed class BrowseLayout(ILocalization _localization)
{
	private const string SearchPrefix = "\U0001f50d ";
	private const string Caret = "\u2588";
	private const string PageSeparator = "  \u2022  ";

	public Block Search(string searchQuery) =>
		(searchQuery.Length == 0
			? TextBlock.Line(new StyledText(_localization.TypeToSearch, TextRole.Muted), new StyledText(Caret, TextRole.Primary))
			: TextBlock.Line(
				new StyledText(SearchPrefix, TextRole.Muted),
				new StyledText(DisplayText.Sanitize(searchQuery), TextRole.Primary),
				new StyledText(Caret, TextRole.Primary))) with { Band = true };

	public Block Pagination(int currentPage, int totalPages, int totalItems, string totalLabel) =>
		TextBlock.Line(
			new StyledText($"{_localization.Page} ", TextRole.Muted),
			new StyledText($"{currentPage + 1}/{totalPages}", TextRole.Primary),
			new StyledText(PageSeparator, TextRole.Muted),
			new StyledText(totalItems.ToString(CultureInfo.InvariantCulture), TextRole.Primary),
			new StyledText($" {totalLabel}", TextRole.Muted)) with { Band = true };
}
