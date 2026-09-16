using EtcdTerminal.App.Engine;
using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Components;

public sealed class MenuScreen(ITerminal _terminal, Menu _menu)
{
	public async Task RunAsync<TId>(string title, IReadOnlyList<MenuItem<TId>> items, Func<TId, Task> onChoice)
	{
		while (true)
		{
			_terminal.Clear();
			Header.Render(_terminal);

			var choice = _menu.Show(title, items);

			if (choice is null)
				return;

			await onChoice(choice.Id);
		}
	}
}
