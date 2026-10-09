using System.Text.Json;
using EtcdTerminal.App.Components;
using EtcdTerminal.Keys;
using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Screens.Keys.Import;

/// <summary>
/// Flattens the pasted JSON into key/value entries, reporting an unparsable or
/// an empty document instead of throwing it at the caller.
/// </summary>
public sealed class ImportEntriesParser(Message _message, ILocalizationCatalog _localizations)
{
	public IReadOnlyList<KeyValuePair<string, string>>? Parse(ImportSource source)
	{
		IReadOnlyList<KeyValuePair<string, string>> entries;

		try
		{
			entries = JsonKeyFlattener.Flatten(source.Json, source.Prefix, source.Separator);
		}
		catch (ArgumentException)
		{
			_message.ShowWarning(_localizations.Current.NoKeysInJson);

			return null;
		}
		catch (Exception ex) when (ex is JsonException or InvalidOperationException)
		{
			_message.ShowError(string.Format(_localizations.Current.InvalidJson, ex.Message));

			return null;
		}

		if (entries.Count == 0)
		{
			_message.ShowWarning(_localizations.Current.NoKeysInJson);

			return null;
		}

		return entries;
	}
}
