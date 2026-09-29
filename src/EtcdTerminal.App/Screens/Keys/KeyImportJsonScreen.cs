using System.Text.Json;
using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Screens;
using EtcdTerminal.Configuration;
using EtcdTerminal.Keys;
using EtcdTerminal.Localization;
using EtcdTerminal.Presentation;
using EtcdTerminal.Security;
using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyImportJsonScreen(
	ITerminalOutput _output,
	ITerminalStyle _style,
	IKeyImporter _importer,
	ScreenShell _shell,
	Prompt _prompt,
	MultiLinePasteReader _pasteReader,
	Menu _menu,
	Spinner _spinner,
	Message _message,
	ILocalization _localization,
	IAppSettingsStore _settings) : IMainMenuEntry
{
	private const int _previewLimit = 15;
	private const int _previewValueLength = 60;

	public MainMenuAction Action => MainMenuAction.ImportJson;

	public string Label => _localization.ImportJson;

	public bool IsAvailable(UserCapabilities capabilities) => capabilities.CanWriteKeys;

	public async Task ShowAsync()
	{
		_shell.Show();

		var trim = _settings.Current.TrimInputValues;
		var separator = _prompt.Ask(_localization.EnterSeparator, ":", trim: trim);

		if (separator is null)
			return;

		var prefix = _prompt.Ask(_localization.EnterPrefix, allowEmpty: true, trim: trim);

		if (prefix is null)
			return;

		_output.WriteLine();

		var json = await _pasteReader.ReadAsync(_localization.PasteJson);

		if (json is null)
			return;

		IReadOnlyList<KeyValuePair<string, string>> entries;

		try
		{
			entries = JsonKeyFlattener.Flatten(json, prefix, separator);
		}
		catch (ArgumentException)
		{
			_message.ShowWarning(_localization.NoKeysInJson);

			return;
		}
		catch (Exception ex) when (ex is JsonException or InvalidOperationException)
		{
			_message.ShowError(string.Format(_localization.InvalidJson, ex.Message));

			return;
		}

		if (entries.Count == 0)
		{
			_message.ShowWarning(_localization.NoKeysInJson);

			return;
		}

		if (!ConfirmImport(entries))
		{
			_message.ShowWarning(_localization.ImportCancelled);

			return;
		}

		KeyImportResult confirmed = default;

		_output.WriteLine();

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

	private bool ConfirmImport(IReadOnlyList<KeyValuePair<string, string>> entries)
	{
		// The preview, the question and the choices must stay on one screen, so
		// they share panels instead of growing one panel per line.
		List<PanelLine> preview =
		[
			new([new StyledText(string.Format(_localization.ImportPreviewTitle, entries.Count), TextRole.Primary)]),
			new([])
		];

		foreach (var (Key, Value) in entries.Take(_previewLimit))
		{
			preview.Add(new PanelLine(
			[
				new StyledText(_style.Indent + Key + " = ", TextRole.Primary),
				new StyledText(ValuePreview.Preview(Value, _previewValueLength), TextRole.Muted)
			]));
		}

		if (entries.Count > _previewLimit)
			preview.Add(new([new StyledText(string.Format(_localization.ImportPreviewMore, entries.Count - _previewLimit), TextRole.Muted)]));

		preview.Add(new([]));
		preview.Add(new([new StyledText(_localization.ConfirmImport, TextRole.Primary)]));

		bool? confirmed = _menu.ShowFramed<bool>(
			string.Empty,
			[
				new(true, _localization.Yes),
				new(false, _localization.No)
			],
			notices: [new PanelModel(preview)])?.Id;

		return confirmed ?? false;
	}
}
