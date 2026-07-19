using EtcdTerminal;
using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Modules;
using EtcdTerminal.Models;
using Spectre.Console;

namespace EtcdTerminal.App.Screens;

public sealed class KeyCreateScreen(IEtcdClient _etcdClient)
{
	public async Task ShowAsync(EtcdConnectionConfig config)
	{
		AnsiConsole.Clear();
		StatusBar.Render(config);

		var key = Prompt.Ask("Enter key:");

		if (key is null)
			return;

		var value = Prompt.Ask("Enter value:");

		if (value is null)
			return;

		var result = await _etcdClient.CreateKeyAsync(key, value);

		if (result)
			AnsiConsole.MarkupLine("[green]Key created successfully![/]");
		else
			AnsiConsole.MarkupLine("[red]Key already exists or could not be created.[/]");

		AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
		Console.ReadKey(true);
	}
}
