using EtcdTerminal.App.Screens.Settings;
using EtcdTerminal.Configuration;

namespace EtcdTerminal.App.Screens.Connections;

public sealed class InstanceToolActions(ManageConnectionsScreen _manageConnections, SettingsScreen _settings)
{
	/// <summary>
	/// Returns whether the instance menu should stay open. It closes on Exit
	/// and after a language change, because the screens of this scope were
	/// built with the old text and the runner rebuilds them in the new one.
	/// </summary>
	public bool Run(InstanceFixedAction action, IReadOnlyList<EtcdConnectionConfig> instances)
	{
		switch (action)
		{
			case InstanceFixedAction.ManageConnections:
				_manageConnections.Show(instances);
				break;
			case InstanceFixedAction.Settings:
				if (_settings.Show())
					return false;
				break;
			case InstanceFixedAction.Exit:
				return false;
		}

		return true;
	}
}
