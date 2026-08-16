using System.Text.Json;
using System.Text.Json.Nodes;
using EtcdTerminal.Configuration;
using EtcdTerminal.Environment;

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

	private static bool IsValid(EtcdConnectionConfig config)
	{
		if (string.IsNullOrWhiteSpace(config.Name))
			return false;

		if (string.IsNullOrWhiteSpace(config.ConnectionString))
			return false;

		if (!Uri.TryCreate(config.ConnectionString, UriKind.Absolute, out var uri))
			return false;

		if (uri.Scheme is not ("http" or "https"))
			return false;

		return true;
	}

	public void AddInstance(EtcdConnectionConfig config)
	{
		var instances = LoadInstances().ToList();
		instances.RemoveAll(i => i.Name == config.Name);
		instances.Add(config);
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

	private void SaveInstances(List<EtcdConnectionConfig> instances)
	{
		var dir = Path.GetDirectoryName(_configPath)!;

		Directory.CreateDirectory(dir);

		var root = LoadExistingRoot();

		root[InstancesSection] = JsonSerializer.SerializeToNode(instances.Select(i => new
		{
			i.Name,
			i.ConnectionString,
			i.Username,
			i.Password
		}));

		File.WriteAllText(_configPath, root.ToJsonString(_jsonOptions));
	}

	private JsonObject LoadExistingRoot()
	{
		if (!File.Exists(_configPath))
			return [];

		try
		{
			return JsonNode.Parse(File.ReadAllText(_configPath)) as JsonObject ?? [];
		}
		catch
		{
			return [];
		}
	}
}
