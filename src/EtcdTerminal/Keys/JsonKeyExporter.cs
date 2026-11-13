using System.Text.Json;

namespace EtcdTerminal.Keys;

/// <summary>
/// Turns keys into a JSON object whose properties are the full key names.
/// The document can be imported again with an empty prefix, so the export
/// keeps every key exactly as it is stored.
/// </summary>
public static class JsonKeyExporter
{
	public static string Export(IReadOnlyList<EtcdKeyValue> keys)
	{
		using var stream = new MemoryStream();

		using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
		{
			writer.WriteStartObject();

			foreach (var key in keys)
				writer.WriteString(key.Key, key.Value);

			writer.WriteEndObject();
		}

		return System.Text.Encoding.UTF8.GetString(stream.ToArray());
	}
}
