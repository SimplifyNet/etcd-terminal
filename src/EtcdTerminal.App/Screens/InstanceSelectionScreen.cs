using EtcdTerminal;
using System.Reflection;
using EtcdTerminal.App.Engine;
using EtcdTerminal.Models;
using Simplify.System;
using Spectre.Console;

namespace EtcdTerminal.App.Screens;

public sealed class InstanceSelectionScreen(IConnectionConfigRepository _configRepo, IEtcdClient _etcdClient)
{
	public async Task<EtcdConnectionConfig?> ShowAsync()
	{
		while (true)
		{
			AnsiConsole.Clear();
			AnsiConsole.Write(new FigletText("etcd-terminal").Color(Color.OrangeRed1).Centered());
			AnsiConsole.MarkupLineInterpolated($"[grey]Version: {GetVersion()}[/]");
			AnsiConsole.MarkupLine("[grey]Console client for etcd v3+[/]");

			var instances = _configRepo.LoadInstances();

			var choice = PromptForChoice(instances);

			if (choice is null)
				return null;

			if (choice == "Manage Connections")
			{
				ManageConfigs(instances);
			}
			else if (choice == "Exit")
			{
				return null;
			}
			else
			{
				var selected = instances.First(i => i.Name == choice);

				try
				{
					await AnsiConsole.Status()
						.StartAsync("Connecting...", async ctx =>
						{
							await _etcdClient.ConnectAsync(selected);
						});

					AnsiConsole.MarkupLine("[green]Connected successfully![/]");

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
		choices.Add("Manage Connections");
		choices.Add("Exit");

		return Menu.Show("Select etcd instance:", choices, c =>
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

		var manageChoices = new List<string> { "Add Instance" };

		if (instances.Count > 0)
			manageChoices.Add("Remove Instance");

		var action = Menu.Show("Manage Connections", manageChoices);

		if (action is null)
			return;

		if (action == "Add Instance")
		{
			AddInstanceInteractive();
		}
		else if (action == "Remove Instance")
		{
			RemoveInstanceInteractive(instances);
		}
	}

	private void AddInstanceInteractive()
	{
		var name = Prompt.Ask("Enter instance name:");

		if (name is null)
			return;

		name = name.Trim();

		if (string.IsNullOrWhiteSpace(name))
		{
			AnsiConsole.MarkupLine("[red]Instance name cannot be empty.[/]");
			AnsiConsole.MarkupLine(Prompt.PressAnyKeyMarkup);
			Console.ReadKey(true);

			return;
		}

		var connectionString = Prompt.Ask("Enter connection string:", "http://localhost:2379");

		if (connectionString is null)
			return;

		if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
		{
			AnsiConsole.MarkupLine("[red]Invalid connection string. Must be a valid http or https URL.[/]");
			AnsiConsole.MarkupLine(Prompt.PressAnyKeyMarkup);
			Console.ReadKey(true);

			return;
		}

		var useSsl = Prompt.Confirm("Use SSL?");

		if (useSsl is null)
			return;

		var username = Prompt.Ask("Enter username (optional, leave empty for none):");

		if (username is null)
			return;

		var password = string.Empty;

		if (!string.IsNullOrEmpty(username))
		{
			password = Prompt.Secret("Enter password:");

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

		AnsiConsole.MarkupLine("[green]Instance added successfully![/]");
		AnsiConsole.MarkupLine(Prompt.PressAnyKeyMarkup);
		Console.ReadKey(true);
	}

	private void RemoveInstanceInteractive(IReadOnlyList<EtcdConnectionConfig> instances)
	{
		var nameToRemove = Menu.Show("Select instance to remove:", instances.Select(i => i.Name));

		if (nameToRemove is null)
			return;

		var confirm = Prompt.Confirm($"Are you sure you want to remove {nameToRemove}?");

		if (confirm is not true)
		{
			_configRepo.RemoveInstance(nameToRemove);

			AnsiConsole.MarkupLine("[green]Instance removed successfully![/]");
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
