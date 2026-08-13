using EtcdTerminal.App.Engine;
using EtcdTerminal.Configuration;
using Spectre.Console;

namespace EtcdTerminal.App.Components;

public static class MenuScreen
{
	public static async Task RunAsync(string title, IReadOnlyList<string> choices, EtcdConnectionConfig? config, Func<string, Task> onChoice)
	{
		while (true)
		{
			AnsiConsole.Clear();
			Header.Render();

			var choice = Menu.Show(title, choices, config: config);

			if (choice is null)
				return;

			await onChoice(choice);
		}
	}
}
