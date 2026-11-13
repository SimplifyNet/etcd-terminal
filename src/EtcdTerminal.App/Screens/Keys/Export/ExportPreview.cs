using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.Keys;
using EtcdTerminal.Presentation;
using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Screens.Keys.Export;

/// <summary>
/// Shows the names of the keys that would be exported and asks the user to
/// confirm it, with Yes offered as the initial selection.
/// </summary>
public sealed class ExportPreview(Menu _menu, ILocalizationCatalog _localizations)
{
	private const int _previewLimit = 15;

	public bool Confirm(IReadOnlyList<EtcdKeyValue> keys, string prefix)
	{
		IReadOnlyList<Choice<bool>> items =
		[
			new(true, _localizations.Current.Yes),
			new(false, _localizations.Current.No)
		];

		return _menu.Show(null, items, BuildPreview(keys, prefix), selectedId: true)?.Id ?? false;
	}

	private List<Block> BuildPreview(IReadOnlyList<EtcdKeyValue> keys, string prefix)
	{
		List<Block> preview =
		[
			TextBlock.Line(new StyledText(string.Format(_localizations.Current.ExportPreviewTitle, keys.Count), TextRole.Primary)),
			TextBlock.Line(
				new StyledText(_localizations.Current.KeyPrefix + " ", TextRole.Muted),
				new StyledText(prefix.Length is 0 ? _localizations.Current.AllKeys : DisplayText.Sanitize(prefix), TextRole.Accent)),
			TextBlock.Blank()
		];
		List<IReadOnlyList<StyledText>> previewRows = [];

		foreach (var key in keys.Take(_previewLimit))
			previewRows.Add([new StyledText(DisplayText.Sanitize(key.Key), TextRole.Primary)]);

		preview.Add(new TableBlock([], previewRows));

		if (keys.Count > _previewLimit)
			preview.Add(TextBlock.Line(new StyledText(string.Format(_localizations.Current.ExportPreviewMore, keys.Count - _previewLimit), TextRole.Muted)));

		preview.Add(TextBlock.Blank());
		preview.Add(TextBlock.Line(new StyledText(_localizations.Current.ConfirmExport, TextRole.Primary)));

		return preview;
	}
}
