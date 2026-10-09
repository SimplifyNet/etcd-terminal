using System.Text.Json;
using EtcdTerminal.Keys;

namespace EtcdTerminal.App.Screens.Keys.Import;

/// <summary>
/// Flattens the pasted JSON into key/value entries. Reports an unparsable or an
/// empty document as a data-only failure; showing it belongs to the caller.
/// </summary>
public static class ImportEntriesParser
{
	public static ImportParseResult Parse(ImportSource source)
	{
		IReadOnlyList<KeyValuePair<string, string>> entries;

		try
		{
			entries = JsonKeyFlattener.Flatten(source.Json, source.Prefix, source.Separator);
		}
		catch (ArgumentException)
		{
			return Failure(ImportParseFailureKind.NoKeys, null);
		}
		catch (Exception ex) when (ex is JsonException or InvalidOperationException)
		{
			return Failure(ImportParseFailureKind.InvalidJson, ex.Message);
		}

		if (entries.Count == 0)
			return Failure(ImportParseFailureKind.NoKeys, null);

		return new(entries, null);
	}

	private static ImportParseResult Failure(ImportParseFailureKind kind, string? detail) =>
		new([], new(kind, detail));
}
