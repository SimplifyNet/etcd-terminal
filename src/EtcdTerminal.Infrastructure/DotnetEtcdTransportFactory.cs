using dotnet_etcd;
using Grpc.Net.Client;

namespace EtcdTerminal.Infrastructure;

public static class DotnetEtcdTransportFactory
{
	public static dotnet_etcd.interfaces.IEtcdClient Create(
		string connectionString,
		string? username,
		string? password,
		Action<GrpcChannelOptions> configureChannel) =>
			username is null
				? new EtcdClient(connectionString, configureChannelOptions: configureChannel)
				: new EtcdClient(connectionString, username, password!, configureChannelOptions: configureChannel);
}
