using EtcdTerminal.App.Screens.Settings;
using EtcdTerminal.Configuration;

namespace EtcdTerminal.App.Screens.Connections;

public sealed class InstanceToolActions(ManageConnectionsScreen _manageConnections, SettingsScreen _settings)
{
	public bool Run(InstanceFixedAction action, IReadOnlyList<EtcdConnectionConfig> instances)
	{
		switch (action)
		{
			case InstanceFixedAction.ManageConnections:
				_manageConnections.Show(instances);
				break;
			case InstanceFixedAction.Settings:
				_settings.Show();
				break;
			case InstanceFixedAction.Exit:
				return false;
		}

		return true;
	}
}
