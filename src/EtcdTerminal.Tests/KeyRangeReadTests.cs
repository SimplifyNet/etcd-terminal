using System.Reflection;
using System.Text;
using Etcdserverpb;
using EtcdTerminal.Configuration;
using EtcdTerminal.Infrastructure;
using EtcdTerminal.Permissions;
using Google.Protobuf;
using Grpc.Core;
using Mvccpb;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class KeyRangeReadTests
{
	[Test]
	public async Task PrefixQuery_SendsByteSuccessorBounds()
	{
		var (transport, captured) = CapturingTransport.Create();
		var client = CreateClient(transport);

		await client.ConnectAsync(new EtcdConnectionConfig { ConnectionString = "http://localhost:2379" });

		captured.RangeHandler = request => new RangeResponse
		{
			Kvs = { new KeyValue { Key = ByteString.CopyFromUtf8("t05p"), Value = ByteString.CopyFromUtf8("v") } }
		};

		var keys = await client.GetKeysByPrefixAsync("t05p");

		Assert.That(keys.Select(kv => kv.Key), Is.EqualTo(["t05p"]));

		var request = captured.RangeRequests.Single();

		Assert.That(request.Key.ToByteArray(), Is.EqualTo(Encoding.UTF8.GetBytes("t05p")));
		Assert.That(request.RangeEnd.ToByteArray(), Is.EqualTo(Encoding.UTF8.GetBytes("t05q")));
		Assert.That(request.RangeEnd.ToByteArray()[^1], Is.EqualTo((byte)'q'));
	}

	[Test]
	public async Task PrefixQuery_EndingInD7FF_SendsNonTextSuccessorBytes()
	{
		var (transport, captured) = CapturingTransport.Create();
		var client = CreateClient(transport);

		await client.ConnectAsync(new EtcdConnectionConfig { ConnectionString = "http://localhost:2379" });

		await client.GetKeysByPrefixAsync("t05p\uD7FF");

		var request = captured.RangeRequests.Single();

		Assert.That(request.Key.ToByteArray(), Is.EqualTo(Encoding.UTF8.GetBytes("t05p\uD7FF")));
		Assert.That(request.RangeEnd.ToByteArray(), Is.EqualTo(new byte[] { 0x74, 0x30, 0x35, 0x70, 0xED, 0x9F, 0xC0 }));
	}

	[Test]
	public async Task EmptyPrefix_SendsAllKeysRequest()
	{
		var (transport, captured) = CapturingTransport.Create();
		var client = CreateClient(transport);

		await client.ConnectAsync(new EtcdConnectionConfig { ConnectionString = "http://localhost:2379" });

		await client.GetKeysByPrefixAsync(string.Empty);

		var request = captured.RangeRequests.Single();

		Assert.That(request.Key.ToByteArray(), Is.EqualTo(new byte[] { 0 }));
		Assert.That(request.RangeEnd.ToByteArray(), Is.EqualTo(new byte[] { 0 }));
	}

	[Test]
	public async Task RangeQuery_SendsExactBounds()
	{
		var (transport, captured) = CapturingTransport.Create();
		var client = CreateClient(transport);

		await client.ConnectAsync(new EtcdConnectionConfig { ConnectionString = "http://localhost:2379" });

		await client.GetKeysByRangeAsync("a", "c");

		var request = captured.RangeRequests.Single();

		Assert.That(request.Key.ToStringUtf8(), Is.EqualTo("a"));
		Assert.That(request.RangeEnd.ToStringUtf8(), Is.EqualTo("c"));
	}

	[Test]
	public async Task OpenEndedRange_SendsZeroByteEnd()
	{
		var (transport, captured) = CapturingTransport.Create();
		var client = CreateClient(transport);

		await client.ConnectAsync(new EtcdConnectionConfig { ConnectionString = "http://localhost:2379" });

		await client.GetKeysByRangeAsync("a", PermissionRange.AllKeys);

		var request = captured.RangeRequests.Single();

		Assert.That(request.Key.ToStringUtf8(), Is.EqualTo("a"));
		Assert.That(request.RangeEnd.ToByteArray(), Is.EqualTo(new byte[] { 0 }));
	}

	[Test]
	public async Task RangeQuery_EmptyEnd_ThrowsWithoutRequest()
	{
		var (transport, captured) = CapturingTransport.Create();
		var client = CreateClient(transport);

		await client.ConnectAsync(new EtcdConnectionConfig { ConnectionString = "http://localhost:2379" });

		Assert.ThrowsAsync<ArgumentException>(() => client.GetKeysByRangeAsync("a", string.Empty));
		Assert.That(captured.RangeRequests, Is.Empty);
	}

	[Test]
	public async Task RangeQuery_DeniedRead_ThrowsAccessDenied()
	{
		var (transport, captured) = CapturingTransport.Create();
		var client = CreateClient(transport);

		await client.ConnectAsync(new EtcdConnectionConfig { ConnectionString = "http://localhost:2379" });

		captured.RangeHandler = _ => throw new RpcException(new Status(StatusCode.PermissionDenied, "denied"));

		var ex = Assert.ThrowsAsync<EtcdOperationException>(() => client.GetKeysByRangeAsync("a", "c"));

		Assert.That(ex!.Kind, Is.EqualTo(EtcdOperationFailureKind.AccessDenied));
	}

	[Test]
	public async Task GrantPrefix_SendsByteSuccessorEnd()
	{
		var (transport, captured) = CapturingTransport.Create();
		var client = CreateClient(transport);

		await client.ConnectAsync(new EtcdConnectionConfig { ConnectionString = "http://localhost:2379" });

		var result = await client.GrantPermissionAsync("role", PermissionType.Read, "t05p\uD7FF", PermissionScope.Prefix);

		Assert.That(result.Success, Is.True);

		var request = captured.GrantRequests.Single();

		Assert.That(request.Perm.Key.ToStringUtf8(), Is.EqualTo("t05p\uD7FF"));
		Assert.That(request.Perm.RangeEnd.ToByteArray(), Is.EqualTo(new byte[] { 0x74, 0x30, 0x35, 0x70, 0xED, 0x9F, 0xC0 }));
	}

	[Test]
	public async Task GrantRange_ThrowsWithoutRequest()
	{
		var (transport, captured) = CapturingTransport.Create();
		var client = CreateClient(transport);

		await client.ConnectAsync(new EtcdConnectionConfig { ConnectionString = "http://localhost:2379" });

		Assert.ThrowsAsync<ArgumentException>(() => client.GrantPermissionAsync("role", PermissionType.Read, "a", PermissionScope.Range));
		Assert.That(captured.GrantRequests, Is.Empty);
	}

	[Test]
	public async Task RevokePrefix_SendsByteSuccessorEnd()
	{
		var (transport, captured) = CapturingTransport.Create();
		var client = CreateClient(transport);

		await client.ConnectAsync(new EtcdConnectionConfig { ConnectionString = "http://localhost:2379" });

		var result = await client.RevokePermissionAsync("role", PermissionType.Read, "/a", PermissionScope.Prefix);

		Assert.That(result.Success, Is.True);

		var request = captured.RevokeRequests.Single();

		Assert.That(request.Key.ToStringUtf8(), Is.EqualTo("/a"));
		Assert.That(request.RangeEnd.ToByteArray(), Is.EqualTo(Encoding.UTF8.GetBytes("/b")));
	}

	[Test]
	public async Task RevokeRange_ThrowsWithoutRequest()
	{
		var (transport, captured) = CapturingTransport.Create();
		var client = CreateClient(transport);

		await client.ConnectAsync(new EtcdConnectionConfig { ConnectionString = "http://localhost:2379" });

		Assert.ThrowsAsync<ArgumentException>(() => client.RevokePermissionAsync("role", PermissionType.Read, "a", PermissionScope.Range));
		Assert.That(captured.RevokeRequests, Is.Empty);
	}

	private static DotnetEtcdBasedClient CreateClient(dotnet_etcd.interfaces.IEtcdClient transport) =>
		new((_, _, _, _) => transport);

	private class CapturingTransport : DispatchProxy
	{
		public List<RangeRequest> RangeRequests { get; } = [];

		public Func<RangeRequest, RangeResponse> RangeHandler { get; set; } = _ => new RangeResponse();

		public List<AuthRoleGrantPermissionRequest> GrantRequests { get; } = [];

		public List<AuthRoleRevokePermissionRequest> RevokeRequests { get; } = [];

		public static (dotnet_etcd.interfaces.IEtcdClient Client, CapturingTransport Captured) Create()
		{
			var client = DispatchProxy.Create<dotnet_etcd.interfaces.IEtcdClient, CapturingTransport>();
			var captured = (CapturingTransport)(object)client;

			return (client, captured);
		}

		protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
		{
			if (targetMethod?.Name == "GetAsync" && args is [RangeRequest request, ..])
			{
				RangeRequests.Add(request);

				return Task.FromResult(RangeHandler(request));
			}

			if (targetMethod?.Name == "MemberListAsync")
				return Task.FromResult(new MemberListResponse());

			if (targetMethod?.Name == "RoleGrantPermissionAsync" && args is [AuthRoleGrantPermissionRequest grant, ..])
			{
				GrantRequests.Add(grant);

				return Task.FromResult(new AuthRoleGrantPermissionResponse());
			}

			if (targetMethod?.Name == "RoleRevokePermissionAsync" && args is [AuthRoleRevokePermissionRequest revoke, ..])
			{
				RevokeRequests.Add(revoke);

				return Task.FromResult(new AuthRoleRevokePermissionResponse());
			}

			if (targetMethod?.Name == "Dispose")
				return null;

			throw new NotSupportedException($"Unexpected transport call: {targetMethod?.Name}.");
		}
	}
}
