using EtcdTerminal.App.Engine;
using EtcdTerminal.Presentation;

namespace EtcdTerminal.App.Components;

public sealed class MenuScreen(Menu _menu)
{
	public async Task RunAsync<TId>(string title, IReadOnlyList<Choice<TId>> items, Func<TId, Task> onChoice)
	{
		while (true)
		{
			var choice = _menu.Show(title, items);

			if (choice is null)
				return;

			await onChoice(choice.Id);
		}
	}
}
