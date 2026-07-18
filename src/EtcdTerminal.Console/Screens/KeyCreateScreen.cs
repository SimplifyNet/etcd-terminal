using EtcdTerminal.Console.Helpers;
using EtcdTerminal.Models;
using Spectre.Console;

namespace EtcdTerminal.Console.Screens;

public sealed class KeyCreateScreen(IEtcdClient _etcdClient)
{
	public async Task ShowAsync(EtcdConnectionConfig config)
	{
		AnsiConsole.Clear();
		StatusBar.Render(config);

		var key = AnsiConsole.Ask<string>("Enter key:");
		var value = AnsiConsole.Ask<string>("Enter value:");

		var result = await _etcdClient.CreateKeyAsync(key, value);

		if (result)
			AnsiConsole.MarkupLine("[green]Key created successfully![/]");
		else
			AnsiConsole.MarkupLine("[red]Key already exists or could not be created.[/]");

		AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
		System.Console.ReadKey(true);
	}
}
