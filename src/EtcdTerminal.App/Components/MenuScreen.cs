using EtcdTerminal.App.Engine;
using EtcdTerminal.Configuration;
using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Components;

public sealed class MenuScreen(ITerminal _terminal, Menu _menu)
{
	public async Task RunAsync(string title, IReadOnlyList<string> choices, EtcdConnectionConfig? config, Func<string, Task> onChoice)
	{
		while (true)
		{
			_terminal.Clear();
			Header.Render();

			var choice = _menu.Show(title, choices, config: config);

			if (choice is null)
				return;

			await onChoice(choice);
		}
	}
}
