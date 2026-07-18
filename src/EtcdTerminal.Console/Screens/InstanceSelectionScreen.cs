using EtcdTerminal;
using EtcdTerminal.Models;
using Spectre.Console;

namespace EtcdTerminal.Console.Screens;

public sealed class InstanceSelectionScreen(IConnectionConfigRepository _configRepo, IEtcdClient _etcdClient)
{
	public async Task<EtcdConnectionConfig?> ShowAsync()
	{
		AnsiConsole.Write(new FigletText("etcd-terminal").Color(Color.Blue));
		AnsiConsole.MarkupLine("[grey]Console client for etcd v3+[/]\n");

		while (true)
		{
			var instances = _configRepo.LoadInstances();

			var choice = PromptForChoice(instances);

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

					AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
					System.Console.ReadKey(true);
				}
			}
		}
	}

	private string PromptForChoice(IReadOnlyList<EtcdConnectionConfig> instances)
	{
		var choices = new List<string>();
		choices.AddRange(instances.Select(i => i.Name));
		choices.Add("Manage Connections");
		choices.Add("Exit");

		return AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("Select etcd instance:")
				.PageSize(10)
				.AddChoices(choices)
				.UseConverter(c =>
				{
					var instance = instances.FirstOrDefault(i => i.Name == c);

					return instance is not null
						? $"{instance.Name}  [grey]({instance.ConnectionString})[/]"
						: c;
				}));
	}

	private void ManageConfigs(IReadOnlyList<EtcdConnectionConfig> instances)
	{
		AnsiConsole.Clear();

		var manageChoices = new List<string> { "Add Instance" };

		if (instances.Count > 0)
			manageChoices.Add("Remove Instance");

		manageChoices.Add("Back");

		var action = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("Manage Connections")
				.AddChoices(manageChoices));

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
		var name = AnsiConsole.Ask<string>("Enter instance name:");
		var connectionString = AnsiConsole.Ask<string>("Enter connection string:", "http://localhost:2379");
		var useSsl = AnsiConsole.Confirm("Use SSL?", false);
		var username = AnsiConsole.Ask<string>("Enter username (optional, leave empty for none):");
		var password = string.Empty;

		if (!string.IsNullOrEmpty(username))
		{
			password = AnsiConsole.Prompt(
				new TextPrompt<string>("Enter password:")
					.Secret());
		}

		var config = new EtcdConnectionConfig
		{
			Name = name,
			ConnectionString = connectionString,
			UseSsl = useSsl,
			Username = string.IsNullOrEmpty(username) ? null : username,
			Password = string.IsNullOrEmpty(password) ? null : password
		};

		_configRepo.AddInstance(config);

		AnsiConsole.MarkupLine("[green]Instance added successfully![/]");
		AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
		System.Console.ReadKey(true);
	}

	private void RemoveInstanceInteractive(IReadOnlyList<EtcdConnectionConfig> instances)
	{
		var choices = instances.Select(i => i.Name).Append("Back").ToList();

		var nameToRemove = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("Select instance to remove:")
				.AddChoices(choices));

		if (nameToRemove == "Back")
			return;

		if (AnsiConsole.Confirm($"Are you sure you want to remove [red]{nameToRemove}[/]?"))
		{
			_configRepo.RemoveInstance(nameToRemove);

			AnsiConsole.MarkupLine("[green]Instance removed successfully![/]");
		}

		AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
		System.Console.ReadKey(true);
	}
}
