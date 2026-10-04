using System.Text.Json;
using System.Text.Json.Nodes;
using EtcdTerminal.Configuration;

namespace EtcdTerminal.Infrastructure.Configuration;

public sealed class JsonBasedConnectionConfigRepository(JsonConfigFile _configFile) : IConnectionConfigRepository
{
	private const string InstancesSection = "Instances";

	public IReadOnlyList<EtcdConnectionConfig> LoadInstances()
	{
		try
		{
			var instances = _configFile.TryReadRoot()?[InstancesSection]?.AsArray();

			if (instances is null)
				return [];

			return [.. instances
				.Select(TryParseEntry)
				.Where(c => c is not null && IsValid(c))
				.Cast<EtcdConnectionConfig>()];
		}
		catch
		{
			return [];
		}
	}

	public bool IsNameTaken(string name, string? exceptName) =>
		LoadInstances().Any(i => i.Name == name && i.Name != exceptName);

	public void AddInstance(EtcdConnectionConfig config)
	{
		var root = _configFile.ReadRootOrThrow();
		var instances = ParseInstancesStrict(root);

		instances.RemoveAll(i => i.Name == config.Name);
		instances.Add(config);

		SaveInstances(root, instances);
	}

	public void UpdateInstance(string originalName, EtcdConnectionConfig config)
	{
		var root = _configFile.ReadRootOrThrow();
		var instances = ParseInstancesStrict(root);
		var index = instances.FindIndex(i => i.Name == originalName);

		if (index < 0)
		{
			instances.RemoveAll(i => i.Name == config.Name);
			instances.Add(config);
		}
		else if (config.Name != originalName && instances.Any(i => i.Name == config.Name))
			throw new InvalidOperationException($"Cannot rename connection '{originalName}' to '{config.Name}': another connection already uses that name.");
		else
			instances[index] = config;

		SaveInstances(root, instances);
	}

	public void RemoveInstance(string name)
	{
		var root = _configFile.ReadRootOrThrow();
		var instances = ParseInstancesStrict(root);

		instances.RemoveAll(i => i.Name == name);

		SaveInstances(root, instances);
	}

	public void MoveUp(string name)
	{
		var root = _configFile.ReadRootOrThrow();
		var instances = ParseInstancesStrict(root);
		var index = instances.FindIndex(i => i.Name == name);

		if (index <= 0)
			return;

		(instances[index], instances[index - 1]) = (instances[index - 1], instances[index]);

		SaveInstances(root, instances);
	}

	public void MoveDown(string name)
	{
		var root = _configFile.ReadRootOrThrow();
		var instances = ParseInstancesStrict(root);
		var index = instances.FindIndex(i => i.Name == name);

		if (index < 0 || index >= instances.Count - 1)
			return;

		(instances[index], instances[index + 1]) = (instances[index + 1], instances[index]);

		SaveInstances(root, instances);
	}

	private static bool IsValid(EtcdConnectionConfig config) =>
		!string.IsNullOrWhiteSpace(config.Name) && config.IsConnectionStringValid;

	private static List<EtcdConnectionConfig> ParseInstancesStrict(JsonObject root)
	{
		if (root[InstancesSection] is null)
			return [];

		if (root[InstancesSection] is not JsonArray array)
			throw new InvalidDataException("Connection configuration 'Instances' section must be a JSON array.");

		var instances = new List<EtcdConnectionConfig>(array.Count);

		for (var i = 0; i < array.Count; i++)
		{
			var parsed = TryParseEntry(array[i]);

			if (parsed is null || !IsValid(parsed))
				throw new InvalidDataException($"Connection configuration 'Instances' entry at index {i} is invalid.");

			instances.Add(parsed);
		}

		if (instances.GroupBy(i => i.Name).Any(g => g.Count() > 1))
			throw new InvalidDataException("Connection configuration 'Instances' section contains duplicate connection names.");

		return instances;
	}

	private static EtcdConnectionConfig? TryParseEntry(JsonNode? node)
	{
		try
		{
			if (node is not JsonObject obj)
				return null;

			return new EtcdConnectionConfig
			{
				Name = obj["Name"]?.GetValue<string>() ?? string.Empty,
				ConnectionString = obj["ConnectionString"]?.GetValue<string>() ?? string.Empty,
				Username = obj["Username"]?.GetValue<string>(),
				Password = obj["Password"]?.GetValue<string>()
			};
		}
		catch
		{
			return null;
		}
	}

	private void SaveInstances(JsonObject root, List<EtcdConnectionConfig> instances)
	{
		root[InstancesSection] = JsonSerializer.SerializeToNode(instances.Select(i => new
		{
			i.Name,
			i.ConnectionString,
			i.Username,
			i.Password
		}));

		_configFile.WriteRoot(root);
	}
}
