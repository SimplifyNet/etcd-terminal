using System.Text.Json;
using System.Text.Json.Nodes;
using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.Configuration;
using EtcdTerminal.Localization;
using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyImportJsonScreen(
	ITerminal _terminal,
	IEtcdClient _etcdClient,
	ScreenLayout _screenLayout,
	PressAnyKeyPrompt _pressAnyKey,
	Prompt _prompt,
	Menu _menu,
	Spinner _spinner)
{
	private const int _previewLimit = 15;
	private const int _previewValueLength = 60;

	public async Task ShowAsync(EtcdConnectionConfig config)
	{
		_screenLayout.RenderHeader(config);

		var separator = _prompt.Ask(LocalizationStore.Current.EnterSeparator, ":") ?? ":";
		var prefix = _prompt.Ask(LocalizationStore.Current.EnterPrefix) ?? "";

		_terminal.WriteLine();

		var json = _prompt.ReadMultiLine(LocalizationStore.Current.PasteJson);

		if (json is null)
			return;

		var sanitized = SanitizeJson(json);
		JsonNode? node;

		try
		{
			node = JsonNode.Parse(sanitized);
		}
		catch (Exception ex) when (ex is JsonException or InvalidOperationException)
		{
			_terminal.WriteIndentedLine(string.Format(LocalizationStore.Current.InvalidJson, ex.Message), TerminalColor.Error);
			_terminal.WriteLine();
			_pressAnyKey.Show();

			return;
		}

		var entries = new List<(string Key, string Value)>();

		if (node is JsonObject obj)
			FlattenJson(obj, prefix, separator, entries);
		else if (node is JsonArray arr)
		{
			if (string.IsNullOrEmpty(prefix))
			{
				_terminal.WriteIndentedLine(LocalizationStore.Current.NoKeysInJson, TerminalColor.Warning);
				_terminal.WriteLine();
				_pressAnyKey.Show();

				return;
			}

			FlattenNode(arr, prefix, separator, entries);
		}
		else
		{
			_terminal.WriteIndentedLine(string.Format(LocalizationStore.Current.InvalidJson, node?.ToString() ?? string.Empty), TerminalColor.Error);
			_terminal.WriteLine();
			_pressAnyKey.Show();

			return;
		}

		if (entries.Count == 0)
		{
			_terminal.WriteIndentedLine(LocalizationStore.Current.NoKeysInJson, TerminalColor.Warning);
			_terminal.WriteLine();
			_pressAnyKey.Show();

			return;
		}

		if (!ConfirmImport(entries, config))
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

		var imported = await _spinner.RunAsync($"Importing {entries.Count} keys...", async ct =>
		{
			foreach (var (Key, Value) in entries)
			{
				var existing = await _etcdClient.GetKeyAsync(Key, ct);

				if (existing is not null)
				{
					var updated = await _etcdClient.UpdateKeyAsync(Key, Value, ct);

					if (updated)
						overwritten++;
					else
						failed++;
				}
				else
				{
					var result = await _etcdClient.CreateKeyAsync(Key, Value, ct);

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

		_terminal.WriteLine();
		_terminal.WriteIndentedLine(
			string.Format(LocalizationStore.Current.ImportResult, created + overwritten, overwritten, failed),
			failed > 0 ? TerminalColor.Warning : TerminalColor.Success);
		_terminal.WriteLine();
		_pressAnyKey.Show();
	}

	private bool ConfirmImport(List<(string Key, string Value)> entries, EtcdConnectionConfig config)
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

		var choice = _menu.Show(
			LocalizationStore.Current.ConfirmImport,
			[LocalizationStore.Current.Yes, LocalizationStore.Current.No],
			config: config);

		return choice == LocalizationStore.Current.Yes;
	}

	private static string Truncate(string value)
	{
		var single = value.ReplaceLineEndings(" ");

		return single.Length <= _previewValueLength ? single : single[.._previewValueLength] + "...";
	}

	private static string SanitizeJson(string input)
	{
		var trimmed = input.Trim();

		// Wrap in { } if not already an object or array
		if (trimmed.Length > 0 && trimmed[0] != '{' && trimmed[0] != '[')
			trimmed = "{ " + trimmed + " }";

		// Remove trailing commas before } or ]
		trimmed = System.Text.RegularExpressions.Regex.Replace(trimmed, @",\s*([}\]])", "$1");

		return trimmed;
	}

	private static void FlattenJson(JsonObject node, string prefix, string separator, List<(string Key, string Value)> results)
	{
		foreach (var property in node)
		{
			var key = string.IsNullOrEmpty(prefix)
				? property.Key
				: prefix + separator + property.Key;

			FlattenNode(property.Value, key, separator, results);
		}
	}

	private static void FlattenNode(JsonNode? node, string currentPath, string separator, List<(string Key, string Value)> results)
	{
		if (node is null)
			return;

		var valueKind = node.GetValueKind();

		switch (valueKind)
		{
			case JsonValueKind.Object:
				foreach (var property in node.AsObject())
					FlattenNode(property.Value, currentPath + separator + property.Key, separator, results);
				break;

			case JsonValueKind.Array:
				var index = 0;

				foreach (var item in node.AsArray())
					FlattenNode(item, currentPath + separator + index++, separator, results);
				break;

			default:
				results.Add((currentPath, node.ToString()));
				break;
		}
	}
}
