using EtcdTerminal.App.Components;
using EtcdTerminal.Configuration;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Session;
using EtcdTerminal.Presentation;

namespace EtcdTerminal.App.Screens.Connections;

public sealed class InstanceConnector(IConnectionWorkflow _workflow, Spinner _spinner, Message _message, ILocalization _localization)
{
	public async Task<EtcdConnectionConfig?> ConnectAsync(EtcdConnectionConfig selected)
	{
		bool connected;

		try
		{
			connected = await _spinner.RunAsync(_localization.Connecting, ct => _workflow.ConnectAsync(selected, ct));
		}
		catch (Exception ex)
		{
			_message.ShowError(string.Format(_localization.FailedToConnect, ex.Message));

			return null;
		}

		if (connected)
			return selected;

		_message.ShowWarning(_localization.OperationCancelled);

		return null;
	}
}
