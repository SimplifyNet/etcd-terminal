using System.Reflection;
using Etcdserverpb;
using EtcdTerminal.Configuration;
using EtcdTerminal.Infrastructure;
using Google.Protobuf;
using Grpc.Core;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class DotnetEtcdBasedClientTests
{
	[Test]
	public async Task CreateKey_SendsCreateIfAbsentTransaction()
	{
		var (transport, captured) = CapturingTransport.Create();
		var client = CreateClient(transport);

		await client.ConnectAsync(new EtcdConnectionConfig { ConnectionString = "http://localhost:2379" });

		captured.NextSucceeded = true;

		var created = await client.CreateKeyAsync("k", "v");

		Assert.That(created, Is.True);

		var txn = captured.Transactions.Single();
		var compare = txn.Compare.Single();

		Assert.That(compare.Result, Is.EqualTo(Compare.Types.CompareResult.Equal));
		Assert.That(compare.Target, Is.EqualTo(Compare.Types.CompareTarget.Version));
		Assert.That(compare.Key.ToStringUtf8(), Is.EqualTo("k"));
		Assert.That(compare.Version, Is.EqualTo(0));

		var put = txn.Success.Single().RequestPut;

		Assert.That(put.Key.ToStringUtf8(), Is.EqualTo("k"));
		Assert.That(put.Value.ToStringUtf8(), Is.EqualTo("v"));
		Assert.That(put.IgnoreLease, Is.False);
		Assert.That(txn.Failure, Is.Empty);
	}

	[Test]
	public async Task CreateKey_ExistingKey_ReturnsFalse()
	{
		var (transport, captured) = CapturingTransport.Create();
		var client = CreateClient(transport);

		await client.ConnectAsync(new EtcdConnectionConfig { ConnectionString = "http://localhost:2379" });

		captured.NextSucceeded = false;

		Assert.That(await client.CreateKeyAsync("k", "v"), Is.False);
	}

	[Test]
	public async Task UpdateKey_SendsUpdateIfPresentTransactionPreservingLease()
	{
		var (transport, captured) = CapturingTransport.Create();
		var client = CreateClient(transport);

		await client.ConnectAsync(new EtcdConnectionConfig { ConnectionString = "http://localhost:2379" });

		captured.NextSucceeded = true;

		var updated = await client.UpdateKeyAsync("k", "v");

		Assert.That(updated, Is.True);

		var txn = captured.Transactions.Single();
		var compare = txn.Compare.Single();

		Assert.That(compare.Result, Is.EqualTo(Compare.Types.CompareResult.Greater));
		Assert.That(compare.Target, Is.EqualTo(Compare.Types.CompareTarget.Version));
		Assert.That(compare.Key.ToStringUtf8(), Is.EqualTo("k"));
		Assert.That(compare.Version, Is.EqualTo(0));

		var put = txn.Success.Single().RequestPut;

		Assert.That(put.Key.ToStringUtf8(), Is.EqualTo("k"));
		Assert.That(put.Value.ToStringUtf8(), Is.EqualTo("v"));
		Assert.That(put.IgnoreLease, Is.True);
		Assert.That(txn.Failure, Is.Empty);
	}

	[Test]
	public async Task UpdateKey_MissingKey_ReturnsFalse()
	{
		var (transport, captured) = CapturingTransport.Create();
		var client = CreateClient(transport);

		await client.ConnectAsync(new EtcdConnectionConfig { ConnectionString = "http://localhost:2379" });

		captured.NextSucceeded = false;

		Assert.That(await client.UpdateKeyAsync("k", "v"), Is.False);
	}

	[Test]
	public async Task CreateKey_DeniedRequest_ThrowsInsteadOfFalse()
	{
		var (transport, captured) = CapturingTransport.Create();
		var client = CreateClient(transport);

		await client.ConnectAsync(new EtcdConnectionConfig { ConnectionString = "http://localhost:2379" });

		captured.NextError = new RpcException(new Status(StatusCode.PermissionDenied, "permission denied"));

		var ex = Assert.ThrowsAsync<RpcException>(() => client.CreateKeyAsync("k", "v"));

		Assert.That(ex!.StatusCode, Is.EqualTo(StatusCode.PermissionDenied));
	}

	[Test]
	public async Task UpdateKey_DeniedRequest_ThrowsInsteadOfFalse()
	{
		var (transport, captured) = CapturingTransport.Create();
		var client = CreateClient(transport);

		await client.ConnectAsync(new EtcdConnectionConfig { ConnectionString = "http://localhost:2379" });

		captured.NextError = new RpcException(new Status(StatusCode.PermissionDenied, "permission denied"));

		var ex = Assert.ThrowsAsync<RpcException>(() => client.UpdateKeyAsync("k", "v"));

		Assert.That(ex!.StatusCode, Is.EqualTo(StatusCode.PermissionDenied));
	}

	[Test]
	public async Task Disconnect_DisposesTransport()
	{
		var (transport, captured) = CapturingTransport.Create();
		var client = CreateClient(transport);

		await client.ConnectAsync(new EtcdConnectionConfig { ConnectionString = "http://localhost:2379" });

		Assert.That(captured.Disposed, Is.False);

		client.Disconnect();

		Assert.That(captured.Disposed, Is.True);
	}

	private static DotnetEtcdBasedClient CreateClient(dotnet_etcd.interfaces.IEtcdClient transport) =>
		new((_, _, _, _) => transport);

	private class CapturingTransport : DispatchProxy
	{
		public List<TxnRequest> Transactions { get; } = [];

		public bool NextSucceeded { get; set; } = true;

		public Exception? NextError { get; set; }

		public bool Disposed { get; private set; }

		public static (dotnet_etcd.interfaces.IEtcdClient Client, CapturingTransport Captured) Create()
		{
			var client = DispatchProxy.Create<dotnet_etcd.interfaces.IEtcdClient, CapturingTransport>();
			var captured = (CapturingTransport)(object)client;

			return (client, captured);
		}

		protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
		{
			if (targetMethod?.Name == "TransactionAsync" && args is [TxnRequest request, ..])
			{
				Transactions.Add(request);

				if (NextError is not null)
					return Task.FromException<TxnResponse>(NextError);

				return Task.FromResult(new TxnResponse { Succeeded = NextSucceeded });
			}

			if (targetMethod?.Name == "MemberListAsync")
				return Task.FromResult(new MemberListResponse());

			if (targetMethod?.Name == "Dispose")
			{
				Disposed = true;

				return null;
			}

			throw new NotSupportedException($"Unexpected transport call: {targetMethod?.Name}.");
		}
	}
}
