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

		var instances = _configRepo.LoadInstances();

		if (instances.Count == 0)
		{
			AnsiConsole.MarkupLine("[yellow]No etcd instances configured.[/]");
			AnsiConsole.MarkupLine($"Create config at: [cyan]~/.config/etcd-terminal/appsettings.json[/]");
			AnsiConsole.WriteLine();

			return null;
		}

		var selected = AnsiConsole.Prompt(
			new SelectionPrompt<EtcdConnectionConfig>()
				.Title("Select etcd instance:")
				.PageSize(10)
				.AddChoices(instances)
				.UseConverter(c => $"{c.Name}  [grey]({c.ConnectionString})[/]"));

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

			return null;
		}
	}
}
