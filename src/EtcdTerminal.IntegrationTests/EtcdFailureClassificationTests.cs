using EtcdTerminal.Configuration;
using EtcdTerminal.Infrastructure;
using NUnit.Framework;

namespace EtcdTerminal.IntegrationTests;

[TestFixture]
public sealed class EtcdFailureClassificationTests
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
	public async Task MissingUser_ReturnsNull()
	{
		using var client = CreateClient();

		await client.ConnectAsync(EtcdTestServer.RootConfig(_endpoint));

		Assert.That(await client.GetUserAsync("ghost-" + Guid.NewGuid().ToString("N")), Is.Null);
	}

	[Test]
	public async Task MissingRole_ReturnsNull()
	{
		using var client = CreateClient();

		await client.ConnectAsync(EtcdTestServer.RootConfig(_endpoint));

		Assert.That(await client.GetRoleAsync("ghost-" + Guid.NewGuid().ToString("N")), Is.Null);
	}

	[Test]
	public async Task DeniedKeyRead_ThrowsAccessDeniedInsteadOfNull()
	{
		using var client = CreateClient();

		await client.ConnectAsync(EtcdTestServer.WriterConfig(_endpoint));

		var ex = Assert.ThrowsAsync<EtcdOperationException>(() => client.GetKeyAsync("/t03w/k"));

		Assert.That(ex!.Kind, Is.EqualTo(EtcdOperationFailureKind.AccessDenied));
	}

	[Test]
	public async Task DeniedKeyList_ThrowsAccessDeniedInsteadOfEmpty()
	{
		using var client = CreateClient();

		await client.ConnectAsync(EtcdTestServer.WriterConfig(_endpoint));

		var ex = Assert.ThrowsAsync<EtcdOperationException>(() => client.GetKeysByPrefixAsync("/t03w/"));

		Assert.That(ex!.Kind, Is.EqualTo(EtcdOperationFailureKind.AccessDenied));
	}

	[Test]
	public async Task EmptyKeyList_ReturnsEmpty()
	{
		using var client = CreateClient();

		await client.ConnectAsync(EtcdTestServer.RootConfig(_endpoint));

		Assert.That(await client.GetKeysByPrefixAsync("/t04-empty-" + Guid.NewGuid().ToString("N") + "/"), Is.Empty);
	}

	[Test]
	public void InvalidAuthentication_Connect_ThrowsAccessDenied()
	{
		using var client = CreateClient();

		var config = new EtcdConnectionConfig
		{
			Name = "root",
			ConnectionString = _endpoint,
			Username = "root",
			Password = "wrongpw"
		};

		var ex = Assert.ThrowsAsync<EtcdOperationException>(() => client.ConnectAsync(config));

		Assert.That(ex!.Kind, Is.EqualTo(EtcdOperationFailureKind.AccessDenied));
	}

	[Test]
	public void CancelledConnect_PropagatesCancellation()
	{
		using var client = CreateClient();
		using var cts = new CancellationTokenSource();

		cts.Cancel();

		Assert.ThrowsAsync<OperationCanceledException>(() =>
			client.ConnectAsync(EtcdTestServer.RootConfig(_endpoint), cts.Token));
	}

	private static DotnetEtcdBasedClient CreateClient() =>
		new(DotnetEtcdTransportFactory.Create);
}
