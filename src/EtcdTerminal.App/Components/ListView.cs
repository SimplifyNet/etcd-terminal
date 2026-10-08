using EtcdTerminal.Presentation;

namespace EtcdTerminal.App.Components;

/// <summary>
/// The frame of a view-only list page: banner, filter band, one table page, pagination band.
/// </summary>
public sealed class ListView(Screen _screen, BrowseLayout _layout)
{
	public void Reset() => _screen.Reset();

	public FrameModel Frame(IReadOnlyList<StyledText> headers, IReadOnlyList<IReadOnlyList<StyledText>> pageRows,
		string query, int page, int totalPages, int filteredCount, string emptyText, string totalLabel) =>
		new(
		[
			_screen.Banner(),
			_layout.Search(query),
			TextBlock.Blank(),
			pageRows.Count == 0
				? TextBlock.Line(new StyledText(emptyText, TextRole.Muted))
				: new TableBlock(headers, pageRows),
			TextBlock.Blank(),
			_layout.Pagination(page, totalPages, filteredCount, totalLabel)
		]);
}
