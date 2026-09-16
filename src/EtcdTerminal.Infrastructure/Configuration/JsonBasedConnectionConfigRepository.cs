using System.Text.Json;
using System.Text.Json.Nodes;
using EtcdTerminal.Configuration;
using EtcdTerminal.Environment;
using EtcdTerminal.Infrastructure.IO;

namespace EtcdTerminal.Infrastructure.Configuration;

public sealed class JsonBasedConnectionConfigRepository(IAppEnvironment environment) : IConnectionConfigRepository
{
	private const string InstancesSection = "Instances";

	private readonly string _configPath = environment.ConfigFilePath;
	private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

	public IReadOnlyList<EtcdConnectionConfig> LoadInstances()
	{
		if (!File.Exists(_configPath))
			return [];

		try
		{
			var json = File.ReadAllText(_configPath);
			var doc = JsonDocument.Parse(json);
			var instances = doc.RootElement
				.GetProperty(InstancesSection)
				.EnumerateArray();

			return [.. instances
				.Select(i =>
				{
					try
					{
						return new EtcdConnectionConfig
						{
							Name = i.GetProperty("Name").GetString() ?? string.Empty,
							ConnectionString = i.GetProperty("ConnectionString").GetString() ?? string.Empty,
							Username = i.TryGetProperty("Username", out var u) ? u.GetString() : null,
							Password = i.TryGetProperty("Password", out var p) ? p.GetString() : null,
						};
					}
					catch
					{
						return null;
					}
				})
				.Where(c => c is not null && IsValid(c))
				.Cast<EtcdConnectionConfig>()];
		}
		catch
		{
			return [];
		}
	}

	public void AddInstance(EtcdConnectionConfig config)
	{
		var instances = LoadInstances().ToList();

		instances.RemoveAll(i => i.Name == config.Name);
		instances.Add(config);

		SaveInstances(instances);
	}

	public void UpdateInstance(string originalName, EtcdConnectionConfig config)
	{
		var instances = LoadInstances().ToList();
		var index = instances.FindIndex(i => i.Name == originalName);

		if (index < 0)
			instances.Add(config);
		else
			instances[index] = config;

		SaveInstances(instances);
	}

	public void RemoveInstance(string name)
	{
		var instances = LoadInstances().ToList();

		instances.RemoveAll(i => i.Name == name);

		SaveInstances(instances);
	}

	public void MoveUp(string name)
	{
		var instances = LoadInstances().ToList();
		var index = instances.FindIndex(i => i.Name == name);

		if (index <= 0)
			return;

		(instances[index], instances[index - 1]) = (instances[index - 1], instances[index]);

		SaveInstances(instances);
	}

	public void MoveDown(string name)
	{
		var instances = LoadInstances().ToList();
		var index = instances.FindIndex(i => i.Name == name);

		if (index < 0 || index >= instances.Count - 1)
			return;

		(instances[index], instances[index + 1]) = (instances[index + 1], instances[index]);

		SaveInstances(instances);
	}

	private static bool IsValid(EtcdConnectionConfig config) =>
		!string.IsNullOrWhiteSpace(config.Name) && config.IsConnectionStringValid;

	private void SaveInstances(List<EtcdConnectionConfig> instances)
	{
		var dir = Path.GetDirectoryName(_configPath)!;

		PrivateFileSystem.CreateDirectory(dir);

		var root = LoadExistingRoot();

		root[InstancesSection] = JsonSerializer.SerializeToNode(instances.Select(i => new
		{
			i.Name,
			i.ConnectionString,
			i.Username,
			i.Password
		}));

		PrivateFileSystem.WriteAllTextAtomic(_configPath, root.ToJsonString(_jsonOptions));
	}

	private JsonObject LoadExistingRoot()
	{
		if (!File.Exists(_configPath))
			return [];

		var root = JsonNode.Parse(File.ReadAllText(_configPath)) as JsonObject;

		if (root is null)
			throw new JsonException($"Configuration file '{_configPath}' does not contain a JSON object.");

		return root;
	}
}
