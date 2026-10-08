using EtcdTerminal.Configuration;

namespace EtcdTerminal.App.Screens.Connections;

public sealed class InstanceSelectionScreen(InstanceMenu _menu, InstanceConnector _connector, InstanceToolActions _tools)
{
	public async Task<EtcdConnectionConfig?> ShowAsync()
	{
		while (true)
		{
			var shown = _menu.Show();
			var choice = shown.Choice;

			if (choice is null)
				return null;

			if (choice.Action is not null)
			{
				if (!_tools.Run(choice.Action.Value, shown.Instances))
					return null;
			}
			else if (choice.Instance is not null)
			{
				var connected = await _connector.ConnectAsync(choice.Instance);

				if (connected is not null)
					return connected;
			}
		}
	}
}
