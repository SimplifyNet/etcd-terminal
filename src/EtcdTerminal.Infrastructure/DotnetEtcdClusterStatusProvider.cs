using Grpc.Core;
using Grpc.Net.Client;
using Etcdserverpb;
using EtcdTerminal.Cluster;
using EtcdTerminal.Session;

namespace EtcdTerminal.Infrastructure;

public sealed class DotnetEtcdClusterStatusProvider(EtcdConnectionHandle _handle, IConnectionSession _session) : IEtcdClusterStatusProvider
{
	private static readonly TimeSpan StatusTimeout = TimeSpan.FromSeconds(3);

	public Task<IReadOnlyList<EtcdClusterMember>> GetMembersAsync(CancellationToken ct = default) =>
		_handle.Guard<IReadOnlyList<EtcdClusterMember>>(async () =>
		{
			var response = await _handle.Client.MemberListAsync(new MemberListRequest(), cancellationToken: ct);
			var statuses = await Task.WhenAll(response.Members.Select(member => ReadStatusAsync(member, ct)));
			var leader = statuses.FirstOrDefault(status => status is not null)?.Leader ?? 0;

			return [.. response.Members.Select((member, index) =>
			{
				var status = statuses[index];

				return new EtcdClusterMember
				{
					Id = member.ID,
					Name = member.Name,
					Version = status?.Version ?? string.Empty,
					IsLeader = leader != 0 && member.ID == leader,
					IsHealthy = status is { Errors.Count: 0 },
					HasStatus = status is not null,
					DbSize = status?.DbSize ?? 0,
					DbSizeInUse = status?.DbSizeInUse ?? 0,
					RaftTerm = status?.RaftTerm ?? 0,
					RaftIndex = status?.RaftIndex ?? 0,
					RaftAppliedIndex = status?.RaftAppliedIndex ?? 0,
					Errors = status is null ? [] : [.. status.Errors],
					PeerUrls = [.. member.PeerURLs],
					ClientUrls = [.. member.ClientURLs],
					IsLearner = member.IsLearner
				};
			})];
		});

	private async Task<StatusResponse?> ReadStatusAsync(Member member, CancellationToken ct)
	{
		var endpoint = member.ClientURLs.FirstOrDefault();

		if (endpoint is null)
			return null;

		var config = _session.Active;
		var username = config?.IsAuthenticationEnabled is true ? config.Username : null;
		var password = config?.IsAuthenticationEnabled is true ? config.Password : null;
		var useSsl = endpoint.StartsWith("https", StringComparison.OrdinalIgnoreCase);

		using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);

		timeout.CancelAfter(StatusTimeout);

		try
		{
			var client = DotnetEtcdTransportFactory.Create(endpoint, username, password,
				options => options.Credentials = useSsl ? ChannelCredentials.SecureSsl : ChannelCredentials.Insecure);

			try
			{
				return await client.StatusAsync(new StatusRequest(), cancellationToken: timeout.Token);
			}
			finally
			{
				client.Dispose();
			}
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception)
		{
			return null;
		}
	}
}
