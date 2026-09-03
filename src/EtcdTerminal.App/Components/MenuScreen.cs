using EtcdTerminal.App.Engine;
using EtcdTerminal.Configuration;
using Spectre.Console;

namespace EtcdTerminal.App.Components;

public sealed class MenuScreen(Menu _menu)
{
	public async Task RunAsync(string title, IReadOnlyList<string> choices, EtcdConnectionConfig? config, Func<string, Task> onChoice)
	{
		while (true)
		{
			AnsiConsole.Clear();
			Header.Render();

			var choice = _menu.Show(title, choices, config: config);

			if (choice is null)
				return;

			await onChoice(choice);
		}
	}
}
