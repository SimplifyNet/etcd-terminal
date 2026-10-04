using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Etcdserverpb;
using EtcdTerminal.Configuration;
using EtcdTerminal.Infrastructure;
using Grpc.Core;
using Mvccpb;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class EtcdOperationFailureTests
{
	[Test]
	public async Task GetUser_MissingUser_ReturnsNull()
	{
		var client = CreateClient(name => name switch
		{
			"MemberListAsync" => Task.FromResult(new MemberListResponse()),
			"Dispose" => null,
			"UserGetAsync" => Task.FromException<AuthUserGetResponse>(MissingUser()),
			_ => throw new NotSupportedException($"Unexpected transport call: {name}.")
		});

		await client.ConnectAsync(new EtcdConnectionConfig { ConnectionString = "http://localhost:2379" });

		Assert.That(await client.GetUserAsync("ghost"), Is.Null);
	}

	[Test]
	public async Task GetUser_ExistingUser_ReturnsMappedUser()
	{
		var client = CreateClient(name => name switch
		{
			"MemberListAsync" => Task.FromResult(new MemberListResponse()),
			"Dispose" => null,
			"UserGetAsync" => Task.FromResult(new AuthUserGetResponse { Roles = { "dev" } }),
			_ => throw new NotSupportedException($"Unexpected transport call: {name}.")
		});

		await client.ConnectAsync(new EtcdConnectionConfig { ConnectionString = "http://localhost:2379" });

		var user = await client.GetUserAsync("dev");

		Assert.That(user!.Username, Is.EqualTo("dev"));
		Assert.That(user.Roles, Is.EqualTo(["dev"]));
	}

	[Test]
	public void GetUser_OtherFailedPrecondition_ThrowsTransportErrorInsteadOfNull()
	{
		var client = CreateClient(name => name switch
		{
			"MemberListAsync" => Task.FromResult(new MemberListResponse()),
			"Dispose" => null,
			"UserGetAsync" => Task.FromException<AuthUserGetResponse>(FailedPrecondition("etcdserver: authentication is not enabled")),
			_ => throw new NotSupportedException($"Unexpected transport call: {name}.")
		});

		var ex = Assert.ThrowsAsync<EtcdOperationException>(async () =>
		{
			await client.ConnectAsync(new EtcdConnectionConfig { ConnectionString = "http://localhost:2379" });

			await client.GetUserAsync("ghost");
		});

		Assert.That(ex!.Kind, Is.EqualTo(EtcdOperationFailureKind.TransportError));
	}

	[Test]
	public async Task GetRole_MissingRole_ReturnsNull()
	{
		var client = CreateClient(name => name switch
		{
			"MemberListAsync" => Task.FromResult(new MemberListResponse()),
			"Dispose" => null,
			"RoleGetAsync" => Task.FromException<AuthRoleGetResponse>(MissingRole()),
			_ => throw new NotSupportedException($"Unexpected transport call: {name}.")
		});

		await client.ConnectAsync(new EtcdConnectionConfig { ConnectionString = "http://localhost:2379" });

		Assert.That(await client.GetRoleAsync("ghost"), Is.Null);
	}

	[Test]
	public void GetKey_DeniedRead_ThrowsAccessDeniedInsteadOfNull()
	{
		var client = CreateClient(name => name switch
		{
			"MemberListAsync" => Task.FromResult(new MemberListResponse()),
			"Dispose" => null,
			"GetAsync" => Task.FromException<RangeResponse>(Denied()),
			_ => throw new NotSupportedException($"Unexpected transport call: {name}.")
		});

		var ex = Assert.ThrowsAsync<EtcdOperationException>(async () =>
		{
			await client.ConnectAsync(new EtcdConnectionConfig { ConnectionString = "http://localhost:2379" });

			await client.GetKeyAsync("k");
		});

		Assert.That(ex!.Kind, Is.EqualTo(EtcdOperationFailureKind.AccessDenied));
	}

	[Test]
	public async Task GetKeysByPrefix_DeniedRead_ThrowsAccessDeniedInsteadOfEmpty()
	{
		var client = CreateClient(name => name switch
		{
			"MemberListAsync" => Task.FromResult(new MemberListResponse()),
			"Dispose" => null,
			"GetAsync" => Task.FromException<RangeResponse>(Denied()),
			_ => throw new NotSupportedException($"Unexpected transport call: {name}.")
		});

		await client.ConnectAsync(new EtcdConnectionConfig { ConnectionString = "http://localhost:2379" });

		var ex = Assert.ThrowsAsync<EtcdOperationException>(() => client.GetKeysByPrefixAsync("/"));

		Assert.That(ex!.Kind, Is.EqualTo(EtcdOperationFailureKind.AccessDenied));
	}

	[Test]
	public async Task GetKeysByPrefix_EmptySuccess_ReturnsEmpty()
	{
		var client = CreateClient(name => name switch
		{
			"MemberListAsync" => Task.FromResult(new MemberListResponse()),
			"Dispose" => null,
			"GetAsync" => Task.FromResult(new RangeResponse()),
			_ => throw new NotSupportedException($"Unexpected transport call: {name}.")
		});

		await client.ConnectAsync(new EtcdConnectionConfig { ConnectionString = "http://localhost:2379" });

		Assert.That(await client.GetKeysByPrefixAsync("/"), Is.Empty);
	}

	[Test]
	public void GetKeysByPrefix_Unavailable_ThrowsUnavailable()
	{
		var client = CreateClient(name => name switch
		{
			"MemberListAsync" => Task.FromResult(new MemberListResponse()),
			"Dispose" => null,
			"GetAsync" => Task.FromException<RangeResponse>(new RpcException(new Status(StatusCode.Unavailable, "Error connecting to subchannel."))),
			_ => throw new NotSupportedException($"Unexpected transport call: {name}.")
		});

		var ex = Assert.ThrowsAsync<EtcdOperationException>(async () =>
		{
			await client.ConnectAsync(new EtcdConnectionConfig { ConnectionString = "http://localhost:2379" });

			await client.GetKeysByPrefixAsync("/");
		});

		Assert.That(ex!.Kind, Is.EqualTo(EtcdOperationFailureKind.Unavailable));
	}

	[Test]
	public void GetKey_DeadlineExceeded_ThrowsUnconfirmed()
	{
		var client = CreateClient(name => name switch
		{
			"MemberListAsync" => Task.FromResult(new MemberListResponse()),
			"Dispose" => null,
			"GetAsync" => Task.FromException<RangeResponse>(new RpcException(new Status(StatusCode.DeadlineExceeded, "Deadline Exceeded"))),
			_ => throw new NotSupportedException($"Unexpected transport call: {name}.")
		});

		var ex = Assert.ThrowsAsync<EtcdOperationException>(async () =>
		{
			await client.ConnectAsync(new EtcdConnectionConfig { ConnectionString = "http://localhost:2379" });

			await client.GetKeyAsync("k");
		});

		Assert.That(ex!.Kind, Is.EqualTo(EtcdOperationFailureKind.Unconfirmed));
	}

	[Test]
	public async Task CreateUser_InvalidAuthentication_ReturnsFailure()
	{
		var client = CreateClient(name => name switch
		{
			"MemberListAsync" => Task.FromResult(new MemberListResponse()),
			"Dispose" => null,
			"UserAddAsync" => Task.FromException<AuthUserAddResponse>(InvalidAuth()),
			_ => throw new NotSupportedException($"Unexpected transport call: {name}.")
		});

		await client.ConnectAsync(new EtcdConnectionConfig { ConnectionString = "http://localhost:2379" });

		var result = await client.CreateUserAsync("bob", "pw");

		Assert.That(result.Success, Is.False);
		Assert.That(result.ErrorMessage, Does.Contain("authentication failed"));
		Assert.That(result.Kind, Is.EqualTo(EtcdOperationFailureKind.AccessDenied));
	}

	[Test]
	public void CreateUser_CancelledRpc_PropagatesInsteadOfFailure()
	{
		var client = CreateClient(name => name switch
		{
			"MemberListAsync" => Task.FromResult(new MemberListResponse()),
			"Dispose" => null,
			"UserAddAsync" => Task.FromException<AuthUserAddResponse>(Cancelled()),
			_ => throw new NotSupportedException($"Unexpected transport call: {name}.")
		});

		Assert.ThrowsAsync<RpcException>(async () =>
		{
			await client.ConnectAsync(new EtcdConnectionConfig { ConnectionString = "http://localhost:2379" });

			await client.CreateUserAsync("bob", "pw");
		});
	}

	[Test]
	public void GetKey_CancelledToken_PropagatesInsteadOfNull()
	{
		var client = CreateClient(name => name switch
		{
			"MemberListAsync" => Task.FromResult(new MemberListResponse()),
			"Dispose" => null,
			"GetAsync" => Task.FromException<RangeResponse>(new OperationCanceledException()),
			_ => throw new NotSupportedException($"Unexpected transport call: {name}.")
		});

		Assert.ThrowsAsync<OperationCanceledException>(async () =>
		{
			await client.ConnectAsync(new EtcdConnectionConfig { ConnectionString = "http://localhost:2379" });

			await client.GetKeyAsync("k");
		});
	}

	[Test]
	public void Connect_UnavailableServer_ThrowsUnavailable()
	{
		using var client = new DotnetEtcdBasedClient(DotnetEtcdTransportFactory.Create);

		var ex = Assert.ThrowsAsync<EtcdOperationException>(() =>
			client.ConnectAsync(new EtcdConnectionConfig { ConnectionString = $"http://127.0.0.1:{ClosedPort()}" }));

		Assert.That(ex!.Kind, Is.EqualTo(EtcdOperationFailureKind.Unavailable));
	}

	[Test]
	public void Connect_CancelledToken_PropagatesCancellation()
	{
		using var client = new DotnetEtcdBasedClient(DotnetEtcdTransportFactory.Create);
		using var cts = new CancellationTokenSource();

		cts.Cancel();

		Assert.ThrowsAsync<OperationCanceledException>(() =>
			client.ConnectAsync(new EtcdConnectionConfig { ConnectionString = "http://localhost:2379" }, cts.Token));
	}

	private static DotnetEtcdBasedClient CreateClient(Func<string, object?> responder) =>
		new((_, _, _, _) => ResponderTransport.Create(responder));

	private static RpcException MissingUser() =>
		FailedPrecondition("etcdserver: user name not found");

	private static RpcException MissingRole() =>
		FailedPrecondition("etcdserver: role name not found");

	private static RpcException Denied() =>
		new(new Status(StatusCode.PermissionDenied, "etcdserver: permission denied"));

	private static RpcException InvalidAuth() =>
		new(new Status(StatusCode.InvalidArgument, "etcdserver: authentication failed, invalid user ID or password"));

	private static RpcException Cancelled() =>
		new(new Status(StatusCode.Cancelled, "Cancelled"));

	private static RpcException FailedPrecondition(string detail) =>
		new(new Status(StatusCode.FailedPrecondition, detail));

	private static int ClosedPort()
	{
		var listener = new TcpListener(IPAddress.Loopback, 0);

		listener.Start();

		var port = ((IPEndPoint)listener.LocalEndpoint).Port;

		listener.Stop();

		return port;
	}

	private class ResponderTransport : DispatchProxy
	{
		public Func<string, object?> Responder { get; set; } = name => throw new NotSupportedException($"Unexpected transport call: {name}.");

		public static dotnet_etcd.interfaces.IEtcdClient Create(Func<string, object?> responder)
		{
			var client = DispatchProxy.Create<dotnet_etcd.interfaces.IEtcdClient, ResponderTransport>();
			((ResponderTransport)(object)client).Responder = responder;

			return client;
		}

		protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
			Responder(targetMethod?.Name ?? string.Empty);
	}
}
