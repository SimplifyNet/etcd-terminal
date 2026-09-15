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
	Spinner _spinner)
{
	public async Task ShowAsync(EtcdConnectionConfig config)
	{
		_screenLayout.RenderHeader(config);

		var separator = _prompt.Ask(LocalizationStore.Current.EnterSeparator, ":") ?? ":";
		var prefix = _prompt.Ask(LocalizationStore.Current.EnterPrefix);

		if (prefix is null)
			return;

		_terminal.WriteLine();

		var json = _prompt.ReadMultiLine(LocalizationStore.Current.PasteJson);

		if (json is null)
			return;

		var sanitized = SanitizeJson(json);

		JsonObject? root;

		try
		{
			root = JsonNode.Parse(sanitized)?.AsObject();
		}
		catch (JsonException ex)
		{
			_terminal.WriteIndentedLine(string.Format(LocalizationStore.Current.InvalidJson, ex.Message), TerminalColor.Error);
			_terminal.WriteLine();
			_pressAnyKey.Show();

			return;
		}

		if (root is null || root.Count == 0)
		{
			_terminal.WriteIndentedLine(LocalizationStore.Current.NoKeysInJson, TerminalColor.Warning);
			_terminal.WriteLine();
			_pressAnyKey.Show();

			return;
		}

		var entries = new List<(string Key, string Value)>();

		FlattenJson(root, prefix, separator, entries);

		if (entries.Count == 0)
		{
			_terminal.WriteIndentedLine(LocalizationStore.Current.NoKeysInJson, TerminalColor.Warning);
			_terminal.WriteLine();
			_pressAnyKey.Show();

			return;
		}

		var created = 0;
		var overwritten = 0;
		var failed = 0;

		await _spinner.RunAsync($"Importing {entries.Count} keys...", async ct =>
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

		_terminal.WriteLine();
		_terminal.WriteIndentedLine(
			string.Format(LocalizationStore.Current.ImportResult, created + overwritten, overwritten, failed),
			failed > 0 ? TerminalColor.Warning : TerminalColor.Success);
		_terminal.WriteLine();
		_pressAnyKey.Show();
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
			var key = prefix + separator + property.Key;

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
