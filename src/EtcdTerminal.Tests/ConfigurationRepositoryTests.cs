using System.Text.Json;
using System.Text.Json.Nodes;
using EtcdTerminal.Configuration;
using EtcdTerminal.Environment;
using EtcdTerminal.Infrastructure.Configuration;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class ConfigurationRepositoryTests
{
	[Test]
	public void SettingsSave_MalformedFile_ThrowsAndPreservesBytes()
	{
		var (configPath, _, settings) = CreateRepositories();

		File.WriteAllText(configPath, "{bad");

		var before = File.ReadAllBytes(configPath);

		Assert.That(() => settings.Save(new AppSettings { PageSize = 42 }), Throws.InstanceOf<JsonException>());

		Assert.That(File.ReadAllBytes(configPath), Is.EqualTo(before));
	}

	[Test]
	public void SettingsSave_NonObjectRoot_ThrowsAndPreservesBytes()
	{
		var (configPath, _, settings) = CreateRepositories();

		File.WriteAllText(configPath, "[1,2]");

		var before = File.ReadAllBytes(configPath);

		Assert.That(() => settings.Save(new AppSettings { PageSize = 42 }), Throws.InstanceOf<JsonException>());

		Assert.That(File.ReadAllBytes(configPath), Is.EqualTo(before));
	}

	[Test]
	public void SettingsSave_MissingFile_CreatesSettings()
	{
		var (_, _, settings) = CreateRepositories();

		settings.Save(new AppSettings { PageSize = 42, TrimInputValues = false });

		var loaded = settings.Load();

		Assert.That(loaded.PageSize, Is.EqualTo(42));
		Assert.That(loaded.TrimInputValues, Is.False);
	}

	[Test]
	public void SettingsSave_PreservesInstancesAndOtherSections()
	{
		var (configPath, connections, settings) = CreateRepositories();

		File.WriteAllText(configPath, "{\"Settings\":{\"PageSize\":30,\"TrimInputValues\":true},\"Instances\":[" + ValidInstance("a") + "],\"Other\":{\"x\":1}}");

		settings.Save(new AppSettings { PageSize = 50, TrimInputValues = true });

		Assert.That(connections.LoadInstances().Select(i => i.Name), Is.EqualTo(["a"]));
		Assert.That(settings.Load().PageSize, Is.EqualTo(50));

		var root = JsonNode.Parse(File.ReadAllText(configPath))!.AsObject();

		Assert.That(root["Other"]?["x"]?.GetValue<int>(), Is.EqualTo(1));
	}

	[Test]
	public void SettingsSave_PreservesInvalidInstances()
	{
		var (configPath, connections, settings) = CreateRepositories();

		File.WriteAllText(configPath, "{\"Settings\":{\"PageSize\":30,\"TrimInputValues\":true},\"Instances\":[{\"Name\":\"a\",\"ConnectionString\":\"not-a-url\"}]}");

		settings.Save(new AppSettings { PageSize = 50, TrimInputValues = true });

		Assert.That(File.ReadAllText(configPath), Does.Contain("not-a-url"));
		Assert.That(connections.LoadInstances(), Is.Empty);
	}

	[Test]
	public void ConnectionAdd_MalformedFile_ThrowsAndPreservesBytes()
	{
		var (configPath, connections, _) = CreateRepositories();

		File.WriteAllText(configPath, "{bad");

		var before = File.ReadAllBytes(configPath);

		Assert.That(() => connections.AddInstance(new EtcdConnectionConfig { Name = "c", ConnectionString = "http://c:2379" }), Throws.InstanceOf<JsonException>());

		Assert.That(File.ReadAllBytes(configPath), Is.EqualTo(before));
	}

	[Test]
	public void ConnectionMutation_NonObjectRoot_ThrowsAndPreservesBytes()
	{
		var (configPath, connections, _) = CreateRepositories();

		File.WriteAllText(configPath, "[1,2]");

		var before = File.ReadAllBytes(configPath);

		Assert.That(() => connections.RemoveInstance("a"), Throws.InstanceOf<JsonException>());

		Assert.That(File.ReadAllBytes(configPath), Is.EqualTo(before));
	}

	[Test]
	public void ConnectionAdd_InvalidEntryAmongValid_ThrowsAndPreservesBytes()
	{
		var (configPath, connections, _) = CreateRepositories();

		File.WriteAllText(configPath, "{\"Instances\":[" + ValidInstance("a") + ",{\"Name\":\"b\",\"ConnectionString\":\"bad\"}]}");

		var before = File.ReadAllBytes(configPath);

		Assert.Throws<InvalidDataException>(() => connections.AddInstance(new EtcdConnectionConfig { Name = "c", ConnectionString = "http://c:2379" }));

		Assert.That(File.ReadAllBytes(configPath), Is.EqualTo(before));
		Assert.That(connections.LoadInstances().Select(i => i.Name), Is.EqualTo(["a"]));
	}

	[Test]
	public void ConnectionMutation_NonArrayInstances_ThrowsAndPreservesBytes()
	{
		var (configPath, connections, _) = CreateRepositories();

		File.WriteAllText(configPath, "{\"Instances\":{\"a\":1}}");

		var before = File.ReadAllBytes(configPath);

		Assert.Throws<InvalidDataException>(() => connections.MoveUp("x"));

		Assert.That(File.ReadAllBytes(configPath), Is.EqualTo(before));
	}

	[Test]
	public void ConnectionMutation_NonObjectEntry_ThrowsAndPreservesBytes()
	{
		var (configPath, connections, _) = CreateRepositories();

		File.WriteAllText(configPath, "{\"Instances\":[\"oops\"]}");

		var before = File.ReadAllBytes(configPath);

		Assert.Throws<InvalidDataException>(() => connections.RemoveInstance("oops"));

		Assert.That(File.ReadAllBytes(configPath), Is.EqualTo(before));
	}

	[Test]
	public void ConnectionRemove_DuplicateNames_ThrowsAndPreservesBytes()
	{
		var (configPath, connections, _) = CreateRepositories();

		File.WriteAllText(configPath, "{\"Instances\":[" + ValidInstance("a", "http://one:2379") + "," + ValidInstance("a", "http://two:2379") + "]}");

		var before = File.ReadAllBytes(configPath);

		Assert.Throws<InvalidDataException>(() => connections.RemoveInstance("a"));

		Assert.That(File.ReadAllBytes(configPath), Is.EqualTo(before));
	}

	[Test]
	public void UpdateInstance_RenameCollision_ThrowsAndPreservesBytes()
	{
		var (configPath, connections, _) = CreateRepositories();

		File.WriteAllText(configPath, "{\"Instances\":[" + ValidInstance("a") + "," + ValidInstance("b") + "]}");

		var before = File.ReadAllBytes(configPath);

		Assert.Throws<InvalidOperationException>(() => connections.UpdateInstance("a", new EtcdConnectionConfig { Name = "b", ConnectionString = "http://other:2379" }));

		Assert.That(File.ReadAllBytes(configPath), Is.EqualTo(before));
	}

	[Test]
	public void UpdateInstance_RenameToSelf_Succeeds()
	{
		var (configPath, connections, _) = CreateRepositories();

		File.WriteAllText(configPath, "{\"Instances\":[" + ValidInstance("a") + "," + ValidInstance("b") + "]}");

		connections.UpdateInstance("a", new EtcdConnectionConfig { Name = "a", ConnectionString = "http://new:2379" });

		var instances = connections.LoadInstances();

		Assert.That(instances.Single(i => i.Name == "a").ConnectionString, Is.EqualTo("http://new:2379"));
		Assert.That(instances.Select(i => i.Name), Is.EquivalentTo(["a", "b"]));
	}

	[Test]
	public void UpdateInstance_RenameToFreshName_Succeeds()
	{
		var (configPath, connections, _) = CreateRepositories();

		File.WriteAllText(configPath, "{\"Instances\":[" + ValidInstance("a") + "," + ValidInstance("b") + "]}");

		connections.UpdateInstance("a", new EtcdConnectionConfig { Name = "c", ConnectionString = "http://c:2379" });

		Assert.That(connections.LoadInstances().Select(i => i.Name), Is.EquivalentTo(["c", "b"]));
	}

	[Test]
	public void ConnectionAdd_PreservesSettingsAndOtherSections()
	{
		var (configPath, connections, settings) = CreateRepositories();

		File.WriteAllText(configPath, "{\"Settings\":{\"PageSize\":33,\"TrimInputValues\":false},\"Other\":{\"x\":1},\"Instances\":[" + ValidInstance("a") + "]}");

		connections.AddInstance(new EtcdConnectionConfig { Name = "c", ConnectionString = "http://c:2379" });

		Assert.That(settings.Load().PageSize, Is.EqualTo(33));
		Assert.That(connections.LoadInstances().Select(i => i.Name), Is.EquivalentTo(["a", "c"]));

		var root = JsonNode.Parse(File.ReadAllText(configPath))!.AsObject();

		Assert.That(root["Other"]?["x"]?.GetValue<int>(), Is.EqualTo(1));
	}

	[Test]
	public void ConnectionAdd_MissingFile_CreatesFile()
	{
		var (_, connections, _) = CreateRepositories();

		connections.AddInstance(new EtcdConnectionConfig { Name = "a", ConnectionString = "http://a:2379" });

		Assert.That(connections.LoadInstances().Select(i => i.Name), Is.EqualTo(["a"]));
	}

	[Test]
	public void RemoveInstance_RemovesOnlyNamed()
	{
		var (_, connections, _) = CreateRepositories();

		connections.AddInstance(new EtcdConnectionConfig { Name = "a", ConnectionString = "http://a:2379" });
		connections.AddInstance(new EtcdConnectionConfig { Name = "b", ConnectionString = "http://b:2379" });

		connections.RemoveInstance("a");

		Assert.That(connections.LoadInstances().Select(i => i.Name), Is.EqualTo(["b"]));
	}

	private static (string ConfigPath, JsonBasedConnectionConfigRepository Connections, JsonBasedSettingsRepository Settings) CreateRepositories()
	{
		var dir = Path.Combine(Path.GetTempPath(), "etcfg-" + Guid.NewGuid().ToString("N"));
		var configPath = Path.Combine(dir, "config.json");
		var env = new FakeEnv(configPath);
		var configFile = new JsonConfigFile(env);

		Directory.CreateDirectory(dir);

		return (configPath, new JsonBasedConnectionConfigRepository(configFile), new JsonBasedSettingsRepository(configFile));
	}

	private static string ValidInstance(string name, string connectionString = "http://localhost:2379") =>
		"{\"Name\":\"" + name + "\",\"ConnectionString\":\"" + connectionString + "\"}";

	private sealed class FakeEnv(string configPath) : IAppEnvironment
	{
		public string ConfigDirectoryPath => Path.GetDirectoryName(configPath)!;

		public string ConfigFilePath => configPath;

		public string KeyFilePath => Path.Combine(ConfigDirectoryPath, ".key");
	}
}
