using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.Configuration;
using EtcdTerminal.Security;
using EtcdTerminal.Session;
using EtcdTerminal.Localization;
using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Screens;

public sealed class InstanceSelectionScreen(ITerminal _terminal, IConnectionConfigRepository _configRepo, IDecryptFailureSource _decryptFailures, IEtcdConnection _connection, IConnectionSession _session, IUserCapabilitiesProvider _capabilities, SettingsScreen _settings, Menu _menu, Message _message, ScreenLayout _screenLayout, Spinner _spinner, ManageConnectionsScreen _manageConnections)
{
	public async Task<EtcdConnectionConfig?> ShowAsync()
	{
		while (true)
		{
			_screenLayout.RenderHeader();

			var instances = _configRepo.LoadInstances();

			ShowDecryptWarningIfNeeded();

			var choice = PromptForChoice(instances);

			if (choice is null)
				return null;

		if (choice.Action is not null)
		{
			switch (choice.Action)
				{
					case InstanceFixedAction.ManageConnections:
						_manageConnections.Show(instances);
						break;
					case InstanceFixedAction.Settings:
						_settings.Show();
						break;
					case InstanceFixedAction.Exit:
						return null;
				}
			}
		else
		{
			var selected = choice.Instance!;

				bool connected;
				UserCapabilities capabilities = new();

				try
				{
					connected = await _spinner.RunAsync(LocalizationStore.Current.Connecting, async ct =>
					{
						await _connection.ConnectAsync(selected, ct);

						capabilities = await _capabilities.GetCapabilitiesAsync(selected.Username, ct);
					});
				}
				catch (Exception ex)
				{
					_message.ShowError(string.Format(LocalizationStore.Current.FailedToConnect, ex.Message));

					continue;
				}

				if (!connected)
					_message.ShowWarning(LocalizationStore.Current.OperationCancelled);
				else
				{
					_session.Start(selected, capabilities);

					return selected;
				}
			}
		}
	}

	private void ShowDecryptWarningIfNeeded()
	{
		var decryptFailures = _decryptFailures.TakeDecryptFailures();

		if (decryptFailures.Count is 0)
			return;

		_message.ShowWarning(string.Format(LocalizationStore.Current.UndecryptablePasswords, string.Join(", ", decryptFailures)));

		_screenLayout.RenderHeader();
	}

	private InstanceMenuChoice? PromptForChoice(IReadOnlyList<EtcdConnectionConfig> instances)
	{
		List<MenuItem<InstanceMenuChoice>> items = [];

		items.AddRange(instances.Select(i => new MenuItem<InstanceMenuChoice>(new(null, i), i.Name)));

		if (instances.Count > 0)
			items.Add(new MenuItem<InstanceMenuChoice>(new(null, null), string.Empty, IsSelectable: false));

		items.Add(new MenuItem<InstanceMenuChoice>(new(InstanceFixedAction.ManageConnections, null), LocalizationStore.Current.ManageConnections));
		items.Add(new MenuItem<InstanceMenuChoice>(new(InstanceFixedAction.Settings, null), LocalizationStore.Current.Settings));
		items.Add(new MenuItem<InstanceMenuChoice>(new(InstanceFixedAction.Exit, null), LocalizationStore.Current.Exit));

		if (instances.Count == 0)
		{
			_terminal.WriteIndentedLine(LocalizationStore.Current.NoConnectionsMessage, TerminalColor.Warning);
			_terminal.WriteLine();
		}

		return _menu.Show(string.Empty, items, c =>
		{
			var instance = instances.FirstOrDefault(i => i.Name == c);

			return instance is not null
				? $"{instance.Name}  ({instance.ConnectionString})"
				: c;
		})?.Id;
	}
}
