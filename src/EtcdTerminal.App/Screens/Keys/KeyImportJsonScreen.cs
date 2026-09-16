using System.Text.Json;
using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.Keys;
using EtcdTerminal.Localization;
using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyImportJsonScreen(
	ITerminal _terminal,
	IEtcdKeyStore _keyStore,
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

		_terminal.WriteLine();

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
			_terminal.WriteLine();
			_terminal.WriteIndentedLine(LocalizationStore.Current.ImportCancelled, TerminalColor.Muted);
			_terminal.WriteLine();
			_pressAnyKey.Show();

			return;
		}

		var created = 0;
		var overwritten = 0;
		var failed = 0;

		_terminal.WriteLine();

		var imported = await _spinner.RunAsync(string.Format(LocalizationStore.Current.ImportingKeys, entries.Count), async ct =>
		{
			foreach (var (Key, Value) in entries)
			{
				var existing = await _keyStore.GetKeyAsync(Key, ct);

				if (existing is not null)
				{
					var updated = await _keyStore.UpdateKeyAsync(Key, Value, ct);

					if (updated)
						overwritten++;
					else
						failed++;
				}
				else
				{
					var result = await _keyStore.CreateKeyAsync(Key, Value, ct);

					if (result)
						created++;
					else
						failed++;
				}
			}
		});

		if (!imported)
		{
			_terminal.WriteLine();
			_terminal.WriteIndentedLine(LocalizationStore.Current.ImportCancelled, TerminalColor.Muted);

			_terminal.WriteLine();
			_pressAnyKey.Show();

			return;
		}

		var summary = string.Format(LocalizationStore.Current.ImportResult, created + overwritten, overwritten, failed);

		if (failed > 0)
			_message.ShowWarning(summary);
		else
			_message.ShowSuccess(summary);
	}

	private bool ConfirmImport(IReadOnlyList<KeyValuePair<string, string>> entries)
	{
		_terminal.WriteLine();
		_terminal.WriteIndentedLine(string.Format(LocalizationStore.Current.ImportPreviewTitle, entries.Count));
		_terminal.WriteLine();

		foreach (var (Key, Value) in entries.Take(_previewLimit))
		{
			_terminal.Write(_terminal.Indent + Key);
			_terminal.Write(" = ");
			_terminal.WriteLine(Truncate(Value), TerminalColor.Muted);
		}

		if (entries.Count > _previewLimit)
			_terminal.WriteIndentedLine(string.Format(LocalizationStore.Current.ImportPreviewMore, entries.Count - _previewLimit), TerminalColor.Muted);

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
