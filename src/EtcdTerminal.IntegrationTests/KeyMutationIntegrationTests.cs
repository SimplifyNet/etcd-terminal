using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Authpb;
using dotnet_etcd;
using Etcdserverpb;
using EtcdTerminal.Configuration;
using EtcdTerminal.Infrastructure;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using NUnit.Framework;

namespace EtcdTerminal.IntegrationTests;

[TestFixture]
public sealed class KeyMutationIntegrationTests
{
	private Process? _etcd;
	private string _dataDir = string.Empty;
	private string _endpoint = string.Empty;

	[OneTimeSetUp]
	public async Task StartEtcd()
	{
		var clientPort = FreePort();
		var peerPort = FreePort();

		_dataDir = Path.Combine(Path.GetTempPath(), "etcd-t03-" + Guid.NewGuid().ToString("N"));
		_endpoint = $"http://127.0.0.1:{clientPort}";

		Directory.CreateDirectory(_dataDir);

		_etcd = Process.Start(new ProcessStartInfo
		{
			FileName = "etcd",
			ArgumentList =
			{
				"--data-dir", _dataDir,
				"--listen-client-urls", _endpoint,
				"--advertise-client-urls", _endpoint,
				"--listen-peer-urls", $"http://127.0.0.1:{peerPort}",
				"--initial-advertise-peer-urls", $"http://127.0.0.1:{peerPort}",
				"--initial-cluster", $"default=http://127.0.0.1:{peerPort}",
				"--initial-cluster-state", "new"
			},
			RedirectStandardError = true,
			RedirectStandardOutput = true
		});

		await WaitForReadyAsync();

		var setup = new EtcdClient(_endpoint, configureChannelOptions: InsecureChannel);

		await setup.UserAddAsync(new AuthUserAddRequest { Name = "root", Password = "rootpw" });
		await setup.RoleAddAsync(new AuthRoleAddRequest { Name = "root" });
		await setup.UserGrantRoleAsync(new AuthUserGrantRoleRequest { User = "root", Role = "root" });
		await setup.RoleGrantPermissionAsync(new AuthRoleGrantPermissionRequest
		{
			Name = "root",
			Perm = new Permission
			{
				PermType = Permission.Types.Type.Readwrite,
				Key = ByteString.CopyFrom(0x00),
				RangeEnd = ByteString.CopyFrom(0x00)
			}
		});
		await setup.UserAddAsync(new AuthUserAddRequest { Name = "writer", Password = "writerpw" });
		await setup.RoleAddAsync(new AuthRoleAddRequest { Name = "wrole" });
		await setup.RoleGrantPermissionAsync(new AuthRoleGrantPermissionRequest
		{
			Name = "wrole",
			Perm = new Permission
			{
				PermType = Permission.Types.Type.Write,
				Key = ByteString.CopyFromUtf8("/t03w/"),
				RangeEnd = ByteString.CopyFromUtf8("/t03w0")
			}
		});
		await setup.UserGrantRoleAsync(new AuthUserGrantRoleRequest { User = "writer", Role = "wrole" });
		await setup.AuthEnableAsync(new AuthEnableRequest { });

		setup.Dispose();
	}

	[OneTimeTearDown]
	public void StopEtcd()
	{
		try
		{
			if (_etcd is not null && !_etcd.HasExited)
			{
				_etcd.Kill();

				_etcd.WaitForExit(5000);
			}
		}
		finally
		{
			_etcd?.Dispose();

			if (Directory.Exists(_dataDir))
				Directory.Delete(_dataDir, recursive: true);
		}
	}

	[Test]
	public async Task CompetingCreators_ExactlyOneSucceeds()
	{
		var first = CreateClient();
		var second = CreateClient();

		await first.ConnectAsync(RootConfig());
		await second.ConnectAsync(RootConfig());

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

		await client.ConnectAsync(RootConfig());

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

		await client.ConnectAsync(RootConfig());

		using var root = new EtcdClient(_endpoint, "root", "rootpw", configureChannelOptions: InsecureChannel);

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

		await client.ConnectAsync(new EtcdConnectionConfig
		{
			Name = "writer",
			ConnectionString = _endpoint,
			Username = "writer",
			Password = "writerpw"
		});

		var ex = Assert.ThrowsAsync<RpcException>(() => client.CreateKeyAsync("/t03w/k1", "v"));

		Assert.That(ex!.StatusCode, Is.EqualTo(StatusCode.PermissionDenied));
	}

	private EtcdConnectionConfig RootConfig() => new()
	{
		Name = "root",
		ConnectionString = _endpoint,
		Username = "root",
		Password = "rootpw"
	};

	private string UniqueKey() => "/t03/" + Guid.NewGuid().ToString("N") + "/k";

	private static DotnetEtcdBasedClient CreateClient() =>
		new(DotnetEtcdTransportFactory.Create);

	private static void InsecureChannel(GrpcChannelOptions options) => options.Credentials = ChannelCredentials.Insecure;

	private async Task WaitForReadyAsync()
	{
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));

		while (true)
		{
			timeout.Token.ThrowIfCancellationRequested();

			try
			{
				using var probe = new EtcdClient(_endpoint, configureChannelOptions: InsecureChannel);

				await probe.MemberListAsync(new MemberListRequest(), cancellationToken: timeout.Token);

				probe.Dispose();

				return;
			}
			catch when (!timeout.Token.IsCancellationRequested)
			{
				await Task.Delay(200, timeout.Token);
			}
		}
	}

	private static int FreePort()
	{
		var listener = new TcpListener(IPAddress.Loopback, 0);

		listener.Start();

		var port = ((IPEndPoint)listener.LocalEndpoint).Port;

		listener.Stop();

		return port;
	}
}
