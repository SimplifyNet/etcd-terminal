using System.Security.Cryptography;
using EtcdTerminal.Configuration;
using EtcdTerminal.Environment;
using EtcdTerminal.Infrastructure.Configuration;
using EtcdTerminal.Infrastructure.Security;
using EtcdTerminal.Security;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class DecryptFailureTests
{
	[Test]
	public void LostKey_KeepsInstanceWithClearedPasswordAndNamesIt()
	{
		var inner = new StubRepository([new EtcdConnectionConfig { Name = "prod", ConnectionString = "http://x:2379", Password = "cipher" }]);
		var repo = new ProtectedConfigRepository(inner, new ThrowingProtector());

		var instances = repo.LoadInstances();

		Assert.That(instances.Count, Is.EqualTo(1));
		Assert.That(instances[0].Name, Is.EqualTo("prod"));
		Assert.That(instances[0].Password, Is.Null);
		Assert.That(repo.TakeDecryptFailures(), Is.EqualTo(["prod"]));
	}

	[Test]
	public void TakeDecryptFailures_ConsumesFailures()
	{
		var inner = new StubRepository([new EtcdConnectionConfig { Name = "prod", ConnectionString = "http://x:2379", Password = "cipher" }]);
		var repo = new ProtectedConfigRepository(inner, new ThrowingProtector());

		repo.LoadInstances();

		Assert.That(repo.TakeDecryptFailures(), Is.EqualTo(["prod"]));
		Assert.That(repo.TakeDecryptFailures(), Is.Empty);
	}

	[Test]
	public void HealthyDecrypt_PreservesPasswordWithoutFailures()
	{
		var inner = new StubRepository([new EtcdConnectionConfig { Name = "prod", ConnectionString = "http://x:2379", Password = "cipher" }]);
		var repo = new ProtectedConfigRepository(inner, new EchoProtector());

		var instances = repo.LoadInstances();

		Assert.That(instances[0].Password, Is.EqualTo("plain"));
		Assert.That(repo.TakeDecryptFailures(), Is.Empty);
	}

	[Test]
	public void TruncatedKeyFile_ThrowsClearError()
	{
		var dir = Path.Combine(Path.GetTempPath(), "t30-" + Guid.NewGuid().ToString("N"));

		Directory.CreateDirectory(dir);

		var env = new FakeEnv(Path.Combine(dir, "config.json"), Path.Combine(dir, ".key"));

		File.WriteAllBytes(env.KeyFilePath, [1, 2, 3, 4, 5]);

		var ex = Assert.Throws<InvalidDataException>(() => new ConfigProtector(env));

		Assert.That(ex!.Message, Does.Contain("32"));
	}

	private sealed class StubRepository(IReadOnlyList<EtcdConnectionConfig> instances) : IConnectionConfigRepository
	{
		public IReadOnlyList<EtcdConnectionConfig> LoadInstances() => instances;

		public IReadOnlyList<string> TakeDecryptFailures() => [];

		public void AddInstance(EtcdConnectionConfig config) => throw new NotSupportedException();

		public void UpdateInstance(string originalName, EtcdConnectionConfig config) => throw new NotSupportedException();

		public void RemoveInstance(string name) => throw new NotSupportedException();

		public void MoveUp(string name) => throw new NotSupportedException();

		public void MoveDown(string name) => throw new NotSupportedException();
	}

	private sealed class ThrowingProtector : IConfigProtector
	{
		public string Encrypt(string plainText) => throw new NotSupportedException();

		public string? Decrypt(string? cipherText) => throw new CryptographicException();
	}

	private sealed class EchoProtector : IConfigProtector
	{
		public string Encrypt(string plainText) => plainText;

		public string? Decrypt(string? cipherText) => "plain";
	}

	private sealed class FakeEnv(string configPath, string keyPath) : IAppEnvironment
	{
		public string ConfigDirectoryPath => Path.GetDirectoryName(configPath)!;

		public string ConfigFilePath => configPath;

		public string KeyFilePath => keyPath;
	}
}
