using System.Text.Json;
using System.Text.Json.Nodes;
using EtcdTerminal.Environment;
using EtcdTerminal.Infrastructure.IO;

namespace EtcdTerminal.Infrastructure.Configuration;

public sealed class JsonConfigFile(IAppEnvironment _environment)
{
	private readonly string _configPath = _environment.ConfigFilePath;
	private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

	public JsonObject? TryReadRoot()
	{
		if (!File.Exists(_configPath))
			return null;

		try
		{
			return JsonNode.Parse(File.ReadAllText(_configPath)) as JsonObject;
		}
		catch (JsonException)
		{
			return null;
		}
	}

	public JsonObject ReadRootOrThrow()
	{
		if (!File.Exists(_configPath))
			return [];

		var root = JsonNode.Parse(File.ReadAllText(_configPath)) as JsonObject;

		if (root is null)
			throw new JsonException($"Configuration file '{_configPath}' does not contain a JSON object.");

		return root;
	}

	public void WriteRoot(JsonObject root)
	{
		var dir = Path.GetDirectoryName(_configPath)!;

		PrivateFileSystem.CreateDirectory(dir);
		PrivateFileSystem.WriteAllTextAtomic(_configPath, root.ToJsonString(_jsonOptions));
	}
}
