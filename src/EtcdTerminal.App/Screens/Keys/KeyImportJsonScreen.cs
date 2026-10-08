using System.Text.Json;
using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Screens.MainMenu;
using EtcdTerminal.Keys;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Presentation;
using EtcdTerminal.Security;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyImportJsonScreen(
	IKeyImporter _importer,
	Screen _screen,
	UserInput _input,
	MultiLinePasteReader _pasteReader,
	Menu _menu,
	Spinner _spinner,
	Message _message,
	ILocalization _localization) : IMainMenuEntry
{
	private const int _previewLimit = 15;

	public MainMenuAction Action => MainMenuAction.ImportJson;

	public string Label => _localization.ImportJson;

	public bool IsAvailable(UserCapabilities capabilities) => capabilities.CanWriteKeys;

	public async Task ShowAsync()
	{
		_screen.Open();

		var separator = _input.Ask(_localization.EnterSeparator, ":");

		if (separator is null)
			return;

		var prefix = _input.Ask(_localization.EnterPrefix, allowEmpty: true);

		if (prefix is null)
			return;

		var json = await _pasteReader.ReadAsync(_localization.PasteJson);

		if (json is null)
			return;

		var pastedStatus = TextBlock.Line(
			new StyledText(string.Format(_localization.PastedLines, MultiLinePasteReader.CountLines(json)), TextRole.Accent));

		var entries = ParseEntries(json, prefix, separator);

		if (entries is null)
			return;

		if (!ConfirmImport(entries, pastedStatus))
		{
			_message.ShowWarning(_localization.ImportCancelled);

			return;
		}

		await ImportAsync(entries);
	}

	private IReadOnlyList<KeyValuePair<string, string>>? ParseEntries(string json, string prefix, string separator)
	{
		IReadOnlyList<KeyValuePair<string, string>> entries;

		try
		{
			entries = JsonKeyFlattener.Flatten(json, prefix, separator);
		}
		catch (ArgumentException)
		{
			_message.ShowWarning(_localization.NoKeysInJson);

			return null;
		}
		catch (Exception ex) when (ex is JsonException or InvalidOperationException)
		{
			_message.ShowError(string.Format(_localization.InvalidJson, ex.Message));

			return null;
		}

		if (entries.Count == 0)
		{
			_message.ShowWarning(_localization.NoKeysInJson);

			return null;
		}

		return entries;
	}

	private async Task ImportAsync(IReadOnlyList<KeyValuePair<string, string>> entries)
	{
		KeyImportResult confirmed = default;

		bool completed;

		try
		{
			completed = await _spinner.RunAsync(string.Format(_localization.ImportingKeys, entries.Count), async ct =>
			{
				confirmed = await _importer.ImportAsync(entries, snapshot => confirmed = snapshot, ct);
			});
		}
		catch (EtcdOperationException ex)
		{
			ShowStopped(entries, confirmed, ex);

			return;
		}

		if (!completed)
		{
			ShowCancelled(entries, confirmed);

			return;
		}

		ShowImportSummary(confirmed);
	}

	private void ShowImportSummary(KeyImportResult confirmed)
	{
		var summary = string.Format(_localization.ImportResult, confirmed.Created + confirmed.Overwritten, confirmed.Overwritten, confirmed.Failed);

		if (confirmed.Failed > 0)
			_message.ShowWarning(summary);
		else
			_message.ShowSuccess(summary);
	}

	private void ShowStopped(IReadOnlyList<KeyValuePair<string, string>> entries, KeyImportResult confirmed, EtcdOperationException failure)
	{
		var pendingKey = entries[confirmed.Created + confirmed.Overwritten + confirmed.Failed].Key;
		var outcome = failure.Kind is EtcdOperationFailureKind.Unconfirmed
			? string.Format(_localization.ImportEntryUnconfirmed, pendingKey, failure.Message)
			: string.Format(_localization.ImportEntryFailed, pendingKey, failure.Message);
		var partial = string.Format(_localization.ImportPartialResult, confirmed.Created, confirmed.Overwritten, confirmed.Failed);

		_message.ShowWarning(outcome + "\n" + partial);
	}

	private void ShowCancelled(IReadOnlyList<KeyValuePair<string, string>> entries, KeyImportResult confirmed)
	{
		if (confirmed.Created == 0 && confirmed.Overwritten == 0 && confirmed.Failed == 0)
		{
			_message.ShowWarning(_localization.ImportCancelled);

			return;
		}

		var pendingKey = entries[confirmed.Created + confirmed.Overwritten + confirmed.Failed].Key;
		var unconfirmed = string.Format(_localization.ImportEntryUnconfirmed, pendingKey, _localization.OperationCancelled);
		var partial = string.Format(_localization.ImportPartialResult, confirmed.Created, confirmed.Overwritten, confirmed.Failed);

		_message.ShowWarning(_localization.ImportCancelled + "\n" + unconfirmed + "\n" + partial);
	}

	private bool ConfirmImport(IReadOnlyList<KeyValuePair<string, string>> entries, Block pastedStatus)
	{
		IReadOnlyList<Choice<bool>> items =
		[
			new(true, _localization.Yes),
			new(false, _localization.No)
		];

		return _menu.Show(null, items, BuildPreview(entries, pastedStatus))?.Id ?? false;
	}

	private List<Block> BuildPreview(IReadOnlyList<KeyValuePair<string, string>> entries, Block pastedStatus)
	{
		List<Block> preview =
		[
			pastedStatus,
			TextBlock.Line(new StyledText(string.Format(_localization.ImportPreviewTitle, entries.Count), TextRole.Primary)),
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
			preview.Add(TextBlock.Line(new StyledText(string.Format(_localization.ImportPreviewMore, entries.Count - _previewLimit), TextRole.Muted)));

		preview.Add(TextBlock.Blank());
		preview.Add(TextBlock.Line(new StyledText(_localization.ConfirmImport, TextRole.Primary)));

		return preview;
	}
}
