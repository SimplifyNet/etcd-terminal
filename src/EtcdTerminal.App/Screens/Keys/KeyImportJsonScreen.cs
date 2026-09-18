using System.Text.Json;
using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.Keys;
using EtcdTerminal.Localization;
using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyImportJsonScreen(
	ITerminalOutput _output,
	ITerminalStyle _style,
	IKeyImporter _importer,
	ScreenLayout _screenLayout,
	PressAnyKeyPrompt _pressAnyKey,
	Prompt _prompt,
	MultiLinePasteReader _pasteReader,
	Menu _menu,
	Spinner _spinner,
	Message _message)
{
	private const int _previewLimit = 15;
	private const int _previewValueLength = 60;

	public async Task ShowAsync()
	{
		_screenLayout.RenderHeader();

		var separator = _prompt.Ask(LocalizationStore.Current.EnterSeparator, ":") ?? ":";
		var prefix = _prompt.Ask(LocalizationStore.Current.EnterPrefix) ?? "";

		_output.WriteLine();

		var json = await _pasteReader.ReadAsync(LocalizationStore.Current.PasteJson);

		if (json is null)
			return;

		IReadOnlyList<KeyValuePair<string, string>> entries;

		try
		{
			entries = JsonKeyFlattener.Flatten(json, prefix, separator);
		}
		catch (ArgumentException)
		{
			_message.ShowWarning(LocalizationStore.Current.NoKeysInJson);

			return;
		}
		catch (Exception ex) when (ex is JsonException or InvalidOperationException)
		{
			_message.ShowError(string.Format(LocalizationStore.Current.InvalidJson, ex.Message));

			return;
		}

		if (entries.Count == 0)
		{
			_message.ShowWarning(LocalizationStore.Current.NoKeysInJson);

			return;
		}

		if (!ConfirmImport(entries))
		{
			_output.WriteLine();
			_output.WriteIndentedLine(LocalizationStore.Current.ImportCancelled, TerminalColor.Muted);
			_output.WriteLine();
			_pressAnyKey.Show();

			return;
		}

		KeyImportResult result = default;

		_output.WriteLine();

		var imported = await _spinner.RunAsync(string.Format(LocalizationStore.Current.ImportingKeys, entries.Count), async ct =>
		{
			result = await _importer.ImportAsync(entries, ct);
		});

		if (!imported)
		{
			_output.WriteLine();
			_output.WriteIndentedLine(LocalizationStore.Current.ImportCancelled, TerminalColor.Muted);

			_output.WriteLine();
			_pressAnyKey.Show();

			return;
		}

		var summary = string.Format(LocalizationStore.Current.ImportResult, result.Created + result.Overwritten, result.Overwritten, result.Failed);

		if (result.Failed > 0)
			_message.ShowWarning(summary);
		else
			_message.ShowSuccess(summary);
	}

	private bool ConfirmImport(IReadOnlyList<KeyValuePair<string, string>> entries)
	{
		_output.WriteLine();
		_output.WriteIndentedLine(string.Format(LocalizationStore.Current.ImportPreviewTitle, entries.Count));
		_output.WriteLine();

		foreach (var (Key, Value) in entries.Take(_previewLimit))
		{
			_output.Write(_style.Indent + Key);
			_output.Write(" = ");
			_output.WriteLine(Truncate(Value), TerminalColor.Muted);
		}

		if (entries.Count > _previewLimit)
			_output.WriteIndentedLine(string.Format(LocalizationStore.Current.ImportPreviewMore, entries.Count - _previewLimit), TerminalColor.Muted);

		bool? confirmed = _menu.Show<bool>(
			LocalizationStore.Current.ConfirmImport,
			[
				new(true, LocalizationStore.Current.Yes),
				new(false, LocalizationStore.Current.No)
			])?.Id;

		return confirmed ?? false;
	}

	private static string Truncate(string value)
	{
		var single = value.ReplaceLineEndings(" ");

		return single.Length <= _previewValueLength ? single : single[.._previewValueLength] + "...";
	}
}
