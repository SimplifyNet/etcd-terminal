using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.Configuration;
using EtcdTerminal.Localization;
using Spectre.Console;

namespace EtcdTerminal.App.Screens;

public sealed class InstanceSelectionScreen(IConnectionConfigRepository _configRepo, IEtcdClient _etcdClient, SettingsScreen _settings, Menu _menu, PressAnyKeyPrompt _pressAnyKey)
{
	public async Task<EtcdConnectionConfig?> ShowAsync()
	{
		while (true)
		{
			AnsiConsole.Clear();
			Header.Render();

			var instances = _configRepo.LoadInstances();

			var choice = PromptForChoice(instances);

			if (choice is null)
				return null;

			if (choice == LocalizationStore.Current.ManageConnections)
				ManageConfigs(instances);
			else if (choice == LocalizationStore.Current.Settings)
				_settings.Show();
			else if (choice == LocalizationStore.Current.Exit)
				return null;
			else
			{
				var selected = instances.First(i => i.Name == choice);

				try
				{
					await AnsiConsole.Status()
						.StartAsync(LocalizationStore.Current.Connecting, async ctx =>
						{
							await _etcdClient.ConnectAsync(selected);
							await _etcdClient.PingAsync();
						});

					AnsiConsole.MarkupLine(LocalizationStore.Current.ConnectedSuccess);

					return selected;
				}
				catch (Exception ex)
				{
					AnsiConsole.MarkupLine($"[red]Failed to connect:[/] {ex.Message}");
					AnsiConsole.WriteLine();
					_pressAnyKey.Show();
				}
			}
		}
	}

	private string? PromptForChoice(IReadOnlyList<EtcdConnectionConfig> instances)
	{
		var choices = new List<string>();

		choices.AddRange(instances.Select(i => i.Name));
		choices.Add(LocalizationStore.Current.ManageConnections);
		choices.Add(LocalizationStore.Current.Settings);
		choices.Add(LocalizationStore.Current.Exit);

		if (instances.Count == 0)
		{
			AnsiConsole.MarkupLine($"[yellow]{LocalizationStore.Current.NoConnectionsMessage}[/]");
			AnsiConsole.WriteLine();
		}

		return _menu.Show(string.Empty, choices, c =>
		{
			var instance = instances.FirstOrDefault(i => i.Name == c);

			return instance is not null
				? $"{instance.Name}  ({instance.ConnectionString})"
				: c;
		});
	}

	private void ManageConfigs(IReadOnlyList<EtcdConnectionConfig> instances)
	{
		AnsiConsole.Clear();
		Header.Render();

		var manageChoices = new List<string> { LocalizationStore.Current.AddInstance };

		if (instances.Count > 0)
		{
			manageChoices.Add(LocalizationStore.Current.EditInstance);
			manageChoices.Add(LocalizationStore.Current.RemoveInstance);
		}

		if (instances.Count > 1)
		{
			manageChoices.Add(LocalizationStore.Current.MoveUpInstance);
			manageChoices.Add(LocalizationStore.Current.MoveDownInstance);
		}

		var action = _menu.Show(LocalizationStore.Current.ManageConnections, manageChoices);

		if (action is null)
			return;

		if (action == LocalizationStore.Current.AddInstance)
			AddInstanceInteractive();
		else if (action == LocalizationStore.Current.EditInstance)
			EditInstanceInteractive(instances);
		else if (action == LocalizationStore.Current.MoveUpInstance)
			MoveInstanceInteractive(instances, -1);
		else if (action == LocalizationStore.Current.MoveDownInstance)
			MoveInstanceInteractive(instances, 1);
		else if (action == LocalizationStore.Current.RemoveInstance)
			RemoveInstanceInteractive(instances);
	}

	private void AddInstanceInteractive()
	{
		var name = Prompt.Ask(LocalizationStore.Current.EnterInstanceName);

		if (name is null)
			return;

		var connectionString = Prompt.Ask(LocalizationStore.Current.EnterConnStr, LocalizationStore.Current.DefaultConnStr);

		if (connectionString is null)
			return;

		if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
		{
			AnsiConsole.MarkupLine(LocalizationStore.Current.InvalidConnStr);
			_pressAnyKey.Show();

			return;
		}

		var username = Prompt.Ask(LocalizationStore.Current.EnterUsername, allowEmpty: true);

		if (username is null)
			return;

		var password = string.Empty;

		if (!string.IsNullOrEmpty(username))
		{
			var entered = Prompt.Secret(LocalizationStore.Current.EnterPassword);

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

		AnsiConsole.MarkupLine(LocalizationStore.Current.InstanceAdded);
		_pressAnyKey.Show();
	}

	private void EditInstanceInteractive(IReadOnlyList<EtcdConnectionConfig> instances)
	{
		var existingName = _menu.Show(LocalizationStore.Current.SelectInstanceToEdit, instances.Select(i => i.Name));

		if (existingName is null)
			return;

		var existing = instances.First(i => i.Name == existingName);

		AnsiConsole.Clear();
		Header.Render();

		var name = Prompt.Ask(LocalizationStore.Current.EnterInstanceName, existing.Name);

		if (name is null)
			return;

		var connectionString = Prompt.Ask(LocalizationStore.Current.EnterConnStr, existing.ConnectionString);

		if (connectionString is null)
			return;

		if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
		{
			AnsiConsole.MarkupLine(LocalizationStore.Current.InvalidConnStr);
			_pressAnyKey.Show();

			return;
		}

		var username = Prompt.Ask(LocalizationStore.Current.EnterUsername, existing.Username ?? string.Empty);

		if (username is null)
			return;

		var password = string.Empty;

		if (!string.IsNullOrEmpty(username))
		{
			password = existing.Password ?? string.Empty;

			if (Prompt.Confirm(LocalizationStore.Current.ChangePassword))
			{
				var newPassword = Prompt.Secret(LocalizationStore.Current.EnterPassword);

				if (newPassword is null)
					return;

				password = newPassword;
			}
		}

		var config = new EtcdConnectionConfig
		{
			Name = name,
			ConnectionString = connectionString,
			Username = string.IsNullOrEmpty(username) ? null : username,
			Password = string.IsNullOrEmpty(password) ? null : password
		};

		_configRepo.AddInstance(config);

		AnsiConsole.MarkupLine(LocalizationStore.Current.InstanceUpdated);
		_pressAnyKey.Show();
	}

	private void MoveInstanceInteractive(IReadOnlyList<EtcdConnectionConfig> instances, int direction)
	{
		var name = _menu.Show(direction < 0 ? LocalizationStore.Current.SelectInstanceToMoveUp : LocalizationStore.Current.SelectInstanceToMoveDown, instances.Select(i => i.Name));

		if (name is null)
			return;

		if (direction < 0)
			_configRepo.MoveUp(name);
		else
			_configRepo.MoveDown(name);
	}

	private void RemoveInstanceInteractive(IReadOnlyList<EtcdConnectionConfig> instances)
	{
		var nameToRemove = _menu.Show(LocalizationStore.Current.SelectInstanceToRemove, instances.Select(i => i.Name));

		if (nameToRemove is null)
			return;

		if (Prompt.Confirm(string.Format(LocalizationStore.Current.AreYouSureRemove, nameToRemove)))
		{
			_configRepo.RemoveInstance(nameToRemove);

			AnsiConsole.MarkupLine(LocalizationStore.Current.InstanceRemoved);
		}

		_pressAnyKey.Show();
	}
}
