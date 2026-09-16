using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.Configuration;
using EtcdTerminal.Localization;
using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Screens;

public sealed class InstanceSelectionScreen(ITerminal _terminal, IConnectionConfigRepository _configRepo, IEtcdClient _etcdClient, SettingsScreen _settings, Menu _menu, Message _message, Prompt _prompt, 	Spinner _spinner)
{
	public async Task<EtcdConnectionConfig?> ShowAsync()
	{
		while (true)
		{
			_terminal.Clear();
			Header.Render(_terminal);

			var instances = _configRepo.LoadInstances();
			var choice = PromptForChoice(instances);

			if (choice is null)
				return null;

			if (Enum.TryParse<InstanceFixedAction>(choice, out var fixedAction))
			{
				switch (fixedAction)
				{
					case InstanceFixedAction.ManageConnections:
						ManageConfigs(instances);
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
				var selected = instances.First(i => i.Name == choice);

				bool connected;

				try
				{
					connected = await _spinner.RunAsync(LocalizationStore.Current.Connecting, async ct =>
					{
						await _etcdClient.ConnectAsync(selected, ct);
					});
				}
				catch (Exception ex)
				{
					_message.ShowError($"Failed to connect: {ex.Message}");

					continue;
				}

				if (!connected)
					_message.ShowWarning(LocalizationStore.Current.OperationCancelled);
				else
					return selected;
			}
		}
	}

	private string? PromptForChoice(IReadOnlyList<EtcdConnectionConfig> instances)
	{
		List<MenuItem<string>> items = [];

		items.AddRange(instances.Select(i => new MenuItem<string>(i.Name, i.Name)));
		items.Add(new MenuItem<string>(nameof(InstanceFixedAction.ManageConnections), LocalizationStore.Current.ManageConnections));
		items.Add(new MenuItem<string>(nameof(InstanceFixedAction.Settings), LocalizationStore.Current.Settings));
		items.Add(new MenuItem<string>(nameof(InstanceFixedAction.Exit), LocalizationStore.Current.Exit));

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

	private void ManageConfigs(IReadOnlyList<EtcdConnectionConfig> instances)
	{
		_terminal.Clear();
		Header.Render(_terminal);

		List<MenuItem<ManageConnectionsAction>> actions = [new(ManageConnectionsAction.AddInstance, LocalizationStore.Current.AddInstance)];

		if (instances.Count > 0)
		{
			actions.Add(new(ManageConnectionsAction.EditInstance, LocalizationStore.Current.EditInstance));
			actions.Add(new(ManageConnectionsAction.RemoveInstance, LocalizationStore.Current.RemoveInstance));
		}

		if (instances.Count > 1)
		{
			actions.Add(new(ManageConnectionsAction.MoveUpInstance, LocalizationStore.Current.MoveUpInstance));
			actions.Add(new(ManageConnectionsAction.MoveDownInstance, LocalizationStore.Current.MoveDownInstance));
		}

		ManageConnectionsAction? action = _menu.Show(LocalizationStore.Current.ManageConnections, actions)?.Id;

		if (action is null)
			return;

		switch (action)
		{
			case ManageConnectionsAction.AddInstance:
				AddInstanceInteractive();
				break;
			case ManageConnectionsAction.EditInstance:
				EditInstanceInteractive(instances);
				break;
			case ManageConnectionsAction.MoveUpInstance:
				MoveInstanceInteractive(instances, -1);
				break;
			case ManageConnectionsAction.MoveDownInstance:
				MoveInstanceInteractive(instances, 1);
				break;
			case ManageConnectionsAction.RemoveInstance:
				RemoveInstanceInteractive(instances);
				break;
		}
	}

	private void AddInstanceInteractive()
	{
		var name = _prompt.Ask(LocalizationStore.Current.EnterInstanceName);

		if (name is null)
			return;

		var connectionString = _prompt.Ask(LocalizationStore.Current.EnterConnStr, LocalizationStore.Current.DefaultConnStr);

		if (connectionString is null)
			return;

		if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
		{
			_message.ShowError(LocalizationStore.Current.InvalidConnStr);

			return;
		}

		var username = _prompt.Ask(LocalizationStore.Current.EnterUsername, allowEmpty: true);

		if (username is null)
			return;

		var password = string.Empty;

		if (!string.IsNullOrEmpty(username))
		{
			var entered = _prompt.Secret(LocalizationStore.Current.EnterPassword);

			if (entered is null)
				return;

			password = entered;
		}

		var config = new EtcdConnectionConfig
		{
			Name = name,
			ConnectionString = connectionString,
			Username = string.IsNullOrEmpty(username) ? null : username,
			Password = string.IsNullOrEmpty(password) ? null : password
		};

		_configRepo.AddInstance(config);

		_message.ShowSuccess(LocalizationStore.Current.InstanceAdded);
	}

	private void EditInstanceInteractive(IReadOnlyList<EtcdConnectionConfig> instances)
	{
		var existingName = _menu.Show(LocalizationStore.Current.SelectInstanceToEdit, instances.Select(i => new MenuItem<string>(i.Name, i.Name)).ToList())?.Id;

		if (existingName is null)
			return;

		var existing = instances.First(i => i.Name == existingName);

		_terminal.Clear();
		Header.Render(_terminal);

		var name = _prompt.Ask(LocalizationStore.Current.EnterInstanceName, existing.Name);

		if (name is null)
			return;

		var connectionString = _prompt.Ask(LocalizationStore.Current.EnterConnStr, existing.ConnectionString);

		if (connectionString is null)
			return;

		if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
		{
			_message.ShowError(LocalizationStore.Current.InvalidConnStr);

			return;
		}

		var username = _prompt.Ask(LocalizationStore.Current.EnterUsername, existing.Username ?? string.Empty);

		if (username is null)
			return;

		var password = string.Empty;

		if (!string.IsNullOrEmpty(username))
		{
			password = existing.Password ?? string.Empty;

			var newPassword = _prompt.Secret(LocalizationStore.Current.EnterPasswordKeepCurrent);

			if (newPassword is null)
				return;

			if (!string.IsNullOrEmpty(newPassword))
				password = newPassword;
		}

		var config = new EtcdConnectionConfig
		{
			Name = name,
			ConnectionString = connectionString,
			Username = string.IsNullOrEmpty(username) ? null : username,
			Password = string.IsNullOrEmpty(password) ? null : password
		};

		_configRepo.UpdateInstance(existingName, config);

		_message.ShowSuccess(LocalizationStore.Current.InstanceUpdated);
	}

	private void MoveInstanceInteractive(IReadOnlyList<EtcdConnectionConfig> instances, int direction)
	{
		var name = _menu.Show(direction < 0 ? LocalizationStore.Current.SelectInstanceToMoveUp : LocalizationStore.Current.SelectInstanceToMoveDown, instances.Select(i => new MenuItem<string>(i.Name, i.Name)).ToList())?.Id;

		if (name is null)
			return;

		if (direction < 0)
			_configRepo.MoveUp(name);
		else
			_configRepo.MoveDown(name);
	}

	private void RemoveInstanceInteractive(IReadOnlyList<EtcdConnectionConfig> instances)
	{
		var nameToRemove = _menu.Show(LocalizationStore.Current.SelectInstanceToRemove, instances.Select(i => new MenuItem<string>(i.Name, i.Name)).ToList())?.Id;

		if (nameToRemove is null)
			return;

		_terminal.WriteLine();

		_configRepo.RemoveInstance(nameToRemove);

		_message.ShowSuccess(LocalizationStore.Current.InstanceRemoved);
	}
}
