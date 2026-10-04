using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.Session;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Presentation;

namespace EtcdTerminal.App.Screens;

public sealed class MainScreen(
	IConnectionWorkflow _workflow,
	IConnectionSession _session,
	IEnumerable<IMainMenuEntry> _entries,
	Menu _menu,
	ILocalization _localization)
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
			MainMenuAction? action = _menu.Show(string.Empty, BuildMenuItems())?.Id;

			if (action is null)
				return;

			var entry = _entries.FirstOrDefault(e => e.Action == action);

			if (entry is null)
				return;

			await entry.ShowAsync();
		}
	}

	/// <summary>
	/// Only shows the actions the connected account is actually permitted to perform.
	/// </summary>
	private List<Choice<MainMenuAction>> BuildMenuItems()
	{
		List<Choice<MainMenuAction>> items = [.. _entries
			.Where(e => e.IsAvailable(_session.Capabilities))
			.Select(e => new Choice<MainMenuAction>(e.Action, e.Label))];

		items.Add(new(MainMenuAction.Disconnect, _localization.Disconnect));

		return items;
	}
}
