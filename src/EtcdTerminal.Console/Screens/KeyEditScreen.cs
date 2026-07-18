using Spectre.Console;

namespace EtcdTerminal.Console.Screens;

public sealed class KeyEditScreen(IEtcdClient _etcdClient)
{
	public async Task ShowAsync()
	{
		var key = AnsiConsole.Ask<string>("Enter key to edit:");

		var existing = await _etcdClient.GetKeyAsync(key);

		if (existing is null)
		{
			AnsiConsole.MarkupLine("[red]Key not found.[/]");
			AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
			System.Console.ReadKey(true);

			return;
		}

		AnsiConsole.MarkupLine($"Current value: [cyan]{Markup.Escape(existing.Value)}[/]");

		var newValue = AnsiConsole.Ask<string>("Enter new value:");

		var result = await _etcdClient.UpdateKeyAsync(key, newValue);

		if (result)
			AnsiConsole.MarkupLine("[green]Key updated successfully![/]");
		else
			AnsiConsole.MarkupLine("[red]Could not update key.[/]");

		AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
		System.Console.ReadKey(true);
	}
}
