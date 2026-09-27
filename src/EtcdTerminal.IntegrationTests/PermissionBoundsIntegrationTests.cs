using System.Text;
using Authpb;
using dotnet_etcd;
using Etcdserverpb;
using EtcdTerminal.Configuration;
using EtcdTerminal.Infrastructure;
using EtcdTerminal.Keys;
using EtcdTerminal.Permissions;
using EtcdTerminal.Security;
using Google.Protobuf;
using Grpc.Net.Client;
using NUnit.Framework;

namespace EtcdTerminal.IntegrationTests;

[TestFixture]
public sealed class PermissionBoundsIntegrationTests
{
	private EtcdTestServer.RunningServer? _server;
	private string _endpoint = string.Empty;
	private DotnetEtcdBasedClient? _admin;
	private UserCapabilitiesProvider? _capabilities;

	[OneTimeSetUp]
	public async Task StartEtcd()
	{
		_server = await EtcdTestServer.StartAsync();
		_endpoint = _server.Endpoint;

		await EtcdTestServer.EnableAuthAsync(_endpoint);

		using var root = new EtcdClient(_endpoint, "root", "rootpw", configureChannelOptions: InsecureChannel);

		await CreateUserAsync(root, "exact-u", "exact-r", new Permission
		{
			PermType = Permission.Types.Type.Read,
			Key = ByteString.CopyFromUtf8("/t05/exact")
		});
		await CreateUserAsync(root, "pre-u", "pre-r", PrefixGrant("/t05/pre/"));
		await CreateUserAsync(root, "rng-u", "rng-r", new Permission
		{
			PermType = Permission.Types.Type.Read,
			Key = ByteString.CopyFromUtf8("/t05/r1"),
			RangeEnd = ByteString.CopyFromUtf8("/t05/r3")
		});
		await CreateUserAsync(root, "open-u", "open-r", new Permission
		{
			PermType = Permission.Types.Type.Read,
			Key = ByteString.CopyFromUtf8("/t05o/m"),
			RangeEnd = ByteString.CopyFrom(0x00)
		});
		await CreateUserAsync(root, "d7-u", "d7-r", PrefixGrant("/t05d\uD7FF"));

		foreach (var key in AllKeys())
			await root.PutAsync(key, "v");

		_admin = new DotnetEtcdBasedClient(DotnetEtcdTransportFactory.Create);

		await _admin.ConnectAsync(EtcdTestServer.RootConfig(_endpoint));

		_capabilities = new UserCapabilitiesProvider(_admin, _admin, _admin);
	}

	[OneTimeTearDown]
	public void StopEtcd()
	{
		_admin?.Dispose();
		_server?.Dispose();
	}

	[Test]
	public async Task ExactUser_SeesOnlyExactKey()
	{
		var keys = await ReadAsAsync("exact-u");

		Assert.That(keys, Is.EqualTo(["/t05/exact"]));
	}

	[Test]
	public async Task PrefixUser_SeesPrefixOnly()
	{
		var keys = await ReadAsAsync("pre-u");

		Assert.That(keys, Is.EquivalentTo(["/t05/pre/1", "/t05/pre/2"]));
	}

	[Test]
	public async Task RangeUser_SeesBoundedRange()
	{
		var keys = await ReadAsAsync("rng-u");

		Assert.That(keys, Is.EquivalentTo(["/t05/r1", "/t05/r2"]));
	}

	[Test]
	public async Task OpenEndedUser_SeesTail()
	{
		var keys = await ReadAsAsync("open-u");

		Assert.That(keys, Is.EquivalentTo(["/t05o/m", "/t05o/z", "t05plain"]));
	}

	[Test]
	public async Task PrefixEndingInD7FF_MatchesBytePrefix()
	{
		var keys = await ReadAsAsync("d7-u");

		Assert.That(keys, Is.EquivalentTo(["/t05d\uD7FF", "/t05d\uD7FFx"]));
	}

	[Test]
	public async Task Root_SeesAllKeysIncludingPlain()
	{
		using var client = new DotnetEtcdBasedClient(DotnetEtcdTransportFactory.Create);

		await client.ConnectAsync(EtcdTestServer.RootConfig(_endpoint));

		var provider = new ReadableKeysProvider(client);
		var keys = await provider.GetReadableKeysAsync(UserCapabilities.Unrestricted);

		Assert.That(keys.Select(kv => kv.Key), Is.EquivalentTo(AllKeys()));
	}

	private async Task<IReadOnlyList<string>> ReadAsAsync(string username)
	{
		using var client = new DotnetEtcdBasedClient(DotnetEtcdTransportFactory.Create);

		await client.ConnectAsync(UserConfig(username));

		var capabilities = await _capabilities!.GetCapabilitiesAsync(username);
		var provider = new ReadableKeysProvider(client);
		var keys = await provider.GetReadableKeysAsync(capabilities);

		return [.. keys.Select(kv => kv.Key)];
	}

	private EtcdConnectionConfig UserConfig(string username) => new()
	{
		Name = username,
		ConnectionString = _endpoint,
		Username = username,
		Password = username + "-pw"
	};

	private static string[] AllKeys() =>
	[
		"/t05/exact", "/t05/exact2",
		"/t05/pre/1", "/t05/pre/2", "/t05/other",
		"/t05/r1", "/t05/r2", "/t05/r3",
		"/t05o/a", "/t05o/m", "/t05o/z",
		"/t05d\uD7FF", "/t05d\uD7FFx", "/t05d\uE000",
		"t05plain"
	];

	private static Permission PrefixGrant(string prefix) => new()
	{
		PermType = Permission.Types.Type.Read,
		Key = ByteString.CopyFromUtf8(prefix),
		RangeEnd = ByteString.CopyFrom(PermissionRange.PrefixRangeEndBytes(Encoding.UTF8.GetBytes(prefix)))
	};

	private static async Task CreateUserAsync(EtcdClient root, string username, string role, Permission permission)
	{
		await root.UserAddAsync(new AuthUserAddRequest { Name = username, Password = username + "-pw" });
		await root.RoleAddAsync(new AuthRoleAddRequest { Name = role });
		await root.RoleGrantPermissionAsync(new AuthRoleGrantPermissionRequest { Name = role, Perm = permission });
		await root.UserGrantRoleAsync(new AuthUserGrantRoleRequest { User = username, Role = role });
	}

	private static void InsecureChannel(GrpcChannelOptions options) => options.Credentials = Grpc.Core.ChannelCredentials.Insecure;
}
