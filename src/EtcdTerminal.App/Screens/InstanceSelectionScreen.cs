using EtcdTerminal.App.Engine;
using EtcdTerminal.Models;
using Simplify.System;
using Spectre.Console;

namespace EtcdTerminal.App.Screens;

public sealed class InstanceSelectionScreen(IConnectionConfigRepository _configRepo, IEtcdClient _etcdClient)
{
	private const string InstanceAdded = "[green]Instance added successfully![/]";
	private const string InstanceRemoved = "[green]Instance removed successfully![/]";
	private const string ConnectedSuccess = "[green]Connected successfully![/]";
	private const string NameEmpty = "[red]Instance name cannot be empty.[/]";
	private const string InvalidConnStr = "[red]Invalid connection string. Must be a valid http or https URL.[/]";
	private const string ConsoleClientDesc = "[grey]Console client for etcd v3+[/]";
	private const string ManageConnections = "Manage Connections";
	private const string AddInstance = "Add Instance";
	private const string RemoveInstance = "Remove Instance";
	private const string Exit = "Exit";
	private const string SelectInstance = "Select etcd instance:";
	private const string SelectInstanceToRemove = "Select instance to remove:";
	private const string EnterInstanceName = "Enter instance name:";
	private const string EnterConnStr = "Enter connection string:";
	private const string DefaultConnStr = "http://localhost:2379";
	private const string UseSsl = "Use SSL?";
	private const string EnterUsername = "Enter username (optional, leave empty for none):";
	private const string EnterPassword = "Enter password:";
	private const string Connecting = "Connecting...";

	public async Task<EtcdConnectionConfig?> ShowAsync()
	{
		while (true)
		{
			AnsiConsole.Clear();

			AnsiConsole.Write(new FigletText("etcd-terminal").Color(Color.OrangeRed1).Centered());

			AnsiConsole.Write(Align.Center(new Markup($"[grey]Version: [/][white]{GetVersion()}[/]")));
			Console.WriteLine();

			AnsiConsole.Write(Align.Center(new Markup(ConsoleClientDesc)));
			Console.WriteLine();

			var instances = _configRepo.LoadInstances();

			var choice = PromptForChoice(instances);

			if (choice is null)
				return null;

			if (choice == ManageConnections)
			{
				ManageConfigs(instances);
			}
			else if (choice == Exit)
			{
				return null;
			}
			else
			{
				var selected = instances.First(i => i.Name == choice);

				try
				{
					await AnsiConsole.Status()
						.StartAsync(Connecting, async ctx =>
						{
							await _etcdClient.ConnectAsync(selected);
						});

					AnsiConsole.MarkupLine(ConnectedSuccess);

					return selected;
				}
				catch (Exception ex)
				{
					AnsiConsole.MarkupLine($"[red]Failed to connect: {ex.Message}[/]");
					AnsiConsole.WriteLine();

					AnsiConsole.MarkupLine(Prompt.PressAnyKeyMarkup);
					Console.ReadKey(true);
				}
			}
		}
	}

	private string? PromptForChoice(IReadOnlyList<EtcdConnectionConfig> instances)
	{
		var choices = new List<string>();
		choices.AddRange(instances.Select(i => i.Name));
		choices.Add(ManageConnections);
		choices.Add(Exit);

		return Menu.Show(SelectInstance, choices, c =>
		{
			var instance = instances.FirstOrDefault(i => i.Name == c);

			return instance is not null
				? $"{instance.Name}  [grey]({instance.ConnectionString})[/]"
				: c;
		});
	}

	private void ManageConfigs(IReadOnlyList<EtcdConnectionConfig> instances)
	{
		AnsiConsole.Clear();

		var manageChoices = new List<string> { AddInstance };

		if (instances.Count > 0)
			manageChoices.Add(RemoveInstance);

		var action = Menu.Show(ManageConnections, manageChoices);

		if (action is null)
			return;

		if (action == AddInstance)
		{
			AddInstanceInteractive();
		}
		else if (action == RemoveInstance)
		{
			RemoveInstanceInteractive(instances);
		}
	}

	private void AddInstanceInteractive()
	{
		var name = Prompt.Ask(EnterInstanceName);

		if (name is null)
			return;

		name = name.Trim();

		if (string.IsNullOrWhiteSpace(name))
		{
			AnsiConsole.MarkupLine(NameEmpty);
			AnsiConsole.MarkupLine(Prompt.PressAnyKeyMarkup);
			Console.ReadKey(true);

			return;
		}

		var connectionString = Prompt.Ask(EnterConnStr, DefaultConnStr);

		if (connectionString is null)
			return;

		if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
		{
			AnsiConsole.MarkupLine(InvalidConnStr);
			AnsiConsole.MarkupLine(Prompt.PressAnyKeyMarkup);
			Console.ReadKey(true);

			return;
		}

		var useSsl = Prompt.Confirm(UseSsl);

		if (useSsl is null)
			return;

		var username = Prompt.Ask(EnterUsername);

		if (username is null)
			return;

		var password = string.Empty;

		if (!string.IsNullOrEmpty(username))
		{
			password = Prompt.Secret(EnterPassword);

			if (password is null)
				return;
		}

		var config = new EtcdConnectionConfig
		{
			Name = name,
			ConnectionString = connectionString,
			UseSsl = useSsl.Value,
			Username = string.IsNullOrEmpty(username) ? null : username,
			Password = string.IsNullOrEmpty(password) ? null : password
		};

		_configRepo.AddInstance(config);

		AnsiConsole.MarkupLine(InstanceAdded);
		AnsiConsole.MarkupLine(Prompt.PressAnyKeyMarkup);
		Console.ReadKey(true);
	}

	private void RemoveInstanceInteractive(IReadOnlyList<EtcdConnectionConfig> instances)
	{
		var nameToRemove = Menu.Show(SelectInstanceToRemove, instances.Select(i => i.Name));

		if (nameToRemove is null)
			return;

		var confirm = Prompt.Confirm($"Are you sure you want to remove {nameToRemove}?");

		if (confirm is not true)
		{
			_configRepo.RemoveInstance(nameToRemove);

			AnsiConsole.MarkupLine(InstanceRemoved);
		}

		AnsiConsole.MarkupLine(Prompt.PressAnyKeyMarkup);
		Console.ReadKey(true);
	}

	private string GetVersion()
	{
		var version = AssemblyInfo.Entry.Version;

		return $"{version.Major}.{version.Minor}" + (version.Build != 0 ? "." + version.Build : "");
	}
}
