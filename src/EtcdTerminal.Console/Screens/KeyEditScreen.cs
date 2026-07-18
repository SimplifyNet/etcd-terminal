using EtcdTerminal.Console.Engine;
using EtcdTerminal.Console.Modules;
using EtcdTerminal.Models;
using Spectre.Console;

namespace EtcdTerminal.Console.Screens;

public sealed class KeyEditScreen(IEtcdClient _etcdClient)
{
	public async Task ShowAsync(EtcdConnectionConfig config)
	{
		AnsiConsole.Clear();
		StatusBar.Render(config);

		var key = Prompt.Ask("Enter key to edit:");

		if (key is null)
			return;

		var existing = await _etcdClient.GetKeyAsync(key);

		if (existing is null)
		{
			AnsiConsole.MarkupLine("[red]Key not found.[/]");
			AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
			System.Console.ReadKey(true);

			return;
		}

		AnsiConsole.MarkupLine($"Current value: [cyan]{Markup.Escape(existing.Value)}[/]");

		var newValue = Prompt.Ask("Enter new value:");

		if (newValue is null)
			return;

		var result = await _etcdClient.UpdateKeyAsync(key, newValue);

		if (result)
			AnsiConsole.MarkupLine("[green]Key updated successfully![/]");
		else
			AnsiConsole.MarkupLine("[red]Could not update key.[/]");

		AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
		System.Console.ReadKey(true);
	}
}
