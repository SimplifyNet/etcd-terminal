using System.Globalization;
using EtcdTerminal.Presentation;
using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Components;

/// <summary>
/// The two bands every browsable list shares: the filter line above the table
/// and the pagination line below it. The screen owns the list it shows; this
/// component only composes the literal lines with their semantic roles.
/// </summary>
public sealed class BrowseLayout(ILocalizationCatalog _localizations)
{
	private const string SearchPrefix = "\U0001f50d ";
	private const string Caret = "\u2588";
	private const string PageSeparator = "  \u2022  ";

	public Block Search(string searchQuery, bool showCaret = true)
	{
		List<StyledText> spans = [];

		if (searchQuery.Length == 0)
			spans.Add(new(_localizations.Current.TypeToSearch, TextRole.Muted));
		else
		{
			spans.Add(new(SearchPrefix, TextRole.Muted));
			spans.Add(new(DisplayText.Sanitize(searchQuery), TextRole.Primary));
		}

		if (showCaret)
			spans.Add(new(Caret, TextRole.Primary));

		return TextBlock.Line(spans) with { Band = true };
	}

	public Block Pagination(int currentPage, int totalPages, int totalItems, string totalLabel) =>
		TextBlock.Line(
			new StyledText($"{_localizations.Current.Page} ", TextRole.Muted),
			new StyledText($"{currentPage + 1}/{totalPages}", TextRole.Primary),
			new StyledText(PageSeparator, TextRole.Muted),
			new StyledText(totalItems.ToString(CultureInfo.InvariantCulture), TextRole.Primary),
			new StyledText($" {totalLabel}", TextRole.Muted)) with { Band = true };
}
