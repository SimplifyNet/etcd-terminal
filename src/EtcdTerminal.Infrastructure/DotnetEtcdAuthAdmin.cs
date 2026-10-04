using Etcdserverpb;
using EtcdTerminal.Security;
using Grpc.Core;

namespace EtcdTerminal.Infrastructure;

public sealed class DotnetEtcdAuthAdmin(EtcdConnectionHandle _handle) : IEtcdAuthAdmin
{
	public Task<bool> IsAuthenticationEnabledAsync(CancellationToken ct = default) =>
		_handle.Guard<bool>(async () =>
		{
			try
			{
				var call = _handle.ConcreteClient.GetConnection().AuthClient.AuthStatusAsync(new AuthStatusRequest(), null, null, ct);
				var response = await call.ResponseAsync;

				return response.Enabled;
			}
			catch (RpcException ex) when (ex.StatusCode == StatusCode.PermissionDenied)
			{
				// Only an authenticated session can be denied here, so auth is definitely on.
				return true;
			}
		});
}
