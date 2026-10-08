using EtcdTerminal.App.Engine;
using EtcdTerminal.Session;

namespace EtcdTerminal.App.Screens.MainMenu;

public sealed class MainScreen(
	IConnectionWorkflow _workflow,
	MainMenuItems _items,
	Menu _menu)
{
	public async Task ShowAsync()
	{
		try
		{
			await RunMenuLoopAsync();
		}
		catch
		{
			try
			{
				await _workflow.DisconnectAsync();
			}
			catch
			{
			}

			throw;
		}

		await _workflow.DisconnectAsync();
	}

	private async Task RunMenuLoopAsync()
	{
		while (true)
		{
			MainMenuAction? action = _menu.Show(string.Empty, _items.Build())?.Id;

			if (action is null)
				return;

			var entry = _items.Find(action.Value);

			if (entry is null)
				return;

			await entry.ShowAsync();
		}
	}
}
