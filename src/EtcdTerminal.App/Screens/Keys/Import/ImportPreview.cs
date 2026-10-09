using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.Presentation;
using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Screens.Keys.Import;

/// <summary>
/// Shows what the import would write and asks the user to confirm it.
/// </summary>
public sealed class ImportPreview(Menu _menu, Message _message, ILocalizationCatalog _localizations)
{
	private const int _previewLimit = 15;

	public bool Confirm(IReadOnlyList<KeyValuePair<string, string>> entries, string json)
	{
		var pastedStatus = TextBlock.Line(
			new StyledText(string.Format(_localizations.Current.PastedLines, MultiLinePasteReader.CountLines(json)), TextRole.Accent));

		if (!ConfirmImport(entries, pastedStatus))
		{
			_message.ShowWarning(_localizations.Current.ImportCancelled);

			return false;
		}

		return true;
	}

	private bool ConfirmImport(IReadOnlyList<KeyValuePair<string, string>> entries, Block pastedStatus)
	{
		IReadOnlyList<Choice<bool>> items =
		[
			new(true, _localizations.Current.Yes),
			new(false, _localizations.Current.No)
		];

		return _menu.Show(null, items, BuildPreview(entries, pastedStatus))?.Id ?? false;
	}

	private List<Block> BuildPreview(IReadOnlyList<KeyValuePair<string, string>> entries, Block pastedStatus)
	{
		List<Block> preview =
		[
			pastedStatus,
			TextBlock.Line(new StyledText(string.Format(_localizations.Current.ImportPreviewTitle, entries.Count), TextRole.Primary)),
			TextBlock.Blank()
		];
		List<IReadOnlyList<StyledText>> previewRows = [];

		foreach (var (Key, Value) in entries.Take(_previewLimit))
			previewRows.Add(
			[
				new StyledText(Key, TextRole.Primary),
				new StyledText(DisplayText.Sanitize(Value), TextRole.Muted)
			]);

		preview.Add(new TableBlock([], previewRows));

		if (entries.Count > _previewLimit)
			preview.Add(TextBlock.Line(new StyledText(string.Format(_localizations.Current.ImportPreviewMore, entries.Count - _previewLimit), TextRole.Muted)));

		preview.Add(TextBlock.Blank());
		preview.Add(TextBlock.Line(new StyledText(_localizations.Current.ConfirmImport, TextRole.Primary)));

		return preview;
	}
}
