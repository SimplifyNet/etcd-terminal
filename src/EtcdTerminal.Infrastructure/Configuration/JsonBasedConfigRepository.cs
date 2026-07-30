using System.Text.Json;
using EtcdTerminal.Models;

namespace EtcdTerminal.Infrastructure.Configuration;

public sealed class JsonBasedConfigRepository : IConnectionConfigRepository
{
	private readonly string _configPath;
	private readonly IConfigProtector _protector;
	private readonly JsonSerializerOptions _jsonOptions;

	public JsonBasedConfigRepository(IAppEnvironment environment, IConfigProtector protector)
	{
		_configPath = environment.ConfigFilePath;
		_protector = protector;
		_jsonOptions = new JsonSerializerOptions { WriteIndented = true };
	}

	public IReadOnlyList<EtcdConnectionConfig> LoadInstances()
	{
		if (!File.Exists(_configPath))
			return [];

		try
		{
			var json = File.ReadAllText(_configPath);
			var doc = JsonDocument.Parse(json);
			var instances = doc.RootElement
				.GetProperty("Instances")
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
							Password = _protector.Decrypt(i.TryGetProperty("Password", out var p) ? p.GetString() : null),
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
			return Array.Empty<EtcdConnectionConfig>();
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

		var json = JsonSerializer.Serialize(new
		{
			Instances = instances.Select(i => new
			{
				i.Name,
				i.ConnectionString,
				i.Username,
				Password = i.Password != null ? _protector.Encrypt(i.Password) : null
			})
		}, _jsonOptions);

		File.WriteAllText(_configPath, json);
	}
}
