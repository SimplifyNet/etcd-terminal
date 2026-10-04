using EtcdTerminal.Configuration;
using EtcdTerminal.Security;

namespace EtcdTerminal.Session;

public sealed class ConnectionWorkflow(IEtcdConnection _connection, IUserCapabilitiesProvider _capabilities, IConnectionSession _session) : IConnectionWorkflow
{
	public async Task ConnectAsync(EtcdConnectionConfig config, CancellationToken ct)
	{
		await _connection.ConnectAsync(config, ct);

		try
		{
			var capabilities = await _capabilities.GetCapabilitiesAsync(config.Username, ct);

			_session.Start(config, capabilities);
		}
		catch
		{
			_session.End();

			try
			{
				await _connection.DisconnectAsync();
			}
			catch
			{
			}

			throw;
		}
	}

	public async Task DisconnectAsync()
	{
		_session.End();

		await _connection.DisconnectAsync();
	}
}
