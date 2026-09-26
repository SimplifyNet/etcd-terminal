using dotnet_etcd;
using Etcdserverpb;
using EtcdTerminal.Infrastructure;
using Google.Protobuf;
using NUnit.Framework;

namespace EtcdTerminal.IntegrationTests;

[TestFixture]
public sealed class KeyMutationIntegrationTests
{
	private EtcdTestServer.RunningServer? _server;
	private string _endpoint = string.Empty;

	[OneTimeSetUp]
	public async Task StartEtcd()
	{
		_server = await EtcdTestServer.StartAsync();
		_endpoint = _server.Endpoint;

		await EtcdTestServer.EnableAuthAsync(_endpoint);
	}

	[OneTimeTearDown]
	public void StopEtcd() => _server?.Dispose();

	[Test]
	public async Task CompetingCreators_ExactlyOneSucceeds()
	{
		var first = CreateClient();
		var second = CreateClient();

		await first.ConnectAsync(EtcdTestServer.RootConfig(_endpoint));
		await second.ConnectAsync(EtcdTestServer.RootConfig(_endpoint));

		var key = UniqueKey();

		var results = await Task.WhenAll(first.CreateKeyAsync(key, "a"), second.CreateKeyAsync(key, "b"));

		Assert.That(results.Count(r => r), Is.EqualTo(1));

		var stored = await first.GetKeyAsync(key);

		Assert.That(stored!.Value, Is.EqualTo(results[0] ? "a" : "b"));

		first.Dispose();
		second.Dispose();
	}

	[Test]
	public async Task UpdateKey_MissingAndDeletedKeys_ReturnFalse()
	{
		using var client = CreateClient();

		await client.ConnectAsync(EtcdTestServer.RootConfig(_endpoint));

		var key = UniqueKey();

		Assert.That(await client.UpdateKeyAsync(key, "v"), Is.False);
		Assert.That(await client.CreateKeyAsync(key, "v"), Is.True);
		Assert.That(await client.DeleteKeyAsync(key), Is.True);
		Assert.That(await client.UpdateKeyAsync(key, "v"), Is.False);
	}

	[Test]
	public async Task UpdateLeasedKey_RetainsLeaseUntilRevoked()
	{
		using var client = CreateClient();

		await client.ConnectAsync(EtcdTestServer.RootConfig(_endpoint));

		using var root = new EtcdClient(_endpoint, "root", "rootpw", configureChannelOptions: EtcdTestServer.InsecureChannel);

		var lease = await root.LeaseGrantAsync(new LeaseGrantRequest { TTL = 100 });
		var key = UniqueKey();

		await root.PutAsync(new PutRequest
		{
			Key = ByteString.CopyFromUtf8(key),
			Value = ByteString.CopyFromUtf8("v1"),
			Lease = lease.ID
		});

		Assert.That(await client.UpdateKeyAsync(key, "v2"), Is.True);

		var stored = await client.GetKeyAsync(key);

		Assert.That(stored!.Value, Is.EqualTo("v2"));
		Assert.That(stored.Lease, Is.EqualTo(lease.ID));

		await root.LeaseRevokeAsync(new LeaseRevokeRequest { ID = lease.ID });

		Assert.That(await client.GetKeyAsync(key), Is.Null);
	}

	[Test]
	public async Task WriteOnlyAccount_CreateKey_ReportsDenial()
	{
		using var client = CreateClient();

		await client.ConnectAsync(EtcdTestServer.WriterConfig(_endpoint));

		var ex = Assert.ThrowsAsync<EtcdOperationException>(() => client.CreateKeyAsync("/t03w/k1", "v"));

		Assert.That(ex!.Kind, Is.EqualTo(EtcdOperationFailureKind.AccessDenied));
	}

	private string UniqueKey() => "/t03/" + Guid.NewGuid().ToString("N") + "/k";

	private static DotnetEtcdBasedClient CreateClient() =>
		new(DotnetEtcdTransportFactory.Create);
}
