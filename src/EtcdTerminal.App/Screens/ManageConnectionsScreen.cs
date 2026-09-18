using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.Configuration;
using EtcdTerminal.Localization;
using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Screens;

public sealed class ManageConnectionsScreen(ITerminalOutput _terminal, ScreenLayout _screenLayout, IConnectionConfigRepository _configRepo, Menu _menu, Prompt _prompt, Message _message, ILocalization _localization)
{
	public void Show(IReadOnlyList<EtcdConnectionConfig> instances)
	{
		_screenLayout.RenderHeader();

		List<MenuItem<ManageConnectionsAction>> actions = [new(ManageConnectionsAction.AddInstance, _localization.AddInstance)];

		if (instances.Count > 0)
		{
			actions.Add(new(ManageConnectionsAction.EditInstance, _localization.EditInstance));
			actions.Add(new(ManageConnectionsAction.RemoveInstance, _localization.RemoveInstance));
		}

		if (instances.Count > 1)
		{
			actions.Add(new(ManageConnectionsAction.MoveUpInstance, _localization.MoveUpInstance));
			actions.Add(new(ManageConnectionsAction.MoveDownInstance, _localization.MoveDownInstance));
		}

		ManageConnectionsAction? action = _menu.Show(_localization.ManageConnections, actions)?.Id;

		if (action is null)
			return;

		switch (action)
		{
			case ManageConnectionsAction.AddInstance:
				SaveInstanceInteractive(null, _localization.InstanceAdded);
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

	private void EditInstanceInteractive(IReadOnlyList<EtcdConnectionConfig> instances)
	{
		var existingName = _menu.Show(_localization.SelectInstanceToEdit, instances.Select(i => new MenuItem<string>(i.Name, i.Name)).ToList())?.Id;

		if (existingName is null)
			return;

		var existing = instances.First(i => i.Name == existingName);

		_screenLayout.RenderHeader();

		SaveInstanceInteractive(existing, _localization.InstanceUpdated);
	}

	private void SaveInstanceInteractive(EtcdConnectionConfig? existing, string successMessage)
	{
		var name = existing is null
			? _prompt.Ask(_localization.EnterInstanceName)
			: _prompt.Ask(_localization.EnterInstanceName, existing.Name);

		if (name is null)
			return;

		var connectionString = _prompt.Ask(_localization.EnterConnStr, existing?.ConnectionString ?? _localization.DefaultConnStr);

		if (connectionString is null)
			return;

		if (!new EtcdConnectionConfig { ConnectionString = connectionString }.IsConnectionStringValid)
		{
			_message.ShowError(_localization.InvalidConnStr);

			return;
		}

		var username = existing is null
			? _prompt.Ask(_localization.EnterUsername, allowEmpty: true)
			: _prompt.Ask(_localization.EnterUsername, existing.Username ?? string.Empty);

		if (username is null)
			return;

		var password = string.Empty;

		if (!string.IsNullOrEmpty(username))
		{
			password = existing?.Password ?? string.Empty;

			var passwordPrompt = existing is null
				? _localization.EnterPassword
				: _localization.EnterPasswordKeepCurrent;

			var entered = _prompt.Secret(passwordPrompt);

			if (entered is null)
				return;

			if (existing is null || !string.IsNullOrEmpty(entered))
				password = entered;
		}

		var config = new EtcdConnectionConfig
		{
			Name = name,
			ConnectionString = connectionString,
			Username = string.IsNullOrEmpty(username) ? null : username,
			Password = string.IsNullOrEmpty(password) ? null : password
		};

		if (existing is null)
			_configRepo.AddInstance(config);
		else
			_configRepo.UpdateInstance(existing.Name, config);

		_message.ShowSuccess(successMessage);
	}

	private void MoveInstanceInteractive(IReadOnlyList<EtcdConnectionConfig> instances, int direction)
	{
		var name = _menu.Show(direction < 0 ? _localization.SelectInstanceToMoveUp : _localization.SelectInstanceToMoveDown, instances.Select(i => new MenuItem<string>(i.Name, i.Name)).ToList())?.Id;

		if (name is null)
			return;

		if (direction < 0)
			_configRepo.MoveUp(name);
		else
			_configRepo.MoveDown(name);
	}

	private void RemoveInstanceInteractive(IReadOnlyList<EtcdConnectionConfig> instances)
	{
		var nameToRemove = _menu.Show(_localization.SelectInstanceToRemove, instances.Select(i => new MenuItem<string>(i.Name, i.Name)).ToList())?.Id;

		if (nameToRemove is null)
			return;

		_terminal.WriteLine();

		_configRepo.RemoveInstance(nameToRemove);

		_message.ShowSuccess(_localization.InstanceRemoved);
	}
}
