using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace EtcdTerminal.Keys;

public static partial class JsonKeyFlattener
{
	public static IReadOnlyList<KeyValuePair<string, string>> Flatten(string json, string prefix, string separator)
	{
		var sanitized = SanitizeJson(json);
		var node = JsonNode.Parse(sanitized);

		if (node is JsonObject obj)
		{
			List<KeyValuePair<string, string>> results = [];

			FlattenJson(obj, prefix, separator, results);

			return results;
		}

		if (node is JsonArray arr)
		{
			if (string.IsNullOrEmpty(prefix))
				throw new ArgumentException("A prefix is required when the root is a JSON array.", nameof(prefix));

			List<KeyValuePair<string, string>> results = [];

			FlattenNode(arr, prefix, separator, results);

			return results;
		}

		throw new JsonException($"Unsupported JSON root value: {node?.ToString() ?? string.Empty}.");
	}

	private static string SanitizeJson(string input)
	{
		var trimmed = input.Trim();

		// Wrap in { } if not already an object or array
		if (trimmed.Length > 0 && trimmed[0] != '{' && trimmed[0] != '[')
			trimmed = "{ " + trimmed + " }";

		// Remove trailing commas before } or ]
		trimmed = TrailingCommaPattern().Replace(trimmed, "$1");

		return trimmed;
	}

	private static void FlattenJson(JsonObject node, string prefix, string separator, List<KeyValuePair<string, string>> results)
	{
		foreach (var property in node)
		{
			var key = string.IsNullOrEmpty(prefix)
				? property.Key
				: prefix + separator + property.Key;

			FlattenNode(property.Value, key, separator, results);
		}
	}

	private static void FlattenNode(JsonNode? node, string currentPath, string separator, List<KeyValuePair<string, string>> results)
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
				results.Add(new(currentPath, node.ToString()));
				break;
		}
	}

	[GeneratedRegex(@",\s*([}\]])")]
	private static partial Regex TrailingCommaPattern();
}
