using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Authpb;
using dotnet_etcd;
using Etcdserverpb;
using EtcdTerminal.Configuration;
using Google.Protobuf;
using Grpc.Net.Client;
using Grpc.Core;

namespace EtcdTerminal.IntegrationTests;

internal static class EtcdTestServer
{
	internal sealed class RunningServer(Process process, string dataDirectory, string endpoint) : IDisposable
	{
		public string Endpoint { get; } = endpoint;

		public void Dispose()
		{
			try
			{
				if (!process.HasExited)
				{
					process.Kill();

					process.WaitForExit(5000);
				}
			}
			finally
			{
				process.Dispose();

				if (Directory.Exists(dataDirectory))
					Directory.Delete(dataDirectory, recursive: true);
			}
		}
	}

	internal static async Task<RunningServer> StartAsync()
	{
		var clientPort = FreePort();
		var peerPort = FreePort();
		var dataDirectory = Path.Combine(Path.GetTempPath(), "etcd-test-" + Guid.NewGuid().ToString("N"));
		var endpoint = $"http://127.0.0.1:{clientPort}";

		Directory.CreateDirectory(dataDirectory);

		var process = Process.Start(new ProcessStartInfo
		{
			FileName = "etcd",
			ArgumentList =
			{
				"--data-dir", dataDirectory,
				"--listen-client-urls", endpoint,
				"--advertise-client-urls", endpoint,
				"--listen-peer-urls", $"http://127.0.0.1:{peerPort}",
				"--initial-advertise-peer-urls", $"http://127.0.0.1:{peerPort}",
				"--initial-cluster", $"default=http://127.0.0.1:{peerPort}",
				"--initial-cluster-state", "new"
			},
			RedirectStandardError = true,
			RedirectStandardOutput = true
		})!;

		try
		{
			await WaitForReadyAsync(endpoint);

			return new RunningServer(process, dataDirectory, endpoint);
		}
		catch
		{
			try
			{
				if (!process.HasExited)
				{
					process.Kill();

					process.WaitForExit(5000);
				}
			}
			finally
			{
				process.Dispose();

				if (Directory.Exists(dataDirectory))
					Directory.Delete(dataDirectory, recursive: true);
			}

			throw;
		}
	}

	internal static async Task EnableAuthAsync(string endpoint)
	{
		using var setup = new EtcdClient(endpoint, configureChannelOptions: InsecureChannel);

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
	}

	internal static EtcdConnectionConfig RootConfig(string endpoint) => new()
	{
		Name = "root",
		ConnectionString = endpoint,
		Username = "root",
		Password = "rootpw"
	};

	internal static EtcdConnectionConfig WriterConfig(string endpoint) => new()
	{
		Name = "writer",
		ConnectionString = endpoint,
		Username = "writer",
		Password = "writerpw"
	};

	internal static void InsecureChannel(GrpcChannelOptions options) => options.Credentials = ChannelCredentials.Insecure;

	private static async Task WaitForReadyAsync(string endpoint)
	{
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));

		while (true)
		{
			timeout.Token.ThrowIfCancellationRequested();

			try
			{
				using var probe = new EtcdClient(endpoint, configureChannelOptions: InsecureChannel);

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
