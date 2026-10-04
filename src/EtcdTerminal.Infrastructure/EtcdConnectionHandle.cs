using dotnet_etcd;
using Etcdserverpb;
using EtcdTerminal.Configuration;
using Grpc.Core;
using Grpc.Net.Client;

namespace EtcdTerminal.Infrastructure;

public sealed class EtcdConnectionHandle(Func<string, string?, string?, Action<GrpcChannelOptions>, dotnet_etcd.interfaces.IEtcdClient> _createTransport) : IEtcdConnection
{
	private dotnet_etcd.interfaces.IEtcdClient? _client;

	internal dotnet_etcd.interfaces.IEtcdClient Client => _client ?? throw new InvalidOperationException("Not connected to etcd.");

	internal EtcdClient ConcreteClient => Client as EtcdClient ?? throw new InvalidOperationException("Connected transport does not expose connection access.");

	public async Task ConnectAsync(EtcdConnectionConfig config, CancellationToken ct = default)
	{
		Disconnect();

		var connectionString = config.ConnectionString;
		var useSsl = connectionString.StartsWith("https", StringComparison.OrdinalIgnoreCase);

		void configureChannel(GrpcChannelOptions options)
		{
			options.Credentials = useSsl
				? ChannelCredentials.SecureSsl
				: ChannelCredentials.Insecure;
		}

		_client = config.IsAuthenticationEnabled
			? _createTransport(connectionString, config.Username, config.Password, configureChannel)
			: _createTransport(connectionString, null, null, configureChannel);

		try
		{
			await Guard(() => ProbeAsync(ct));
		}
		catch
		{
			Disconnect();

			throw;
		}
	}

	public Task DisconnectAsync()
	{
		Disconnect();

		return Task.CompletedTask;
	}

	public void Disconnect()
	{
		_client?.Dispose();

		_client = null;
	}

	public void Dispose() => Disconnect();

	internal async Task<T> Guard<T>(Func<Task<T>> operation, Func<RpcException, T>? onError = null)
	{
		try
		{
			return await operation();
		}
		catch (RpcException ex) when (GrpcErrorTranslator.IsCancellation(ex))
		{
			throw;
		}
		catch (RpcException ex) when (onError is not null)
		{
			return onError(ex);
		}
		catch (RpcException ex)
		{
			throw GrpcErrorTranslator.Translate(ex);
		}
	}

	/// <summary>
	/// Verifies connectivity and credentials. MemberList is used instead of a key read because it
	/// requires no key permissions, so non-root accounts can connect too.
	/// </summary>
	private Task<MemberListResponse> ProbeAsync(CancellationToken ct) =>
		Client.MemberListAsync(new MemberListRequest(), cancellationToken: ct);
}
