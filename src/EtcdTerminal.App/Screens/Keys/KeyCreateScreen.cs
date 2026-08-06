using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.Models;
using Spectre.Console;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyCreateScreen(IEtcdClient _etcdClient)
{
	private const string EnterKeyPrompt = "Enter key:";
	private const string EnterValuePrompt = "Enter value:";
	private const string KeyCreated = "[green]Key created successfully![/]";
	private const string KeyCreateFailed = "[red]Key already exists or could not be created.[/]";

	public async Task ShowAsync(EtcdConnectionConfig config)
	{
		AnsiConsole.Clear();
		Header.Render();
		var savedTop = Console.CursorTop;
		StatusBar.Render(config);
		Console.CursorTop = savedTop;
		Console.CursorLeft = 0;

		var key = Prompt.Ask(EnterKeyPrompt);

		if (key is null)
			return;

		var value = Prompt.Ask(EnterValuePrompt);

		if (value is null)
			return;

		var result = await _etcdClient.CreateKeyAsync(key, value);

		if (result)
			AnsiConsole.MarkupLine(KeyCreated);
		else
			AnsiConsole.MarkupLine(KeyCreateFailed);

		PressAnyKeyPrompt.Show();
	}
}
