using System.Text.Json;
using EtcdTerminal;
using EtcdTerminal.Models;

namespace EtcdTerminal.Infrastructure.Configuration;

public sealed class ConnectionConfigRepository : IConnectionConfigRepository
{
    private const string ConfigDir = ".config/etcd-terminal";
    private const string ConfigFile = "appsettings.json";

    private readonly string _configPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public ConnectionConfigRepository()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _configPath = Path.Combine(home, ConfigDir, ConfigFile);
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
    }

    public IReadOnlyList<EtcdConnectionConfig> LoadInstances()
    {
        if (!File.Exists(_configPath))
            return Array.Empty<EtcdConnectionConfig>();

        try
        {
            var json = File.ReadAllText(_configPath);
            var doc = JsonDocument.Parse(json);
            var instances = doc.RootElement
                .GetProperty("EtcdTerminal")
                .GetProperty("Instances")
                .EnumerateArray();

            return instances.Select(i => new EtcdConnectionConfig
            {
                Name = i.GetProperty("Name").GetString() ?? string.Empty,
                ConnectionString = i.GetProperty("ConnectionString").GetString() ?? string.Empty,
                UseSsl = i.GetProperty("UseSsl").GetBoolean(),
                Username = i.TryGetProperty("Username", out var u) ? u.GetString() : null,
                Password = i.TryGetProperty("Password", out var p) ? p.GetString() : null,
            }).ToList();
        }
        catch
        {
            return Array.Empty<EtcdConnectionConfig>();
        }
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

    private void SaveInstances(List<EtcdConnectionConfig> instances)
    {
        var dir = Path.GetDirectoryName(_configPath)!;
        Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(new
        {
            EtcdTerminal = new
            {
                Instances = instances.Select(i => new
                {
                    i.Name,
                    i.ConnectionString,
                    i.UseSsl,
                    i.Username,
                    i.Password
                })
            }
        }, _jsonOptions);

        File.WriteAllText(_configPath, json);
    }
}
