using System.Text.Json;
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
				.Select(i =>
				{
					try
					{
						return new EtcdConnectionConfig
						{
							Name = i?["Name"]?.GetValue<string>() ?? string.Empty,
							ConnectionString = i?["ConnectionString"]?.GetValue<string>() ?? string.Empty,
							Username = i?["Username"]?.GetValue<string>(),
							Password = i?["Password"]?.GetValue<string>(),
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
		var root = _configFile.ReadRootOrThrow();

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
