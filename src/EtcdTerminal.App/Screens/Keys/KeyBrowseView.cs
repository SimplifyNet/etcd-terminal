using EtcdTerminal.App.Components;
using EtcdTerminal.Keys;
using EtcdTerminal.Presentation;
using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Screens.Keys;

/// <summary>
/// Composes the frame of the key browser from the state the control holds.
/// Literal text and semantic roles only; the geometry belongs to Infrastructure.
/// </summary>
public sealed class KeyBrowseView(KeyBrowseLayout _layout, BrowseLayout _browse, Header _header, ILocalization _localization)
{
	public FrameModel Frame(KeyBrowseViewState state, IReadOnlyList<EtcdKeyValue> pageKeys, int totalPages, int totalKeys) =>
		new([_header.BuildModel(), .. Body(state, pageKeys, totalPages, totalKeys)]);

	private IEnumerable<Block> Body(KeyBrowseViewState state, IReadOnlyList<EtcdKeyValue> pageKeys, int totalPages, int totalKeys)
	{
		yield return _browse.Search(state.SearchQuery, showCaret: !state.ShowActions);
		yield return TextBlock.Blank();
		yield return _layout.KeyList(pageKeys, state.SelectedIndex);
		yield return TextBlock.Blank();
		yield return _browse.Pagination(state.CurrentPage, totalPages, totalKeys, _localization.TotalKeys);

		if (!state.ShowActions || state.SelectedKey is null)
			yield break;

		yield return TextBlock.Blank();
		yield return _layout.ActionPanel(state.SelectedKey.Key, state.CanModify);
	}
}
