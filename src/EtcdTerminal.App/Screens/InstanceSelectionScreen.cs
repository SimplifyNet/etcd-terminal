using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.Configuration;
using Spectre.Console;

namespace EtcdTerminal.App.Screens;

public sealed class InstanceSelectionScreen(IConnectionConfigRepository _configRepo, IEtcdClient _etcdClient, SettingsScreen _settings)
{
	private const string InstanceAdded = "[green]Instance added successfully![/]";
	private const string InstanceRemoved = "[green]Instance removed successfully![/]";
	private const string ConnectedSuccess = "[green]Connected successfully![/]";
	private const string InvalidConnStr = "[red]Invalid connection string. Must be a valid http or https URL.[/]";
	private const string ManageConnections = "Manage Connections";
	private const string AddInstance = "Add Instance";
	private const string EditInstance = "Edit Instance";
	private const string MoveUpInstance = "Move Up";
	private const string MoveDownInstance = "Move Down";
	private const string RemoveInstance = "Remove Instance";
	private const string Exit = "Exit";
	private const string Settings = "Settings";
	private const string NoConnectionsMessage = "No connections configured. Go to Manage Connections to add one.";
	private const string SelectInstanceToEdit = "Select instance to edit:";
	private const string SelectInstanceToMoveUp = "Select instance to move up:";
	private const string SelectInstanceToMoveDown = "Select instance to move down:";
	private const string SelectInstanceToRemove = "Select instance to remove:";
	private const string EnterInstanceName = "Enter instance name:";
	private const string EnterConnStr = "Enter connection string:";
	private const string DefaultConnStr = "http://localhost:2379";
	private const string EnterUsername = "Enter username (optional, leave empty for none):";
	private const string EnterPassword = "Enter password:";
	private const string Connecting = "Connecting...";

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

			if (choice == ManageConnections)
				ManageConfigs(instances);
			else if (choice == Settings)
				_settings.Show();
			else if (choice == Exit)
				return null;
			else
			{
				var selected = instances.First(i => i.Name == choice);

				try
				{
					await AnsiConsole.Status()
						.StartAsync(Connecting, async ctx =>
						{
							await _etcdClient.ConnectAsync(selected);
							await _etcdClient.PingAsync();
						});

					AnsiConsole.MarkupLine(ConnectedSuccess);

					return selected;
				}
				catch (Exception ex)
				{
					AnsiConsole.MarkupLine($"[red]Failed to connect:[/] {ex.Message}");
					AnsiConsole.WriteLine();
					PressAnyKeyPrompt.Show();
				}
			}
		}
	}

	private string? PromptForChoice(IReadOnlyList<EtcdConnectionConfig> instances)
	{
		var choices = new List<string>();

		choices.AddRange(instances.Select(i => i.Name));
		choices.Add(ManageConnections);
		choices.Add(Settings);
		choices.Add(Exit);

		if (instances.Count == 0)
		{
			AnsiConsole.MarkupLine($"[yellow]{NoConnectionsMessage}[/]");
			AnsiConsole.WriteLine();
		}

		return Menu.Show(string.Empty, choices, c =>
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

		var manageChoices = new List<string> { AddInstance };

		if (instances.Count > 0)
		{
			manageChoices.Add(EditInstance);
			manageChoices.Add(RemoveInstance);
		}

		if (instances.Count > 1)
		{
			manageChoices.Add(MoveUpInstance);
			manageChoices.Add(MoveDownInstance);
		}

		var action = Menu.Show(ManageConnections, manageChoices);

		if (action is null)
			return;

		if (action == AddInstance)
			AddInstanceInteractive();
		else if (action == EditInstance)
			EditInstanceInteractive(instances);
		else if (action == MoveUpInstance)
			MoveInstanceInteractive(instances, -1);
		else if (action == MoveDownInstance)
			MoveInstanceInteractive(instances, 1);
		else if (action == RemoveInstance)
			RemoveInstanceInteractive(instances);
	}

	private void AddInstanceInteractive()
	{
		var name = Prompt.Ask(EnterInstanceName);

		if (name is null)
			return;

		var connectionString = Prompt.Ask(EnterConnStr, DefaultConnStr);

		if (connectionString is null)
			return;

		if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
		{
			AnsiConsole.MarkupLine(InvalidConnStr);
			PressAnyKeyPrompt.Show();

			return;
		}

		var username = Prompt.Ask(EnterUsername, allowEmpty: true);

		if (username is null)
			return;

		var password = string.Empty;

		if (!string.IsNullOrEmpty(username))
		{
			var entered = Prompt.Secret(EnterPassword);

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

		AnsiConsole.MarkupLine(InstanceAdded);
		PressAnyKeyPrompt.Show();
	}

	private void EditInstanceInteractive(IReadOnlyList<EtcdConnectionConfig> instances)
	{
		var existingName = Menu.Show(SelectInstanceToEdit, instances.Select(i => i.Name));

		if (existingName is null)
			return;

		var existing = instances.First(i => i.Name == existingName);

		AnsiConsole.Clear();
		Header.Render();

		var name = Prompt.Ask(EnterInstanceName, existing.Name);

		if (name is null)
			return;

		var connectionString = Prompt.Ask(EnterConnStr, existing.ConnectionString);

		if (connectionString is null)
			return;

		if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
		{
			AnsiConsole.MarkupLine(InvalidConnStr);
			PressAnyKeyPrompt.Show();

			return;
		}

		var username = Prompt.Ask(EnterUsername, existing.Username ?? string.Empty);

		if (username is null)
			return;

		var password = string.Empty;

		if (!string.IsNullOrEmpty(username))
		{
			password = existing.Password ?? string.Empty;

			if (Prompt.Confirm("Change password?"))
			{
				var newPassword = Prompt.Secret(EnterPassword);

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

		AnsiConsole.MarkupLine("[green]Instance updated successfully![/]");
		PressAnyKeyPrompt.Show();
	}

	private void MoveInstanceInteractive(IReadOnlyList<EtcdConnectionConfig> instances, int direction)
	{
		var name = Menu.Show(direction < 0 ? SelectInstanceToMoveUp : SelectInstanceToMoveDown, instances.Select(i => i.Name));

		if (name is null)
			return;

		if (direction < 0)
			_configRepo.MoveUp(name);
		else
			_configRepo.MoveDown(name);
	}

	private void RemoveInstanceInteractive(IReadOnlyList<EtcdConnectionConfig> instances)
	{
		var nameToRemove = Menu.Show(SelectInstanceToRemove, instances.Select(i => i.Name));

		if (nameToRemove is null)
			return;

		if (Prompt.Confirm($"Are you sure you want to remove {nameToRemove}?"))
		{
			_configRepo.RemoveInstance(nameToRemove);

			AnsiConsole.MarkupLine(InstanceRemoved);
		}

		PressAnyKeyPrompt.Show();
	}
}
